#!/usr/bin/env python3
"""Deploy the isolated SWE5006 stack on the existing demonstration VM."""
import argparse
import datetime
import fcntl
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import subprocess
import sys
import urllib.request

ROOT = Path('/opt/jikeda5006')
SHARED = ROOT / 'shared'
PUBLIC_HOST = 'jikeda5001-m1-apy24jqzjhch2.eastus2.cloudapp.azure.com'
REGISTRY = 'ghcr.io/friedriceee/devops-jikeda-2026'


def run(command, **kwargs):
    return subprocess.run(command, check=True, text=True, **kwargs)


def compose(release, *args, **kwargs):
    env = dict(os.environ, IMAGE_TAG=release['imageTag'], REGISTRY=REGISTRY)
    return run(['docker', 'compose', '-p', 'jikeda5006', '--env-file', str(SHARED / 'runtime.env'),
                '-f', str(Path(release['directory']) / 'compose.yml'),
                '-f', str(Path(release['directory']) / 'compose.override.yml'), *args], env=env, **kwargs)


def runtime():
    return dict(line.split('=', 1) for line in (SHARED / 'runtime.env').read_text().splitlines() if line and not line.startswith('#'))


def fingerprint(release):
    # Compare persistent business rows across image changes without exposing them.
    sql = '''SELECT json_build_object(
      'orders', (SELECT json_agg(t) FROM (SELECT "Id", "Status", "Price" FROM "Orders" ORDER BY "Id") t),
      'stock', (SELECT json_agg(t) FROM (SELECT "Id", "Inventory" FROM "Dishes" ORDER BY "Id") t),
      'payments', (SELECT json_agg(t) FROM (SELECT "Id", "OrderId", "Amount" FROM "Payments" ORDER BY "Id") t),
      'coupons', (SELECT json_agg(t) FROM (SELECT "Id", "IssuedQuantity", "UsedQuantity" FROM "CustomerCoupons" ORDER BY "Id") t)
    );'''
    result = compose(release, 'exec', '-T', 'db', 'psql', '-U', 'takeout', '-d', 'takeout', '-Atc', sql, capture_output=True)
    return hashlib.sha256(result.stdout.strip().encode()).hexdigest()


def smoke(release, label):
    env = dict(os.environ, BASE_URL='http://127.0.0.1:8080', SMOKE_PASSWORD=runtime()['SEED_PASSWORD'])
    log = SHARED / f'smoke-{label}.log'
    with log.open('w') as output:
        result = subprocess.run(['docker', 'run', '--rm', '--network', 'host',
                                 '--env', 'BASE_URL', '--env', 'SMOKE_PASSWORD',
                                 '--volume', str(Path(release['directory']) / 'staging-smoke.mjs') + ':/smoke.mjs:ro',
                                 'node:24-alpine', 'node', '/smoke.mjs'],
                                env=env, stdout=output, stderr=subprocess.STDOUT, text=True)
    if result.returncode:
        # HTTP error details remain in the private log rather than Actions output.
        raise RuntimeError(f'Business smoke failed; inspect {log}')
    lines = [line for line in log.read_text().splitlines() if '✓' in line]
    print(f'SMOKE_OK {label} {len(lines)} checks', flush=True)
    return len(lines)


def apply(release):
    compose(release, 'pull', 'api', 'frontend', 'proxy', stdout=subprocess.DEVNULL)
    compose(release, 'up', '-d', '--wait', '--wait-timeout', '240', stdout=subprocess.DEVNULL)


def backup(release):
    folder = SHARED / 'backups'
    folder.mkdir(exist_ok=True, mode=0o700)
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    path = folder / f'{stamp}-{release["revision"][:12]}.dump'
    with path.open('wb') as output:
        env = dict(os.environ, IMAGE_TAG=release['imageTag'], REGISTRY=REGISTRY)
        subprocess.run(['docker', 'compose', '-p', 'jikeda5006', '--env-file', str(SHARED / 'runtime.env'),
                        '-f', str(Path(release['directory']) / 'compose.yml'),
                        'exec', '-T', 'db', 'pg_dump', '-U', 'takeout', '-d', 'takeout', '-Fc'],
                       env=env, stdout=output, check=True)
    assert path.stat().st_size > 0
    print('DATABASE_BACKUP_OK ' + path.name, flush=True)


def deploy(payload, drill=False):
    revision = payload['revision']
    if not re.fullmatch('[a-f0-9]{40}', revision):
        raise ValueError('A full source commit SHA is required')
    os.umask(0o077)
    SHARED.mkdir(parents=True, exist_ok=True, mode=0o700)
    with (SHARED / 'deployment.lock').open('w') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        envpath = SHARED / 'runtime.env'
        if not envpath.exists():
            values = dict(POSTGRES_PASSWORD=secrets.token_hex(24), JWT_KEY=secrets.token_hex(32),
                          SEED_PASSWORD='Demo-' + secrets.token_hex(12), PUBLIC_HOST=PUBLIC_HOST,
                          STAGING_BIND_ADDRESS='127.0.0.1', STAGING_PORT='8080')
            envpath.write_text(''.join(f'{key}={value}\n' for key, value in values.items()))
            envpath.chmod(0o600)
        directory = ROOT / 'releases' / revision
        directory.mkdir(parents=True, exist_ok=True, mode=0o700)
        allowed = {'compose.yml', 'compose.override.yml', 'Caddyfile', 'staging-smoke.mjs'}
        if set(payload['files']) != allowed:
            raise ValueError('Unexpected deployment files')
        for name, content in payload['files'].items():
            (directory / name).write_text(content)
        currentpath = SHARED / 'current-release.json'
        previous = json.loads(currentpath.read_text()) if currentpath.exists() else None
        target = dict(revision=revision, imageTag='sha-' + revision[:12], directory=str(directory),
                      publicUrl='https://' + PUBLIC_HOST + ':8443',
                      deployedAtUtc=datetime.datetime.now(datetime.timezone.utc).isoformat())
        if previous:
            backup(previous)
        try:
            apply(target)
            checks = smoke(target, 'new')
            urllib.request.urlopen('http://127.0.0.1:8080/api/health', timeout=20).close()
            proof = dict(revision=revision, publicUrl=target['publicUrl'], smokeChecks=checks,
                         rollbackDrill='not requested', databaseRollback=False)
            if drill:
                if previous is None or previous['imageTag'] == target['imageTag']:
                    raise RuntimeError('Rollback drill requires a different previous release')
                before = fingerprint(target)
                apply(previous)
                assert fingerprint(previous) == before, 'Business data changed during rollback'
                smoke(previous, 'rollback')
                after_old_smoke = fingerprint(previous)
                apply(target)
                assert fingerprint(target) == after_old_smoke, 'Business data changed during roll-forward'
                smoke(target, 'roll-forward')
                proof['rollbackDrill'] = 'passed'
                proof['previousRevision'] = previous['revision']
                proof['dataPreservedAcrossImageChanges'] = True
                print('ROLLBACK_DRILL_OK ' + previous['revision'] + ' -> ' + revision, flush=True)
            if previous and previous['imageTag'] != target['imageTag']:
                (SHARED / 'previous-release.json').write_text(json.dumps(previous, indent=2))
            currentpath.write_text(json.dumps(target, indent=2))
            (SHARED / 'verification.json').write_text(json.dumps(proof, indent=2))
            print('DEPLOYMENT_OK ' + revision, flush=True)
            print(json.dumps(proof), flush=True)
        except Exception:
            if previous:
                apply(previous)
                smoke(previous, 'failure-restoration')
                print('PREVIOUS_RELEASE_RESTORED ' + previous['revision'], flush=True)
            raise


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('payload')
    parser.add_argument('--rollback-drill', action='store_true')
    args = parser.parse_args()
    deploy(json.loads(Path(args.payload).read_text()), args.rollback_drill)
