# Contributing

All work on Clercq.It runs through GitHub and is done by agents: an issue describes the change, an agent builds it on a branch, a reviewer agent approves or rejects the pull request, and an approved PR merges and deploys to production. There are no human checks in this loop (owner decision, 21 October 2026).

The owner only steps in for things an agent cannot do: console logins, accounts, credentials and anything that costs money. Those issues get `status:needs-human`.

## The loop

1. **Issue.** Every change starts as an issue created from a template in `.github/ISSUE_TEMPLATE/` (feature, bug, infra change, content). It has a `type:`, `area:` and `priority:` label, acceptance criteria and a "Docs to update" section. Ready work is labelled `status:ready`.
2. **Branch.** The agent picking it up sets `status:in-progress` and creates a branch from `main` named `<type>/<issue>-<slug>`, for example `fix/231-admin-login` or `chore/225-agent-workflow`. Allowed types: `feat`, `fix`, `chore`, `infra`, `content`, `docs`, `security`.
3. **Pull request.** The PR uses `.github/PULL_REQUEST_TEMPLATE.md`, starts with `Closes #<issue>`, lists the docs it changed and how it was tested. The issue moves to `status:needs-review`.
4. **Checks.** `test`, `docs-check` and the other required workflows must be green. The building agent fixes failures and pushes again.
5. **Review.** The reviewer agent (`review.yml`) approves or requests changes. On a change request the building agent addresses it and pushes; the review runs again.
6. **Merge and deploy.** An approved, green PR is merged with auto-merge (squash). The merge to `main` runs `build`, `Deploy to Scaleway` and `Deploy Infra`.

Branches are short-lived (hours to days). See [Versioning](./versioning.md) for how versions are calculated.

## Labels

Labels are defined in `.github/labels.yml` and synced by `.github/workflows/labels.yml`.

| Group | Labels | Meaning |
| --- | --- | --- |
| type | `type:feature`, `type:bug`, `type:chore`, `type:infra`, `type:content`, `type:docs`, `type:security` | What kind of change |
| area | `area:web`, `area:api`, `area:auth`, `area:infra`, `area:ci` | Which part of the system |
| priority | `priority:p1`, `priority:p2`, `priority:p3` | Do next, soon, later |
| status | `status:ready`, `status:in-progress`, `status:needs-review`, `status:blocked`, `status:needs-human` | Where the issue is in the loop |
| docs | `docs:not-needed` | PR overrides the docs check; reason in the PR body |

## Docs rule

Every issue keeps `docs/` up to date. A PR that changes behaviour, configuration, infrastructure, pipelines or this workflow updates the matching page in the same PR. This is enforced twice:

- `docs-check.yml` fails a PR that touches `src/`, `tests/`, `infra/`, `.github/workflows/`, `.github/actions/` or `.claude/` without touching `docs/`, `CLAUDE.md` or `README.md`, unless it has the `docs:not-needed` label.
- The reviewer agent requests changes when docs are missing, incomplete or contradict the code.

New pages are added to the index in [docs/README.md](./README.md).

## Reviewer agent setup

The reviewer runs `anthropics/claude-code-action` and posts as the Claude GitHub app, a different identity from the account that opens PRs (GitHub never lets an author approve their own PR).

It needs, once, by the owner:

1. The [Claude GitHub app](https://github.com/apps/claude) installed on the repository.
2. A repository secret `ANTHROPIC_API_KEY`. Once Scaleway Secret Manager is in place (issue #226) the value is kept there as the source of truth and copied into GitHub.

Dependabot PRs do not receive repository secrets, so the reviewer skips them.

## Branch protection for `main`

The owner switches this on once under Settings → Rules (it will move to rulesets-as-code in issue #245):

- Require a pull request before merging, with 1 approving review.
- Dismiss stale approvals when new commits are pushed.
- Require status checks: `Test .NET API`, `Test Next.js Frontend`, `Docs updated`.
- Block force pushes and deletion.
- Allow auto-merge (Settings → General), and automatically delete head branches.

Whether an approval from the Claude app counts toward the required review must be confirmed on the first PR after setup.
