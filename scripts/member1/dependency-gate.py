#!/usr/bin/env python3
"""Fail closed on High/Critical findings in dotnet list package --format json."""
import json
import sys
from pathlib import Path

def findings(node):
    if isinstance(node, dict):
        if str(node.get('severity', '')).lower() in ('high', 'critical'):
            yield node
        for value in node.values():
            yield from findings(value)
    elif isinstance(node, list):
        for item in node:
            yield from findings(item)

try:
    report = json.loads(Path(sys.argv[1]).read_text())
    if report.get('version') != 1 or not isinstance(report.get('projects'), list) or not report['projects']:
        raise ValueError('Expected version 1 dotnet vulnerability report with projects')
    if report.get('errors') or any(p.get('errors') for p in report['projects']):
        raise ValueError('Vulnerability report contains restore/scan errors')
    issues = list(findings(report))
    print(f'.NET dependency gate: {len(issues)} High/Critical findings')
    for issue in issues:
        print(issue)
    sys.exit(1 if issues else 0)
except (ValueError, OSError, IndexError) as exc:
    print(f'.NET dependency gate failed: {exc}', file=sys.stderr)
    sys.exit(2)
