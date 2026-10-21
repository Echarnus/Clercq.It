# Contributing

All work on Clercq.It runs through GitHub and is done by agents: an issue describes the change, an agent builds it on a branch, and the pull request merges and deploys to production as soon as the required checks pass. There are no human checks and no review approval in this loop (owner decision, 21 October 2026). A reviewer agent may be added later (issue #251).

The owner only steps in for things an agent cannot do: console logins, accounts, credentials and anything that costs money. Those issues get `status:needs-human`.

## The loop

1. **Issue.** Every change starts as an issue created from a template in `.github/ISSUE_TEMPLATE/` (feature, bug, infra change, content). It has a `type:`, `area:` and `priority:` label, acceptance criteria and a "Docs to update" section. Ready work is labelled `status:ready`.
2. **Branch.** The agent picking it up sets `status:in-progress` and creates a branch from `main` named `<type>/<issue>-<slug>`, for example `fix/231-admin-login` or `chore/225-agent-workflow`. Allowed types: `feat`, `fix`, `chore`, `infra`, `content`, `docs`, `security`.
3. **Pull request.** The PR uses `.github/PULL_REQUEST_TEMPLATE.md`, starts with `Closes #<issue>`, lists the docs it changed and how it was tested. The agent enables auto-merge (squash) on it right away. The issue moves to `status:needs-review`.
4. **Checks.** The required checks (tests and docs check) must be green. The building agent fixes failures and pushes again.
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

Every issue keeps `docs/` up to date. A PR that changes behaviour, configuration, infrastructure, pipelines or this workflow updates the matching page in the same PR. `docs-check.yml` enforces this: it fails a PR that touches `src/`, `tests/`, `infra/`, `.github/workflows/`, `.github/actions/` or `.claude/` without touching `docs/`, `CLAUDE.md` or `README.md`, unless it has the `docs:not-needed` label.

New pages are added to the index in [docs/README.md](./README.md).

## Branch protection for `main`

The owner switches this on once under Settings → Rules (it will move to rulesets-as-code in issue #245):

- Require a pull request before merging, with **0** required approvals.
- Require status checks to pass: `Test .NET API`, `Test Next.js Frontend`, `Docs updated`, and require branches to be up to date.
- Block force pushes and deletion.
- Under Settings → General: allow auto-merge, allow squash merging, and automatically delete head branches.

Required status checks are essential: without them, auto-merge merges a PR immediately, before any test has run.
