#!/usr/bin/env python3
"""Wait for CI, CodeQL and filesystem scanning at the exact release SHA."""
import argparse
import json
import os
import time
import urllib.request

def latest_checks(runs, sha):
    latest = {}
    for run in sorted(runs, key=lambda r: r['id'], reverse=True):
        if run['head_sha'] == sha:
            latest.setdefault(run['name'], run)
    return latest

def main():
    p = argparse.ArgumentParser()
    p.add_argument('sha')
    p.add_argument('--wait', type=int, default=600)
    args = p.parse_args()
    token = os.environ.get('GH_TOKEN', '')
    repo = os.environ['GITHUB_REPOSITORY']
    def get(path):
        headers = {'Accept':'application/vnd.github+json','User-Agent':'jikeda-release-gate'}
        if token:
            headers['Authorization'] = 'Bearer ' + token
        with urllib.request.urlopen(urllib.request.Request(f'https://api.github.com/repos/{repo}/{path}', headers=headers), timeout=30) as response:
            return json.load(response)
    sha = get('commits/' + args.sha)['sha']
    deadline = time.monotonic() + args.wait
    required = ['CI', 'CodeQL', 'Trivy security scan']
    while True:
        latest = latest_checks(get(f'actions/runs?head_sha={sha}&per_page=100')['workflow_runs'], sha)
        states = {name: latest.get(name,{}).get('conclusion') for name in required}
        print(json.dumps({'sha':sha,'required':states}), flush=True)
        if all(value == 'success' for value in states.values()):
            return
        if any(value in ('failure','cancelled','timed_out','action_required') for value in states.values()):
            raise SystemExit('Release blocked by failed required workflow')
        if time.monotonic() >= deadline:
            raise SystemExit('Release blocked: required workflow missing or not complete')
        time.sleep(15)

if __name__ == '__main__':
    main()
