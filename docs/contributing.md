# Contributing

All work on Clercq.It runs through GitHub and is done by agents: an issue describes the change, an agent builds it on a branch, and the pull request merges and deploys to production as soon as the required checks pass. There are no human checks and no review approval in this loop (owner decision, 21 October 2026). A reviewer agent may be added later (issue #251).

The owner only steps in for things an agent cannot do: console logins, accounts, credentials and anything that costs money. Those issues get `status:needs-human`.

## The loop

1. **Issue.** Every change starts as an issue created from a template in `.github/ISSUE_TEMPLATE/` (feature, bug, infra change, content). It has a `type:`, `area:` and `priority:` label, acceptance criteria and a "Docs to update" section. Ready work is labelled `status:ready`.
2. **Branch.** The agent picking it up sets `status:in-progress` and creates a branch from `main` named `<type>/<issue>-<slug>`, for example `fix/231-admin-login` or `chore/225-agent-workflow`. Allowed types: `feat`, `fix`, `chore`, `infra`, `content`, `docs`, `security`.
3. **Pull request.** The PR uses `.github/PULL_REQUEST_TEMPLATE.md`, starts with `Closes #<issue>`, lists the docs it changed and how it was tested. The agent enables auto-merge (squash) on it right away. The issue moves to `status:needs-review`.
4. **Checks.** The required checks (see [Enforced rules](#enforced-rules)) must be green. The building agent fixes failures and pushes again.
5. **Merge and deploy.** Once green, GitHub merges the PR and deletes the branch. The merge to `main` runs `build`, `Deploy to Scaleway` and `Deploy Infra`.

Auto-merge is enabled by the agent with its own GitHub credentials, not by a workflow: a merge made with the workflow `GITHUB_TOKEN` would not trigger the deploy pipeline.

Branches are short-lived (hours to days). See [Versioning](./versioning.md) for how versions are calculated.

## Labels

Labels are defined in `.github/labels.yml` and synced by `.github/workflows/labels.yml`.

| Group | Labels | Meaning |
| --- | --- | --- |
| type | `type:feature`, `type:bug`, `type:chore`, `type:infra`, `type:content`, `type:docs`, `type:security` | What kind of change |
| area | `area:web`, `area:api`, `area:auth`, `area:infra`, `area:ci` | Which part of the system |
| priority | `priority:p1`, `priority:p2`, `priority:p3` | Do next, soon, later |
| status | `status:ready`, `status:in-progress`, `status:needs-review` (PR open, waiting on checks), `status:blocked`, `status:needs-human` | Where the issue is in the loop |
| docs | `docs:not-needed` | PR overrides the docs check; reason in the PR body |

## Docs rule

Every issue keeps `docs/` up to date. A PR that changes behaviour, configuration, infrastructure, pipelines or this workflow updates the matching page in the same PR.

### Docs check

`docs-check.yml` (the `Docs updated` check) runs `.github/scripts/docs_check.py` on every PR except Dependabot's. Agents run the same script through the `quality-check` skill. It fails when:

1. The PR touches `src/`, `tests/`, `infra/`, `.github/workflows/`, `.github/actions/` or `.claude/`, but no file under `docs/`, `CLAUDE.md` or `README.md`.
2. An area with its own doc changed, but none of that area's docs did:

   | Area | Docs (one of them must change) |
   | --- | --- |
   | `infra/` | `docs/devops.md`, `docs/secrets.md` |
   | `.github/workflows/`, `.github/actions/`, `.github/rulesets/` | `docs/devops.md`, `docs/contributing.md`, `docs/application-security.md`, `docs/testing.md`, `docs/versioning.md` |
   | Keycloak realm (`src/Clercq.It.AppHost/KeycloakRealms/`) | `docs/ciam.md`, `docs/development.md` |
   | Container image (`src/Dockerfile`, `src/start.sh`, `src/nginx.conf`) | `docs/architecture.md`, `docs/devops.md`, `docs/application-security.md` |
   | Coverage thresholds (`tests/coverage-thresholds.json`, `src/ClercqIt.Web/vitest.config.mts`) | `docs/testing.md` |
   | Claude Code hooks and skills (`.claude/hooks/`, `.claude/skills/`, `.claude/settings.json`) | `docs/development.md`, `CLAUDE.md` |

   Application code has no mapping of its own, so a refactor only needs rule 1.
3. A doc the PR changed links to, or names in backticks, a repository file that does not exist (the same check as the weekly audit, but only on the changed docs).

The `docs:not-needed` label skips rules 1 and 2, never rule 3; the PR body must say why. To add an area, extend `AREAS` in the script, its tests in `.github/scripts/tests/test_docs_check.py`, and the table above.

New pages are added to the index in [docs/README.md](./README.md).

## Enforced rules

GitHub enforces the trunk-based model (issue #245). The rules live as code in `.github/rulesets/` and a weekly audit checks they are still in place.

### Live status

Checked on 2026-10-24 with `gh api repos/Echarnus/Clercq.It/rulesets` and `gh api repos/Echarnus/Clercq.It/rules/branches/main` (issue #433):

| Rule | Status |
| --- | --- |
| Ruleset `main` | **Active** since 2026-10-22 (id 25640431). Matches `main.json`: all 13 required checks, strict, squash only, no bypass actors. `main` is protected. |
| Ruleset `prod-tags` | **Not applied.** Anyone with write access, and any workflow with `contents: write`, can create, move or delete `prod-*` and `v*` tags. |
| Repository settings | Applied: auto-merge on, squash only, head branches deleted after merge. |

To apply `prod-tags`, an admin runs this once from the repository root (agents are not allowed to change repository rules):

```bash
gh api -X POST repos/Echarnus/Clercq.It/rulesets --input .github/rulesets/prod-tags.json
```

### Ruleset `main` (`.github/rulesets/main.json`)

| Rule | Setting |
| --- | --- |
| Pull request required | Yes, **0** approvals (no human review; a reviewer agent may be added in #251), stale approvals dismissed, squash merge only |
| Required status checks | `Test .NET API`, `Test Next.js Frontend`, `Build Docker image (no push)`, `Container smoke test`, `Test deploy scripts`, `Docs updated`, `PR conventions`, `Semgrep`, `gitleaks`, `CodeQL gate`, `.NET Security Scan`, `npm Security Audit`, `Trivy Container Scan`, `Trivy Filesystem Scan` |
| Branch up to date | Required before merging (strict checks) |
| Linear history | Required |
| Force push and deletion | Blocked |
| Bypass | Nobody, including admins |

A direct push to `main` is rejected. Required status checks are essential: without them, auto-merge merges a PR immediately, before any test has run. When you add a check that must block merging, add its job name to `main.json` in the same PR.

### Ruleset `prod-tags` (`.github/rulesets/prod-tags.json`)

Tags `prod-*` and `v*` can only be created by GitHub Actions (the release job in `deploy.yml`). Nobody can update or delete them.

### Repository settings

Squash merge only (merge commits and rebase merges off), auto-merge allowed, head branches deleted after merge.

### PR conventions check

`.github/workflows/pr-conventions.yml` runs `.github/scripts/check-pr-conventions.sh` on every PR:

- The head branch matches `<type>/<issue>-<slug>`: type is one of `feat`, `fix`, `chore`, `infra`, `content`, `docs`, `security`, and the slug is lowercase kebab-case. Bot branches (`dependabot/`, `renovate/`) are exempt.
- The body links its issue (`Closes #<n>`; `Fixes` and `Resolves` also work). Bot PRs are exempt.

### Weekly audit

`.github/workflows/repo-audit.yml` runs `.github/scripts/audit_repo.py` every Monday at 06:00 UTC (and on demand). It opens a `type:bug` `priority:p1` issue titled "Repository audit: drift from the trunk-based rules", or comments on the open one, when it finds:

- a ruleset missing, disabled or different from its JSON in `.github/rulesets/` (rules, required checks, refs, bypass list);
- a repository setting above changed;
- a `release/*`, `develop`, `development` or `master` branch;
- a branch older than 7 days that has an open PR or no PR (short-lived branches only);
- a production deploy from `main` in the last 14 days without a `prod-<version>` tag;
- a page in `docs/`, `CLAUDE.md` or `README.md` that links to, or names in backticks, a repository file that no longer exists.

Run it locally with `GITHUB_REPOSITORY=Echarnus/Clercq.It python3 .github/scripts/audit_repo.py --dry-run` (needs `gh` signed in). Its tests run in the `Test deploy scripts` job.

### Changing a rule

Edit the JSON in `.github/rulesets/` and this section in one PR. After it merges, an admin applies it:

```bash
# Look up the ruleset id, then update it from the file
gh api repos/Echarnus/Clercq.It/rulesets --jq '.[] | "\(.id) \(.name)"'
gh api -X PUT repos/Echarnus/Clercq.It/rulesets/<id> --input .github/rulesets/main.json
# A new ruleset
gh api -X POST repos/Echarnus/Clercq.It/rulesets --input .github/rulesets/<name>.json
```

Until it is applied, the next audit reports the difference.
