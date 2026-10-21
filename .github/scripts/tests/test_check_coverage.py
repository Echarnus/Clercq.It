"""Tests for check_coverage.py (issue #242)."""

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import check_coverage  # noqa: E402


def summary(total, **assemblies):
    return {
        "summary": {"linecoverage": total},
        "coverage": {"assemblies": [{"name": n, "coverage": c} for n, c in assemblies.items()]},
    }


THRESHOLDS = {"total": 70, "assemblies": {"Domain": 90, "Api": 80}}


class CheckCoverageTests(unittest.TestCase):
    def test_at_or_above_thresholds_passes(self):
        _, failures = check_coverage.evaluate(summary(70, Domain=90, Api=85.5), THRESHOLDS)
        self.assertEqual(failures, [])

    def test_assembly_below_threshold_fails(self):
        _, failures = check_coverage.evaluate(summary(75, Domain=89.9, Api=85), THRESHOLDS)
        self.assertEqual(failures, ["Domain line coverage 89.9% is below 90%."])

    def test_total_below_threshold_fails(self):
        _, failures = check_coverage.evaluate(summary(69, Domain=95, Api=85), THRESHOLDS)
        self.assertEqual(failures, ["Total line coverage 69.0% is below 70%."])

    def test_missing_assembly_fails(self):
        _, failures = check_coverage.evaluate(summary(75, Domain=95), THRESHOLDS)
        self.assertIn("Api has a threshold but no coverage data", failures[0])

    def test_new_assembly_without_threshold_fails(self):
        _, failures = check_coverage.evaluate(summary(75, Domain=95, Api=85, New=10), THRESHOLDS)
        self.assertIn("New has coverage data but no threshold", failures[0])

    def test_markdown_lists_every_row(self):
        rows, _ = check_coverage.evaluate(summary(75, Domain=95, Api=70), THRESHOLDS)
        table = check_coverage.markdown(rows)
        self.assertIn("| Domain | 95.0% | 90% | ✅ |", table)
        self.assertIn("| Api | 70.0% | 80% | ❌ |", table)

    def test_repository_thresholds_cover_total_and_assemblies(self):
        import json
        root = Path(__file__).resolve().parents[3]
        thresholds = json.loads((root / "tests/coverage-thresholds.json").read_text(encoding="utf-8"))
        self.assertIn("total", thresholds)
        self.assertIn("Clercq.It.Domain", thresholds["assemblies"])
        self.assertIn("Clercq.It.Application", thresholds["assemblies"])


if __name__ == "__main__":
    unittest.main()
