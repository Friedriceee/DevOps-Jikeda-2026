#!/usr/bin/env python3
"""Exercise the Azure bootstrap SQL on a disposable local PostgreSQL container."""
import json
import os
from pathlib import Path
import secrets
import subprocess
import time

root = Path(__file__).resolve().parents[2]
name = 'jikeda-role-test-' + secrets.token_hex(4)
env = os.environ | {'POSTGRES_USER':'jikeda_admin','POSTGRES_PASSWORD':secrets.token_hex(24),
                    'PGUSER':'jikeda_admin','PGHOST':'127.0.0.1','PGSSLMODE':'disable','PGDATABASE':'postgres'}
env['PGPASSWORD'] = env['POSTGRES_PASSWORD']
for role in ['IDENTITY','CATALOG','ORDER','FULFILMENT']:
    env[role+'_PASSWORD'] = secrets.token_hex(24)
def run(args, **kwargs):
    return subprocess.run(args, env=env, capture_output=True, text=True, **kwargs)
def psql(db, sql, user='jikeda_admin', password=None):
    test_env = env | {'PGPASSWORD':password or env['POSTGRES_PASSWORD']}
    return subprocess.run(['docker','exec','-e','PGPASSWORD','-e','PGHOST',name,'psql','-X','-v','ON_ERROR_STOP=1','-U',user,'-d',db,'-Atc',sql],env=test_env,capture_output=True,text=True)
results = []
try:
    run(['docker','run','-d','--name',name,'--network','none','-e','POSTGRES_USER','-e','POSTGRES_PASSWORD','postgres:16'],check=True)
    for attempt in range(60):
        if psql('postgres','SELECT 1').returncode == 0:
            break
        time.sleep(1)
    else:
        raise RuntimeError('Disposable PostgreSQL did not become ready')
    for db in ['identity','catalog','order','fulfilment']:
        psql('postgres',f'CREATE DATABASE "{db}"').check_returncode()
    forwarded = ['PGUSER','PGPASSWORD','PGHOST','PGDATABASE','PGSSLMODE'] + [r+'_PASSWORD' for r in ['IDENTITY','CATALOG','ORDER','FULFILMENT']]
    args = ['docker','exec','-i']
    for key in forwarded:
        args += ['-e',key]
    run(args+[name,'sh','-s'],input=(root/'infra/azure/bootstrap.sh').read_text(),check=True)
    for db in ['identity','catalog','order','fulfilment']:
        role = db+'_app'; password = env[db.upper()+'_PASSWORD']
        psql(db,'CREATE TABLE member1_probe (id int PRIMARY KEY); INSERT INTO member1_probe VALUES (1)').check_returncode()
        own = psql(db,'SELECT * FROM member1_probe',role,password).returncode == 0
        write = psql(db,'UPDATE member1_probe SET id=2 WHERE id=1',role,password).returncode == 0
        ddl_denied = psql(db,'CREATE TABLE forbidden (id int)',role,password).returncode != 0
        others = [other for other in ['identity','catalog','order','fulfilment'] if other != db]
        denied = all(psql(other,'SELECT 1',role,password).returncode != 0 for other in others)
        record={'role':role,'own_database_read':own,'own_database_write':write,'other_databases_denied':denied,'ddl_denied':ddl_denied}
        if not all([own,write,denied,ddl_denied]):
            raise RuntimeError(str(record))
        results.append(record)
    path = root/'output/member1/database-role-test.json'
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps({'passed':True,'environment':'disposable-local-postgres','roles':results},indent=2))
    print(path.read_text())
finally:
    run(['docker','rm','-f',name])
