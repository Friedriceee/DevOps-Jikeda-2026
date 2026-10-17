import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('cd_plan', Path(__file__).parents[1] / 'cd-plan.py')
cd_plan = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cd_plan)


class ReleasePlanTests(unittest.TestCase):
    def plan(self, **values):
        return cd_plan.release_plan({'GITHUB_REPOSITORY': 'Friedriceee/DevOps-Jikeda-2026',
                                     'GITHUB_EVENT_NAME': 'workflow_run', **values})

    def test_default_only_publishes_under_this_repository(self):
        self.assertEqual(self.plan(), {'registry': 'ghcr.io/friedriceee/devops-jikeda-2026', 'target': 'none'})

    def test_two_repositories_with_same_owner_do_not_share_packages(self):
        other = self.plan(GITHUB_REPOSITORY='Friedriceee/5001')
        self.assertNotEqual(self.plan()['registry'] + '/takeout-api', other['registry'] + '/takeout-api')

    def test_automatic_staging_requires_opt_in(self):
        self.assertEqual(self.plan(STAGING_ENABLED='true')['target'], 'remote')
        self.assertEqual(self.plan(STAGING_ENABLED='false')['target'], 'none')

    def test_manual_staging_does_not_require_automatic_deployment(self):
        self.assertEqual(self.plan(GITHUB_EVENT_NAME='workflow_dispatch', DEPLOY_TO_STAGING='true')['target'], 'remote')

    def test_explicit_rehearsal_never_deploys_to_shared_staging(self):
        self.assertEqual(self.plan(GITHUB_EVENT_NAME='workflow_dispatch', EPHEMERAL_REHEARSAL='true',
                                   STAGING_ENABLED='true')['target'], 'runner')

    def test_conflicting_manual_targets_are_rejected(self):
        with self.assertRaisesRegex(ValueError, 'not both'):
            self.plan(GITHUB_EVENT_NAME='workflow_dispatch', DEPLOY_TO_STAGING='true', EPHEMERAL_REHEARSAL='true')

    def test_drill_without_target_is_rejected(self):
        with self.assertRaisesRegex(ValueError, 'explicit deployment target'):
            self.plan(GITHUB_EVENT_NAME='workflow_dispatch', ROLLBACK_DRILL='true')

    def test_workflow_run_ignores_manual_flags(self):
        self.assertEqual(self.plan(DEPLOY_TO_STAGING='true', EPHEMERAL_REHEARSAL='true')['target'], 'none')

    def test_invalid_repository_cannot_inject_an_output(self):
        with self.assertRaises(ValueError):
            self.plan(GITHUB_REPOSITORY='owner/repo\ntarget=remote')


if __name__ == '__main__':
    unittest.main()
