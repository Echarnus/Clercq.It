#!/usr/bin/env python3
"""Docs check (issues #234 and #245): does a change keep docs/ up to date?

Fails when:

- code, infra, tests, workflows or the Claude Code setup changed, but no doc did
  (docs/, CLAUDE.md or README.md);
- a changed file belongs to an area with its own doc (AREAS below), and none of
  that area's docs changed;
- a changed doc links to, or names in backticks, a repository file that does
  not exist.

The `docs:not-needed` label (`--docs-not-needed`) skips the first two checks,
never the third. CI runs it in docs-check.yml; agents run it through the
quality-check skill. See docs/contributing.md#docs-check.

Usage:
  docs_check.py [--base REF] [--head REF] [--docs-not-needed]

Without --head it checks the working tree against the merge base with --base
(default origin/main), untracked files included.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from audit_repo import check_doc  # noqa: E402

NEEDS_DOCS = re.compile(r"^(src/|infra/|tests/|\.github/(workflows|actions)/|\.claude/)")
IS_DOC = re.compile(r"^(docs/.*\.md|CLAUDE\.md|README\.md)$")

# (area, changed paths, docs of which at least one must change). Only areas
# whose docs describe the files themselves (config, pipelines, infra), so a
# refactor of application code does not need a docs change of its own.
AREAS = [
    ("infrastructure", r"^infra/", ["docs/devops.md", "docs/secrets.md"]),
    ("pipelines", r"^\.github/(workflows|actions|rulesets)/",
     ["docs/devops.md", "docs/contributing.md", "docs/application-security.md", "docs/testing.md",
      "docs/versioning.md"]),
    ("Keycloak realm", r"^src/Clercq\.It\.AppHost/KeycloakRealms/", ["docs/ciam.md", "docs/development.md"]),
    ("container image", r"^src/(Dockerfile|start\.sh|nginx\.conf)$",
     ["docs/architecture.md", "docs/devops.md", "docs/application-security.md"]),
    ("coverage thresholds", r"^(tests/coverage-thresholds\.json|src/ClercqIt\.Web/vitest\.config\.mts)$",
     ["docs/testing.md"]),
    ("Claude Code hooks and skills", r"^\.claude/(hooks|skills)/|^\.claude/settings\.json$",
     ["docs/development.md", "CLAUDE.md"]),
]


def git(root: Path, *args: str) -> list[str]:
    out = subprocess.run(["git", *args], cwd=root, check=True, capture_output=True, text=True).stdout
    return [line for line in out.splitlines() if line]


def changed_files(root: Path, base: str, head: str | None) -> list[str]:
    if head:
        return git(root, "diff", "--name-only", f"{base}...{head}")
    merge_base = git(root, "merge-base", base, "HEAD")[0]
    return sorted(set(git(root, "diff", "--name-only", merge_base))
                  | set(git(root, "ls-files", "--others", "--exclude-standard")))


def check(root: Path, changed: list[str], docs_not_needed: bool) -> list[str]:
    findings = []
    docs = {f for f in changed if IS_DOC.match(f)}

    if not docs_not_needed:
        if any(NEEDS_DOCS.match(f) for f in changed) and not docs:
            findings.append("Code, infra, tests or workflows changed, but no file under docs/ "
                            "(or CLAUDE.md/README.md).")
        for area, pattern, area_docs in AREAS:
            hits = [f for f in changed if re.match(pattern, f)]
            # A change to the area's own doc (e.g. CLAUDE.md) counts as documenting it.
            if hits and not docs.intersection(area_docs) and not set(hits) <= set(area_docs):
                findings.append(f"{area} changed ({', '.join(hits[:3])}{', ...' if len(hits) > 3 else ''}), "
                                f"but none of {', '.join(area_docs)} did.")

    for doc in sorted(docs):
        if (root / doc).exists():
            findings.extend(check_doc(root, root / doc))
    return findings


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--base", default="origin/main")
    parser.add_argument("--head")
    parser.add_argument("--docs-not-needed", action="store_true")
    args = parser.parse_args()

    root = Path(git(Path.cwd(), "rev-parse", "--show-toplevel")[0])
    changed = changed_files(root, args.base, args.head)
    print("Changed files:\n" + "\n".join(f"  {f}" for f in changed))

    findings = check(root, changed, args.docs_not_needed)
    if not findings:
        print("Docs check passed.")
        return 0
    for finding in findings:
        print(f"::error::{finding}")
    print("Update the matching docs page, or add the docs:not-needed label with a reason in the PR body "
          "(broken links must be fixed either way).")
    return 1


if __name__ == "__main__":
    sys.exit(main())
