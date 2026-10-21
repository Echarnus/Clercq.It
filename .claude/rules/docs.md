# Docs rules

- Every PR keeps `docs/` up to date. A change to behaviour, config, commands, architecture, infra, pipelines or the workflow updates the matching page (index: docs/README.md). New pages are linked from docs/README.md.
- The docs check (`python3 .github/scripts/docs_check.py`, the `Docs updated` CI check) fails when code changed but no doc did, when an area with its own doc changed without that doc (infra, pipelines, container image, coverage thresholds, Keycloak realm, Claude Code hooks and skills), or when a changed doc links to or names a file that does not exist.
- Use the `docs:not-needed` label only when nothing documented changed, and say why in the PR body.
- Write plainly: short sentences, commands people can copy, and file paths in backticks so the docs check can verify them.
