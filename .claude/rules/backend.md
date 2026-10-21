---
paths:
  - "src/**/*.cs"
  - "tests/**/*.cs"
  - "**/*.csproj"
  - "Directory.Build.props"
  - ".editorconfig"
---

# Backend rules

Full policy: docs/architecture.md#architecture-rules and docs/development.md#code-style.

- Use `Clercq.It.CI.slnf` for restore, build, test and format. Never build the Aspire AppHost in CI or Docker.
- `dotnet format Clercq.It.CI.slnf --verify-no-changes` and `dotnet build Clercq.It.CI.slnf -c Release` (warnings are errors) must pass. Fix with `dotnet format Clercq.It.CI.slnf`.
- Analyzers: fix the code. If a rule truly does not fit, change its severity in `.editorconfig` with a comment saying why. No `#pragma warning disable`.
- Layering (enforced by `tests/Clercq.It.Architecture.Tests`): Domain depends on nothing; Application only on Domain, MediatR and FluentValidation; endpoints go through MediatR, never the DbContext or repositories. Fix the dependency, not the test.
- Aggregates have no public setters. Change state through `Create`, the constructor or behaviour methods that enforce invariants; build them in tests with `tests/Clercq.It.Application.Tests/Aggregates.cs`.
- MediatR requests end in `Command`/`Query`, handlers in `Handler`, validators in `Validator`.
- A schema change needs an EF migration in `src/Clercq.It.Infrastructure/EF/Migrations` (docs/migrations.md).
