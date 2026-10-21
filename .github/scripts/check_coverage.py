#!/usr/bin/env python3
"""Fails CI when line coverage drops below tests/coverage-thresholds.json (issue #242).

Reads the JsonSummary report from reportgenerator and prints a Markdown table
(to stdout and, in GitHub Actions, the job summary). See docs/testing.md#coverage.

Usage: check_coverage.py <Summary.json> <coverage-thresholds.json>
"""

from __future__ import annotations

import json
import os
import sys


def evaluate(summary: dict, thresholds: dict) -> tuple[list[tuple[str, float, float | None, bool]], list[str]]:
    """Returns table rows (name, coverage, minimum, ok) and failure messages."""
    measured = {a["name"]: float(a["coverage"] or 0) for a in summary["coverage"]["assemblies"]}
    rows, failures = [], []

    total = float(summary["summary"]["linecoverage"] or 0)
    ok = total >= thresholds["total"]
    rows.append(("Total", total, thresholds["total"], ok))
    if not ok:
        failures.append(f"Total line coverage {total:.1f}% is below {thresholds['total']}%.")

    for name, minimum in sorted(thresholds["assemblies"].items()):
        if name not in measured:
            failures.append(f"{name} has a threshold but no coverage data; did its tests stop running?")
            rows.append((name, 0.0, minimum, False))
            continue
        ok = measured[name] >= minimum
        rows.append((name, measured[name], minimum, ok))
        if not ok:
            failures.append(f"{name} line coverage {measured[name]:.1f}% is below {minimum}%.")

    for name in sorted(set(measured) - set(thresholds["assemblies"])):
        failures.append(f"{name} has coverage data but no threshold; add it to tests/coverage-thresholds.json.")
        rows.append((name, measured[name], None, False))
    return rows, failures


def markdown(rows) -> str:
    lines = ["## Line coverage", "", "| Assembly | Coverage | Minimum | |", "| --- | ---: | ---: | --- |"]
    for name, coverage, minimum, ok in rows:
        shown_min = "none" if minimum is None else f"{minimum}%"
        lines.append(f"| {name} | {coverage:.1f}% | {shown_min} | {'✅' if ok else '❌'} |")
    return "\n".join(lines) + "\n"


def main(argv: list[str]) -> int:
    if len(argv) != 3:
        print(__doc__)
        return 2
    with open(argv[1], encoding="utf-8") as f:
        summary = json.load(f)
    with open(argv[2], encoding="utf-8") as f:
        thresholds = json.load(f)

    rows, failures = evaluate(summary, thresholds)
    table = markdown(rows)
    print(table)
    step_summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if step_summary:
        with open(step_summary, "a", encoding="utf-8") as f:
            f.write(table)
    for failure in failures:
        print(f"::error::{failure}")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
