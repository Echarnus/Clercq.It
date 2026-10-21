# Clercq.It agent guide

Personal website www.clercq.it: .NET 9 API (Clean Architecture, EF Core, PostgreSQL) and Next.js 15 frontend, shipped as one Docker image to a Scaleway serverless container. Production auth is Cloud IAM (hosted Keycloak). Full docs: [docs/README.md](docs/README.md).

## How work flows

Everything goes issue → branch → PR → green checks → auto-merge → production. No human reviews and no review approval required. Details: [docs/contributing.md](docs/contributing.md).

- Work only on issues labelled `status:ready`; set `status:in-progress` when you start.
- Branch from `main` as `<type>/<issue>-<slug>` (`feat`, `fix`, `chore`, `infra`, `content`, `docs`, `security`), e.g. `fix/231-admin-login`.
- Open the PR with the template; first line `Closes #<issue>`. Set the issue to `status:needs-review`.
- Right after opening the PR, enable auto-merge (squash) on it with your own GitHub tools, so it merges as soon as the required checks pass. Do not use a workflow token for this: merges made with `GITHUB_TOKEN` do not trigger the deploy pipeline.
- Fix every failing check until the PR is green. Never disable or skip a test or check to get green.
- Keep the PR within the issue's scope. Found something else? Open a new issue from a template.
- Anything that needs a console login, credentials, an account or money: stop, label the issue `status:needs-human`, and say exactly what the owner must do. Never ask for secrets in an issue or PR.

## Definition of done

- Acceptance criteria in the issue are met.
- Tests cover the change and pass.
- **Docs are updated in the same PR.** Any change to behaviour, config, infra, pipelines or this workflow updates the matching page under `docs/` (index: `docs/README.md`). `docs-check.yml` enforces this. Use the `docs:not-needed` label only when nothing documented changed, and say why in the PR body.
- No secrets in code or config. Secrets come from Scaleway Secret Manager (see issue #226) or GitHub secrets.

## Commands

Run before opening a PR (from the repo root):

```bash
# Backend (never build the Aspire AppHost in CI or Docker)
dotnet format Clercq.It.CI.slnf --verify-no-changes   # fix with: dotnet format Clercq.It.CI.slnf
dotnet build Clercq.It.CI.slnf -c Release              # warnings are errors
dotnet test Clercq.It.CI.slnf -c Release --no-build

# Frontend
cd src/ClercqIt.Web && pnpm install --frozen-lockfile && pnpm lint && pnpm format:check && pnpm typecheck && pnpm build

# Infra
cd infra/terraform && terraform fmt -check && terraform validate
```

Local full stack: `dotnet run --project src/Clercq.It.AppHost` (needs Docker and the Aspire workload); see [docs/development.md](docs/development.md).

## Pointers

- Architecture and layering: [docs/architecture.md](docs/architecture.md); the rules are enforced by `tests/Clercq.It.Architecture.Tests`
- Pipelines and deployment: [docs/devops.md](docs/devops.md)
- Enforced repo rules (rulesets, PR conventions, weekly audit): [docs/contributing.md#enforced-rules](docs/contributing.md#enforced-rules)
- Versioning: [docs/versioning.md](docs/versioning.md)
- Production auth (Cloud IAM): [docs/ciam.md](docs/ciam.md)
- Coding conventions: `.github/instructions/*.instructions.md`; code style and analyzer policy: [docs/development.md#code-style](docs/development.md#code-style)
