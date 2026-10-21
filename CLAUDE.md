# Clercq.It agent guide

Personal website www.clercq.it: .NET 9 API (Clean Architecture, EF Core, PostgreSQL) and Next.js 15 frontend, shipped as one Docker image to a Scaleway serverless container. Production auth is Cloud IAM (hosted Keycloak). Full docs: [docs/README.md](docs/README.md).

## How work flows

Everything goes issue → branch → PR → reviewer agent → auto-merge → production. No human reviews. Details: [docs/contributing.md](docs/contributing.md).

- Work only on issues labelled `status:ready`; set `status:in-progress` when you start.
- Branch from `main` as `<type>/<issue>-<slug>` (`feat`, `fix`, `chore`, `infra`, `content`, `docs`, `security`), e.g. `fix/231-admin-login`.
- Open the PR with the template; first line `Closes #<issue>`. Set the issue to `status:needs-review`.
- Address every review comment and failing check until the PR is approved and green. Never disable or skip a test or check to get green.
- Keep the PR within the issue's scope. Found something else? Open a new issue from a template.
- Anything that needs a console login, credentials, an account or money: stop, label the issue `status:needs-human`, and say exactly what the owner must do. Never ask for secrets in an issue or PR.

## Definition of done

- Acceptance criteria in the issue are met.
- Tests cover the change and pass.
- **Docs are updated in the same PR.** Any change to behaviour, config, infra, pipelines or this workflow updates the matching page under `docs/` (index: `docs/README.md`). `docs-check.yml` and the reviewer enforce this. Use the `docs:not-needed` label only when nothing documented changed, and say why in the PR body.
- No secrets in code or config. Secrets come from Scaleway Secret Manager (see issue #226) or GitHub secrets.

## Commands

Run before opening a PR (from the repo root):

```bash
# Backend (never build the Aspire AppHost in CI or Docker)
dotnet build src/ClercqIt.Api/ClercqIt.Api.csproj
dotnet test tests/ClercqIt.Api.Tests tests/Clercq.It.Application.Tests tests/Clercq.It.Domain.Tests

# Frontend
cd src/ClercqIt.Web && pnpm install --frozen-lockfile && pnpm lint && pnpm build

# Infra
cd infra/terraform && terraform fmt -check && terraform validate
```

Local full stack: `dotnet run --project src/Clercq.It.AppHost` (needs Docker and the Aspire workload); see [docs/development.md](docs/development.md).

## Pointers

- Architecture and layering: [docs/architecture.md](docs/architecture.md)
- Pipelines and deployment: [docs/devops.md](docs/devops.md)
- Versioning: [docs/versioning.md](docs/versioning.md)
- Production auth (Cloud IAM): [docs/ciam.md](docs/ciam.md)
- Coding conventions: `.github/instructions/*.instructions.md`
