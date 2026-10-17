#!/usr/bin/env python3
"""Choose this repository's image namespace and an explicitly requested target."""
import os
import re
from pathlib import Path


def release_plan(env):
    repository = env['GITHUB_REPOSITORY'].lower()
    if not re.fullmatch(r'[a-z0-9_.-]+/[a-z0-9_.-]+', repository):
        raise ValueError('GITHUB_REPOSITORY must be owner/repository')
    manual = env.get('GITHUB_EVENT_NAME') == 'workflow_dispatch'
    staging = manual and env.get('DEPLOY_TO_STAGING') == 'true'
    rehearsal = manual and env.get('EPHEMERAL_REHEARSAL') == 'true'
    drill = manual and env.get('ROLLBACK_DRILL') == 'true'
    if staging and rehearsal:
        raise ValueError('Choose persistent staging or ephemeral rehearsal, not both')
    if drill and not (staging or rehearsal or env.get('STAGING_ENABLED') == 'true'):
        raise ValueError('A rollback drill requires an explicit deployment target')
    target = 'runner' if rehearsal else 'remote' if staging or env.get('STAGING_ENABLED') == 'true' else 'none'
    return {'registry': 'ghcr.io/' + repository, 'target': target}


def main():
    plan = release_plan(os.environ)
    output = ''.join(f'{key}={value}\n' for key, value in plan.items())
    if os.environ.get('GITHUB_OUTPUT'):
        with Path(os.environ['GITHUB_OUTPUT']).open('a') as stream:
            stream.write(output)
    print(output, end='')


if __name__ == '__main__':
    main()
