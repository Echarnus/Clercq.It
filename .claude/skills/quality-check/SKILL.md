---
name: quality-check
description: Run every gate a Clercq.It PR needs (docs check, dotnet format/build/tests with coverage, ESLint, Prettier, tsc, Vitest, next build, public bundle check, terraform fmt/validate) and report a pass/fail table. Use before opening or updating a PR, and when asked whether a change is done.
---

# Quality check

This is the definition-of-done command for this repository (issue #234). CI runs the same gates as required checks, so a green run here means the PR's checks should pass.

1. Run it from the repo root:

   ```bash
   .claude/skills/quality-check/quality-check.sh          # everything, as CI runs it
   .claude/skills/quality-check/quality-check.sh --fast   # while iterating: no integration tests, coverage gate or next build
   ```

   The API integration tests need Docker. In a cloud session start it first with `dockerd > /tmp/dockerd.log 2>&1 &`; without Docker those rows show as skipped.

2. Show the user the table the script prints.

3. For every failed row, read the log file named in the row, fix the cause, and run the check again. Formatting failures: use the `fix-quality` skill. Never skip, disable or loosen a check, test, analyzer rule or coverage threshold to get green.

4. A PR is opened only after a full run (not `--fast`) passes. If a row is skipped because a tool or Docker is missing, say so in the PR body.

The docs check (`.github/scripts/docs_check.py`) compares your branch with `origin/main`. It fails when code changed but no doc did, when an area with its own doc changed without that doc, or when a changed doc links to a file that does not exist. Details: docs/contributing.md#docs-check.
