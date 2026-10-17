#!/usr/bin/env python3
"""Run actual test projects; an API-only csproj must not count as tested."""
import json
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[2]
service=sys.argv[1]
contract=json.loads((root/'infra/azure/service-contract.json').read_text())
context=root/contract['services'][service]['context']
tests=[p for p in context.rglob('*.csproj') if 'Microsoft.NET.Test.Sdk' in p.read_text() or '<IsTestProject>true</IsTestProject>' in p.read_text()]
if not tests:
    raise SystemExit(f'Release blocked: no test project inside {context.relative_to(root)}')
output=root/'output/member1/service-tests'/service
output.mkdir(parents=True,exist_ok=True)
for project in tests:
    name=project.stem+'.trx'
    subprocess.run(['dotnet','test',str(project),'-c','Release','--results-directory',str(output),'--logger',f'trx;LogFileName={name}'],check=True)
    tree=ET.parse(output/name)
    counters=tree.find('.//{*}Counters').attrib
    if int(counters.get('executed',0))<1 or int(counters.get('failed',0)) or int(counters.get('notExecuted',0)):
        raise SystemExit('Release blocked: empty, failed or skipped test execution')
