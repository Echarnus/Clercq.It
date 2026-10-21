"""Tests for audit_repo.py (issue #245). Run: python3 -m unittest discover -s .github/scripts/tests"""

import copy
import json
import sys
import tempfile
import unittest
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / ".github/scripts"))

import audit_repo  # noqa: E402

NOW = datetime(2026, 10, 28, tzinfo=timezone.utc)
EXPECTED = audit_repo.load_expected_rulesets(ROOT)


class FakeApi:
    """Serves canned responses for a repository that follows every rule."""

    repo = "owner/repo"

    def __init__(self):
        self.rulesets = {name: dict(copy.deepcopy(r), id=i) for i, (name, r) in enumerate(EXPECTED.items(), 1)}
        self.settings = dict(audit_repo.EXPECTED_SETTINGS)
        self.branches = [{"name": "main", "commit": {"sha": "m"}}]
        self.commit_dates = {}
        self.pulls = {}
        self.tags = [{"ref": "refs/tags/prod-1.0.1", "object": {"sha": "abc1234", "type": "commit"}}]
        self.runs = [{"id": 1, "head_sha": "abc1234", "created_at": "2026-10-27T10:00:00Z", "html_url": "run/1"}]
        self.posts = []

    def get(self, path):
        if path.startswith("repos/{repo}/rulesets?"):
            return [{"name": n, "id": r["id"]} for n, r in self.rulesets.items()]
        if path.startswith("repos/{repo}/rulesets/"):
            rid = int(path.rsplit("/", 1)[1])
            return next(r for r in self.rulesets.values() if r["id"] == rid)
        if path == "repos/{repo}":
            return self.settings
        if path.startswith("repos/{repo}/commits/"):
            sha = path.rsplit("/", 1)[1]
            return {"commit": {"committer": {"date": self.commit_dates[sha]}}}
        if path.startswith("repos/{repo}/pulls?"):
            head = path.split("head=owner%3A", 1)[1]
            return self.pulls.get(head, [])
        if path == "repos/{repo}/git/matching-refs/tags/prod-":
            return self.tags
        if path.startswith("repos/{repo}/actions/workflows/deploy.yml/runs"):
            return {"workflow_runs": self.runs}
        if path.startswith("repos/{repo}/actions/runs/"):
            return {"jobs": [{"name": audit_repo.DEPLOY_JOB, "conclusion": "success"}]}
        raise AssertionError(f"unexpected GET {path}")

    def get_all(self, path):
        if path == "repos/{repo}/branches":
            return self.branches
        if path.startswith("repos/{repo}/issues"):
            return []
        raise AssertionError(f"unexpected GET {path}")

    def post(self, path, body):
        self.posts.append((path, body))

    def add_branch(self, name, date, pulls=()):
        sha = f"sha{len(self.branches)}"
        self.branches.append({"name": name, "commit": {"sha": sha}})
        self.commit_dates[sha] = date
        self.pulls[name.replace("/", "%2F")] = list(pulls)


def audit(api, root=ROOT):
    return (audit_repo.check_rulesets(api, EXPECTED) + audit_repo.check_settings(api)
            + audit_repo.check_branches(api, NOW) + audit_repo.check_deploy_tags(api, NOW))


class RulesetTests(unittest.TestCase):
    def test_compliant_repository_has_no_findings(self):
        self.assertEqual(audit(FakeApi()), [])

    def test_missing_ruleset_is_drift(self):
        api = FakeApi()
        del api.rulesets["main"]
        self.assertIn("Ruleset `main` is missing", " ".join(audit(api)))

    def test_removed_rule_is_drift(self):
        api = FakeApi()
        api.rulesets["main"]["rules"] = [r for r in api.rulesets["main"]["rules"] if r["type"] != "non_fast_forward"]
        self.assertIn("missing the `non_fast_forward` rule", " ".join(audit(api)))

    def test_removed_required_check_is_drift(self):
        api = FakeApi()
        checks = next(r for r in api.rulesets["main"]["rules"] if r["type"] == "required_status_checks")
        checks["parameters"]["required_status_checks"] = checks["parameters"]["required_status_checks"][1:]
        self.assertIn("`required_status_checks` is", " ".join(audit(api)))

    def test_disabled_ruleset_is_drift(self):
        api = FakeApi()
        api.rulesets["main"]["enforcement"] = "disabled"
        self.assertIn("enforcement is `disabled`", " ".join(audit(api)))

    def test_extra_bypass_actor_is_drift(self):
        api = FakeApi()
        api.rulesets["main"]["bypass_actors"] = [{"actor_id": 5, "actor_type": "RepositoryRole", "bypass_mode": "always"}]
        self.assertIn("bypass list differs", " ".join(audit(api)))

    def test_main_requires_pull_requests_without_approvals(self):
        rules = audit_repo.normalise_rules(EXPECTED["main"]["rules"])
        self.assertEqual(rules["pull_request"]["required_approving_review_count"], 0)
        self.assertIn("PR conventions", rules["required_status_checks"]["required_status_checks"])


class SettingsTests(unittest.TestCase):
    def test_wrong_setting_is_drift(self):
        api = FakeApi()
        api.settings["delete_branch_on_merge"] = False
        self.assertIn("`delete_branch_on_merge` is `false`", " ".join(audit(api)))

    def test_hidden_setting_is_skipped(self):
        api = FakeApi()
        del api.settings["allow_auto_merge"]
        self.assertEqual(audit(api), [])


class BranchTests(unittest.TestCase):
    def test_release_branch_is_drift(self):
        api = FakeApi()
        api.add_branch("release/1.2", "2026-10-27T00:00:00Z")
        self.assertIn("`release/1.2` exists", " ".join(audit(api)))

    def test_fresh_branch_is_fine(self):
        api = FakeApi()
        api.add_branch("fix/1-a", "2026-10-25T00:00:00Z")
        self.assertEqual(audit(api), [])

    def test_old_branch_with_open_pr_is_drift(self):
        api = FakeApi()
        api.add_branch("fix/1-a", "2026-10-01T00:00:00Z", [{"state": "open"}])
        self.assertIn("`fix/1-a` is 27 days old with open PR", " ".join(audit(api)))

    def test_old_branch_without_pr_is_drift(self):
        api = FakeApi()
        api.add_branch("fix/1-a", "2026-10-01T00:00:00Z")
        self.assertIn("with no PR", " ".join(audit(api)))

    def test_old_branch_with_finished_pr_is_fine(self):
        api = FakeApi()
        api.add_branch("fix/1-a", "2026-10-01T00:00:00Z", [{"state": "closed"}])
        self.assertEqual(audit(api), [])


class DeployTagTests(unittest.TestCase):
    def test_untagged_recent_deploy_is_drift(self):
        api = FakeApi()
        api.runs.append({"id": 2, "head_sha": "def5678", "created_at": "2026-10-27T12:00:00Z", "html_url": "run/2"})
        self.assertIn("`def5678`", " ".join(audit(api)))

    def test_old_untagged_deploy_is_ignored(self):
        api = FakeApi()
        api.runs.append({"id": 2, "head_sha": "def5678", "created_at": "2026-09-01T12:00:00Z", "html_url": "run/2"})
        self.assertEqual(audit(api), [])


class DocsTests(unittest.TestCase):
    def test_repository_docs_have_no_dead_references(self):
        self.assertEqual(audit_repo.check_docs(ROOT), [])

    def test_dead_link_and_path_are_drift(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "docs").mkdir()
            (root / "src").mkdir()
            (root / "src/real.cs").write_text("")
            (root / "docs/a.md").write_text(
                "[ok](./b.md) [gone](./gone.md) [web](https://x.y) [anchor](#top)\n"
                "`src/real.cs` `src/missing.cs` `src/<Project>/x.cs` `not/a/root.cs`\n")
            (root / "docs/b.md").write_text("")
            findings = audit_repo.check_docs(root)
        self.assertEqual(findings, [
            "`docs/a.md` links to `./gone.md`, which does not exist.",
            "`docs/a.md` names `src/missing.cs`, which does not exist.",
        ])


class ReportTests(unittest.TestCase):
    def test_drift_opens_a_p1_bug(self):
        api = FakeApi()
        audit_repo.report(api, ["Ruleset `main` is missing"], "run/9")
        path, body = api.posts[0]
        self.assertEqual(path, "repos/{repo}/issues")
        self.assertEqual(body["title"], audit_repo.ISSUE_TITLE)
        self.assertIn("priority:p1", body["labels"])
        self.assertIn("type:bug", body["labels"])


if __name__ == "__main__":
    unittest.main()
