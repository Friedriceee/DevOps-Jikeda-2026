#!/usr/bin/env python3
"""Validate actual service build contexts before permitting the Azure workflow."""
import json
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[2]
contract = json.loads((root/'infra/azure/service-contract.json').read_text())
missing = []
for name, spec in contract['services'].items():
    dockerfile = root/spec['context']/spec['dockerfile']
    if not dockerfile.is_file():
        missing.append(str(dockerfile.relative_to(root)))
print(json.dumps({'ready': not missing, 'missing': missing, 'contract': contract['status']}, indent=2))
sys.exit(1 if missing else 0)
