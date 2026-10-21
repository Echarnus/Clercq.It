"""Tests for docs_check.py (issue #234). Run: python3 -m unittest discover -s .github/scripts/tests"""

import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / ".github/scripts"))

import docs_check  # noqa: E402


class DocsCheckTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.root = Path(self._tmp.name)
        (self.root / "docs").mkdir()
        (self.root / "src").mkdir()
        (self.root / "src/Program.cs").write_text("", encoding="utf-8")
        for doc in ("devops.md", "testing.md", "development.md"):
            (self.root / "docs" / doc).write_text("# Doc\n", encoding="utf-8")

    def tearDown(self):
        self._tmp.cleanup()

    def check(self, changed, docs_not_needed=False):
        return docs_check.check(self.root, changed, docs_not_needed)

    def test_check_CodeWithoutDocs_Fails(self):
        findings = self.check(["src/ClercqIt.Api/Program.cs"])

        self.assertEqual(len(findings), 1)
        self.assertIn("no file under docs/", findings[0])

    def test_check_CodeWithAnyDoc_Passes(self):
        self.assertEqual(self.check(["src/ClercqIt.Api/Program.cs", "docs/testing.md"]), [])

    def test_check_OnlyDocsOrRootFiles_Passes(self):
        self.assertEqual(self.check(["docs/testing.md"]), [])
        self.assertEqual(self.check(["package.json"]), [])

    def test_check_AreaWithoutItsDoc_Fails(self):
        findings = self.check(["infra/terraform/main.tf", "docs/testing.md"])

        self.assertEqual(len(findings), 1)
        self.assertIn("infrastructure changed (infra/terraform/main.tf)", findings[0])
        self.assertIn("docs/devops.md", findings[0])

    def test_check_AreaWithItsDoc_Passes(self):
        self.assertEqual(self.check(["infra/terraform/main.tf", "docs/devops.md"]), [])

    def test_check_CoverageThresholdsWithoutTestingDoc_Fails(self):
        findings = self.check(["tests/coverage-thresholds.json", "docs/devops.md"])

        self.assertEqual(len(findings), 1)
        self.assertIn("coverage thresholds", findings[0])

    def test_check_ClaudeHookWithClaudeMd_Passes(self):
        self.assertEqual(self.check([".claude/hooks/x.sh", "CLAUDE.md"]), [])

    def test_check_DocsNotNeeded_SkipsDocRequirements(self):
        self.assertEqual(self.check(["infra/terraform/main.tf", "src/Dockerfile"], docs_not_needed=True), [])

    def test_check_ChangedDocWithBrokenLink_FailsEvenWithLabel(self):
        (self.root / "docs/devops.md").write_text("See [x](./missing.md) and `src/Gone.cs`.\n", encoding="utf-8")

        findings = self.check(["docs/devops.md"], docs_not_needed=True)

        self.assertEqual(len(findings), 2)
        self.assertIn("links to `./missing.md`", findings[0])
        self.assertIn("names `src/Gone.cs`", findings[1])

    def test_check_ChangedDocWithValidLinks_Passes(self):
        (self.root / "docs/devops.md").write_text(
            "See [testing](./testing.md#x), [web](https://example.com) and `src/Program.cs`.\n", encoding="utf-8")

        self.assertEqual(self.check(["docs/devops.md"]), [])

    def test_check_DeletedDoc_IsIgnored(self):
        self.assertEqual(self.check(["docs/removed.md"]), [])


if __name__ == "__main__":
    unittest.main()
