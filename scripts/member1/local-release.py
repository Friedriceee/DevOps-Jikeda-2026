#!/usr/bin/env python3
"""Real, isolated Compose release/rollback rehearsal. Never creates cloud resources.

Build images first, then: python3 scripts/member1/local-release.py --previous sha-A --current sha-B
Secrets stay in a git-ignored mode-0600 file. Evidence contains no credentials.
"""
import argparse
import datetime as dt
import json
import os
from pathlib import Path
import re
import secrets
import subprocess
import time

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'output/member1'
OUT.mkdir(parents=True, exist_ok=True)
parser = argparse.ArgumentParser()
parser.add_argument('--previous', required=True)
parser.add_argument('--current', required=True)
parser.add_argument('--port', type=int, default=8082)
args = parser.parse_args()
for tag in (args.previous, args.current):
    if not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9_.-]{0,127}', tag):
        parser.error('Invalid image tag')
secret_file = ROOT / '.env.member1.local'
if not secret_file.exists():
    secret_file.write_text(json.dumps({
        'POSTGRES_PASSWORD': secrets.token_hex(24),
        'JWT_KEY': secrets.token_hex(32),
        'SEED_PASSWORD': 'Demo-' + secrets.token_hex(12),
    }))
secret_file.chmod(0o600)
env = os.environ | json.loads(secret_file.read_text()) | {
    'REGISTRY': 'jikeda-member1', 'STAGING_PORT': str(args.port),
    'STAGING_BIND_ADDRESS': '127.0.0.1', 'COMPOSE_PROJECT_NAME': 'jikeda-member1',
}
compose = ['docker', 'compose', '-f', 'docker-compose.staging.yml']
record = {'kind': 'local-compose-rehearsal', 'cloud_deployed': False,
          'started_at': dt.datetime.now(dt.timezone.utc).isoformat(),
          'base_url': f'http://127.0.0.1:{args.port}', 'stages': []}

def command(cmd, log, extra=None):
    with (OUT / log).open('w') as stream:
        result = subprocess.run(cmd, cwd=ROOT, env=env | (extra or {}), stdout=stream, stderr=subprocess.STDOUT)
    if result.returncode:
        raise RuntimeError(f'{log}: exit {result.returncode}; inspect saved log')

def database_snapshot():
    sql = '''SELECT json_build_object(
      'orders', (SELECT count(*) FROM "Orders"),
      'completed', (SELECT count(*) FROM "Orders" WHERE "Status" = 9),
      'negative_stock', (SELECT count(*) FROM "Dishes" WHERE "Inventory" < 0),
      'duplicate_assignments', (SELECT count(*) FROM (SELECT "OrderId" FROM "RiderDeliveries" GROUP BY "OrderId" HAVING count(*) > 1) x)
    );'''
    raw = subprocess.check_output(compose + ['exec', '-T', 'db', 'psql', '-U', 'takeout', '-d', 'takeout', '-Atc', sql], cwd=ROOT, env=env, text=True)
    return json.loads(raw)

def deploy(tag, label):
    env['IMAGE_TAG'] = tag
    started = time.monotonic()
    before = database_snapshot() if record['stages'] else None
    command(compose + ['up', '-d', '--pull', 'never', '--wait', '--wait-timeout', '240'], f'{label}-deploy.log')
    after_deploy = database_snapshot()
    if before and before != after_deploy:
        raise RuntimeError(f'{label}: database snapshot changed during image switch')
    command(['node', 'scripts/smoke/staging-smoke.mjs'], f'{label}-smoke.log', {
        'BASE_URL': record['base_url'], 'SMOKE_PASSWORD': env['SEED_PASSWORD'],
    })
    snapshot = database_snapshot()
    if snapshot['negative_stock'] or snapshot['duplicate_assignments']:
        raise RuntimeError(f'{label}: database invariant failed')
    text = (OUT / f'{label}-smoke.log').read_text()
    match = re.search(r'Smoke test passed: (\d+) steps, order #(\d+)', text)
    if not match:
        raise RuntimeError('Smoke test did not record success')
    stage = {'stage': label, 'image_tag': tag, 'duration_seconds': round(time.monotonic()-started, 2),
             'smoke_steps': int(match[1]), 'order_id': int(match[2]), 'database': snapshot,
             'database_preserved_on_image_switch': before is None or before == after_deploy, 'passed': True}
    record['stages'].append(stage)
    print(json.dumps(stage), flush=True)

try:
    for tag, label in [(args.previous,'01-baseline'),(args.current,'02-release'),(args.previous,'03-rollback'),(args.current,'04-roll-forward')]:
        deploy(tag, label)
    record['passed'] = True
except Exception as exc:
    record['passed'] = False
    record['error'] = str(exc)
    raise
finally:
    record['finished_at'] = dt.datetime.now(dt.timezone.utc).isoformat()
    (OUT / 'release-rehearsal.json').write_text(json.dumps(record, indent=2))
