#!/usr/bin/env python3
"""Explicit Azure what-if/provision/app-deploy entrypoint. No implicit login or spend."""
import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('--resource-group',required=True)
p.add_argument('--prefix',required=True)
p.add_argument('--stage',choices=['infra','apps'],default='infra')
p.add_argument('--images',help='JSON object with five immutable ACR image digests')
p.add_argument('--apply',action='store_true',help='Create/update paid resources; default is what-if only')
args=p.parse_args()
if not re.fullmatch(r'[a-z][a-z0-9-]{3,19}',args.prefix):
    p.error('prefix must be 4-20 lowercase letters/digits/hyphens')
if args.stage=='apps':
    subprocess.run(['python3',str(root/'scripts/member1/service-preflight.py')],check=True)
    if not args.images:
        p.error('--images is required for apps')
    images=json.loads(Path(args.images).read_text())
    for service in ['identity','catalog','order','fulfilment','gateway']:
        if not re.fullmatch(r'[^\s]+\.azurecr\.io/[^\s]+@sha256:[0-9a-f]{64}',images.get(service,'')):
            p.error('An immutable ACR digest is required for '+service)
    if os.environ.get('JIKEDA_MIGRATIONS_VERIFIED')!='true':
        p.error('Run each service migration using a protected migration identity, then set JIKEDA_MIGRATIONS_VERIFIED=true')
else:
    images={}
required=['AZURE_DB_ADMIN_PASSWORD','JIKEDA_JWT_KEY']+[f'JIKEDA_{s.upper()}_DB_PASSWORD' for s in ['identity','catalog','order','fulfilment']]
missing=[key for key in required if not os.environ.get(key)]
if missing:
    p.error('Missing protected environment settings: '+', '.join(missing))
subprocess.run(['az','account','show','--query','{name:name,state:state}','-o','json'],check=True)
values={'prefix':args.prefix,'deployApps':args.stage=='apps','images':images,
        'databaseAdminPassword':os.environ['AZURE_DB_ADMIN_PASSWORD'],
        'databasePasswords':{s:os.environ[f'JIKEDA_{s.upper()}_DB_PASSWORD'] for s in ['identity','catalog','order','fulfilment']},
        'jwtKey':os.environ['JIKEDA_JWT_KEY']}
with tempfile.TemporaryDirectory(prefix='jikeda-azure-') as directory:
    parameters=Path(directory)/'parameters.json'
    parameters.write_text(json.dumps({'$schema':'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#','contentVersion':'1.0.0.0','parameters':{k:{'value':v} for k,v in values.items()}}))
    parameters.chmod(0o600)
    subprocess.run(['az','deployment','group','create' if args.apply else 'what-if','--resource-group',args.resource_group,'--template-file',str(root/'infra/azure/main.bicep'),'--parameters','@'+str(parameters)],check=True)
print('Azure operation completed. Validate bootstrap, migrations, readiness and smoke separately before recording release success.')
