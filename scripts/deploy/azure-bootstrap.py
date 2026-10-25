#!/usr/bin/env python3
"""Create an Azure Run Command script without embedding any deployment secret."""
import argparse
import base64
import json
from pathlib import Path
import re


def bootstrap(revision, drill=False):
    if not re.fullmatch('[a-f0-9]{40}', revision):
        raise ValueError('Expected full source SHA')
    root = Path(__file__).resolve().parents[2]
    paths = {'compose.yml': 'docker-compose.staging.yml',
             'compose.override.yml': 'deploy/azure-staging/compose.override.yml',
             'Caddyfile': 'deploy/azure-staging/Caddyfile',
             'staging-smoke.mjs': 'scripts/smoke/staging-smoke.mjs'}
    payload = dict(revision=revision, files={name: (root / path).read_text() for name, path in paths.items()})
    encoded = base64.b64encode(json.dumps(payload).encode()).decode()
    program = base64.b64encode(Path(__file__).with_name('azure-staging.py').read_bytes()).decode()
    return f'''#!/usr/bin/env bash
set -euo pipefail
umask 077
python3 - <<'BOOTSTRAP'
import base64, subprocess
from pathlib import Path
folder=Path('/opt/jikeda5006/operations')
folder.mkdir(parents=True,exist_ok=True,mode=0o700)
(folder/'deploy.py').write_bytes(base64.b64decode('{program}'))
(folder/'payload.json').write_bytes(base64.b64decode('{encoded}'))
log=folder/'deployment.log'
with log.open('w') as output:
    result=subprocess.run(['python3','-u',str(folder/'deploy.py'),str(folder/'payload.json')]{"+['--rollback-drill']" if drill else ""},stdout=output,stderr=subprocess.STDOUT)
if result.returncode:
    print('DEPLOYMENT_FAILED: inspect /opt/jikeda5006/operations/deployment.log')
else:
    for line in log.read_text().splitlines():
        if line.startswith(('DEPLOYMENT_OK','SMOKE_OK','ROLLBACK_DRILL_OK','DATABASE_BACKUP_OK','PUBLIC_ALIAS_OK','{{')): print(line)
raise SystemExit(result.returncode)
BOOTSTRAP
'''


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--revision', required=True)
    parser.add_argument('--rollback-drill', action='store_true')
    args = parser.parse_args()
    print(bootstrap(args.revision, args.rollback_drill))
