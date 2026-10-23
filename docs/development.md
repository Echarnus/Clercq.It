# Local Development Guide

This guide covers local development setup and workflows for the Clercq.It project, including both manual setup and Aspire orchestration.

## Prerequisites

### Required Software
- **.NET 10 SDK** (LTS) - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Node.js 24 LTS** - [Download](https://nodejs.org/). CI (`test.yml`, `security-scan.yml`) and the Docker image (`node:24-slim`) use the same major.
- **pnpm 10.12.4+** - Install with `npm install -g pnpm`
- **Docker** - [Download](https://www.docker.com/products/docker-desktop)
- **Git** - [Download](https://git-scm.com/)

### Frontend versions

The frontend runs Next.js 16 (Turbopack builds), React 19, Tailwind CSS 4, zod 4 with `@hookform/resolvers` 6, and lucide-react 1 for icons. API ids are validated with `z.guid()`, not `z.uuid()`, because zod 4's `uuid()` rejects ids that are valid .NET Guids but not RFC 9562 UUIDs. Tailwind 4 has no `tailwind.config.ts`: the theme (colours, radii, animations) lives in `@theme` in `app/globals.css`, PostCSS uses `@tailwindcss/postcss`, and animations come from `tw-animate-css`. Client-only rendering checks use `useIsClient()` (`hooks/use-is-client.ts`, built on `useSyncExternalStore`) instead of a `mounted` flag set in an effect, which the React hooks lint rules reject. Avoid `latest` as a version range in `package.json`: a new major then lands with any routine update. Vitest runs on Vite 8 with `@vitejs/plugin-react` 6; `vite` is a direct dev dependency because the plugin needs Vite 8 as a peer.

Held back on purpose (audit of 23 October 2026, #351): ESLint stays on 9 because `eslint-plugin-jsx-a11y` 6.10 does not support ESLint 10; TypeScript stays on 5.9 because `typescript-eslint` 8 supports TypeScript below 6.1; `@types/node` stays on 24 to match the Node 24 runtime (Dependabot ignores newer majors). Lift a hold when the blocking package catches up.

`package.json` has `pnpm.overrides` for transitive packages with known vulnerabilities that their parent pins to an old version (`postcss` and `sharp` under Next.js, `yaml` under Vite). Drop an override once the parent ships the fixed version.

### Optional Tools
- **Visual Studio 2022** or **VS Code** with C# extension
- **pgAdmin** or **DBeaver** for database management
- **Postman** or **Insomnia** for API testing

## Project Structure

```
Clercq.It/
├── src/                          # Source code
│   ├── ClercqIt.Api/            # ASP.NET Core API
│   ├── ClercqIt.Web/            # Next.js frontend
│   ├── Clercq.It.Domain/        # Domain layer
│   ├── Clercq.It.Application/   # Application layer
│   ├── Clercq.It.Infrastructure/ # Infrastructure layer
│   ├── Clercq.It.AppHost/       # Aspire orchestration (dev only)
│   └── Clercq.It.ServiceDefaults/ # Aspire service defaults (dev only)
├── tests/                       # Test projects
│   └── ClercqIt.Api.Tests/      # API unit tests
├── docs/                        # Documentation
└── .github/                     # GitHub workflows & config
```

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/Echarnus/Clercq.It.git
cd Clercq.It
```

### 2. Choose Your Development Approach

You can develop using either **Aspire orchestration** (recommended) or **manual setup**.

## Option A: Aspire Orchestration (Recommended)

> **⚠️ Important**: Aspire is **only for local development**. Production deployments use Docker containers without Aspire components.

### What is Aspire?

.NET Aspire is a development orchestration tool that simplifies running the entire application stack locally with enhanced observability.

### Prerequisites
- .NET 10 SDK
- Docker Desktop

The AppHost uses Aspire 13 (`Aspire.AppHost.Sdk/13.6.2`); no workload is needed. The frontend runs through `AddJavaScriptApp` with pnpm.

> **Note**: The Aspire workload is only required for local development when running the AppHost. Production builds and CI/CD pipelines do NOT require this workload.

### Start the Application

```bash
cd src/Clercq.It.AppHost
dotnet run
```

This will automatically:
- Launch the Aspire Dashboard at `http://localhost:15888`
- Start PostgreSQL with pgAdmin web interface
- Launch the API with automatic database connection and migrations
- Start the Next.js frontend (if configured)
- Provide comprehensive observability, logging, and monitoring

### Aspire Dashboard Features

The Aspire Dashboard provides:
- **Service Overview**: Monitor all running services
- **Logs**: Centralized logging from all services
- **Traces**: Distributed tracing across services
- **Metrics**: Performance metrics and health status
- **Configuration**: Service configuration and connection strings

### Services Available

When running with Aspire, the following services are available:

- **PostgreSQL Database**: `postgres` service with pgAdmin
- **API Service**: `clercqit-api` at `https://localhost:7000`
- **Next.js Frontend**: `clercqit-web` (if configured)

### Keycloak Authentication

Aspire automatically starts a Keycloak instance with the `clercqit` realm pre-configured.

- **Keycloak Admin Console**: http://localhost:8080
- **Admin credentials**: Auto-generated by Aspire (visible in Aspire dashboard environment variables)

#### Test Users

The realm includes the following test users for local development:

| Username   | Password   | Email                | Roles                                                                      |
|------------|------------|----------------------|----------------------------------------------------------------------------|
| `admin`    | `admin`    | admin@clercq.it      | Admin.View, Blogs.Contributor, Projects.Contributor, Certifications.Contributor, MyZen.Owner |
| `blogger`  | `blogger`  | blogger@clercq.it    | Admin.View, Blogs.Contributor                                              |
| `projects` | `projects` | projects@clercq.it   | Admin.View, Projects.Contributor                                           |
| `certs`    | `certs`    | certs@clercq.it      | Admin.View, Certifications.Contributor                                     |
| `test`     | `test`     | test@clercq.it       | _(none)_                                                                   |

#### Testing Authentication

Open http://localhost:3000/admin and choose **Sign In**. The app sends you to the local Keycloak login page (authorization code flow with PKCE, issue #257); sign in as one of the users above and Keycloak sends you back to the dashboard. The `test` user signs in but has no roles, useful for testing "no access" scenarios.

No endpoint accepts a password. To get a token for API calls by hand, use the local-only `clercqit-tests` client, which still allows the password grant:

```bash
curl -s -d grant_type=password -d client_id=clercqit-tests -d username=admin -d password=admin \
  http://localhost:8080/realms/clercqit/protocol/openid-connect/token
```

For production authentication configuration, see the [Cloud IAM documentation](./ciam.md).

### Development Benefits

- **Automatic Service Discovery**: No manual connection string configuration
- **Enhanced Observability**: Distributed tracing and structured logging out-of-the-box
- **Simplified Workflow**: Single command starts entire application stack
- **Integrated Database Management**: pgAdmin web interface for database operations
- **Hot Reload**: Development-optimized configurations

### Aspire vs Production

| Aspect | Local Development (Aspire) | Production |
|--------|---------------------------|------------|
| Architecture | AppHost orchestration | Single Docker container |
| Components | AppHost, ServiceDefaults, Dashboard | API, frontend, nginx only |
| Database | Containerized PostgreSQL + pgAdmin | Managed PostgreSQL (Scaleway, etc.) |
| Features | Hot reload, tracing, metrics | Optimized for performance |
| Aspire | Aspire 13 SDK (NuGet, no workload) | No - AppHost excluded from builds |

> **Key Point**: The `Clercq.It.AppHost` project is **excluded from production Docker builds and CI/CD pipelines**. The ServiceDefaults project is included in production.

## Option B: Manual Setup

If you prefer to run services manually or don't want to use Aspire:

### Database Setup

#### Using Docker (Recommended)

```bash
# Start PostgreSQL container
docker run -d --name clercq-postgres \
  -e POSTGRES_DB=ClercqItDb \
  -e POSTGRES_USER=clercq_user \
  -e POSTGRES_PASSWORD=clercq_pass \
  -p 5432:5432 \
  postgres:16

# Verify container is running
docker ps
```

#### Using Local PostgreSQL Installation

1. Install PostgreSQL 16+
2. Create database and user:

```sql
CREATE DATABASE "ClercqItDb";
CREATE USER clercq_user WITH PASSWORD 'clercq_pass';
GRANT ALL PRIVILEGES ON DATABASE "ClercqItDb" TO clercq_user;
```

### Backend Setup (.NET API)

```bash
# Navigate to solution root
cd Clercq.It

# Restore NuGet packages (the CI solution filter leaves out the Aspire AppHost)
dotnet restore Clercq.It.CI.slnf

# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Apply database migrations
cd src/Clercq.It.Infrastructure
dotnet ef database update --startup-project ../ClercqIt.Api

# Build the solution
cd ../..
dotnet build Clercq.It.CI.slnf

# Run tests to verify setup
dotnet test Clercq.It.CI.slnf
```

### Run the API

```bash
cd src/ClercqIt.Api
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7000`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:7000/swagger`
- Scalar UI: `https://localhost:7000/scalar/v1`

### Frontend Setup (Next.js)

```bash
# Navigate to web project
cd src/ClercqIt.Web

# Install dependencies
pnpm install

# Run development server
pnpm dev
```

The web application will be available at `http://localhost:3000`

## Cloud agent environment (Claude Code on the web)

Agents work in a Claude Code cloud container. `.claude/hooks/session-start.sh` (registered in `.claude/settings.json`) runs at session start in that container only, and makes it able to build, test and plan:

| Tool | How it is installed | Why |
| --- | --- | --- |
| .NET SDK 10 (`dotnet-sdk-10.0` from Ubuntu) | `apt-get` | Microsoft's .NET download hosts are blocked by the environment's network policy. The projects target `net10.0`, so this is the same SDK CI and Docker use. |
| Terraform 1.6.6 | zip from releases.hashicorp.com | `terraform fmt` / `validate` on infra changes |
| Scaleway CLI 2.40.0 (pinned) | binary from GitHub releases | Reading secrets from Scaleway Secret Manager |
| NuGet and pnpm packages | `dotnet restore` (API and test projects, never the Aspire AppHost) and `pnpm install --frozen-lockfile` | Builds and tests run without further setup |

The hook is idempotent and synchronous: the session starts once it finishes.

What the owner configures on the cloud environment (Project settings → Cloud environment), not in the repo:

- Network secrets `SCW_ACCESS_KEY`, `SCW_SECRET_KEY`, `SCW_DEFAULT_PROJECT_ID` for a read-only Secret Manager key (see issue #226).
- Network access to `clercq.it` and the Scaleway APIs, for smoke tests and secret reads.

`global.json` pins the .NET 10 SDK (`rollForward: latestFeature`), the same major in the cloud environment, CI (`setup-dotnet` `10.0.x`) and Docker (`mcr.microsoft.com/dotnet/sdk:10.0`). The backend stays on .NET 10 (LTS) and EF Core 10 until a later LTS is worth the move: .NET 11 is a short-term support release and was still a release candidate at the audit of 23 October 2026 (#352). The tests run xunit v2 on `Microsoft.NET.Test.Sdk` 18, `xunit.runner.visualstudio` 4 and `coverlet.collector` 10 (#359; 6.0.4 skipped async method bodies on .NET 10); FluentAssertions stays on 7.x (licence, see [Testing](./testing.md)).

Commands agents run before opening a PR:

```bash
dotnet build src/ClercqIt.Api/ClercqIt.Api.csproj
dotnet test tests/ClercqIt.Api.Tests
dotnet test tests/Clercq.It.Application.Tests
dotnet test tests/Clercq.It.Domain.Tests
cd src/ClercqIt.Web && pnpm lint && pnpm format:check && pnpm typecheck && pnpm build
cd infra/terraform && terraform fmt -check
```

## Configuration

### Connection Strings

The API uses the following connection string by default:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ClercqItDb;Username=clercq_user;Password=clercq_pass;"
  }
}
```

For different environments, create:
- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`

### Environment Variables

You can override settings using environment variables:

```bash
# Database connection
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=ClercqItDb;Username=clercq_user;Password=clercq_pass;"

# Logging level
export Logging__LogLevel__Default="Information"
```

## Development Workflow

### Making Database Changes

When modifying domain entities:

```bash
cd src/Clercq.It.Infrastructure

# Create new migration
dotnet ef migrations add MigrationName --startup-project ../ClercqIt.Api

# Apply migration to database
dotnet ef database update --startup-project ../ClercqIt.Api

# Review generated SQL (optional)
dotnet ef migrations script --startup-project ../ClercqIt.Api
```

### Adding New Features

Follow Clean Architecture patterns:

1. **Domain**: Add entities, value objects, repository interfaces in `Clercq.It.Domain`
2. **Application**: Add queries/commands, handlers, DTOs, validators in `Clercq.It.Application`
3. **Infrastructure**: Add repository implementations, entity configurations in `Clercq.It.Infrastructure`
4. **API**: Add minimal API endpoints in feature folders in `ClercqIt.Api`

### Running Tests

Conventions, the coverage gate and its thresholds are in [Testing](./testing.md).

```bash
# Run all tests
dotnet test Clercq.It.CI.slnf

# Run with coverage (see testing.md for the coverage gate)
dotnet test Clercq.It.CI.slnf --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/ClercqIt.Api.Tests/

# Run tests with detailed output
dotnet test Clercq.It.CI.slnf --verbosity normal
```

### Code style

One standard for every editor and for CI, defined in two files at the repo root:

- **`.editorconfig`**: indentation, line endings and charset for every file type, and the C# style rules: file-scoped namespaces, `using` outside the namespace, `var` when the type is obvious, PascalCase types and members, `I` prefix on interfaces, `_camelCase` private fields. It also tunes analyzer severities.
- **`Directory.Build.props`**: applies to every project under `src/` and `tests/`. It sets `Nullable` and `ImplicitUsings`, the .NET analyzers at `AnalysisLevel=latest-recommended`, and `EnforceCodeStyleInBuild`. Release builds treat every warning as an error. The exceptions are NuGet vulnerability warnings (NU1901 to NU1904), which Dependabot and the security scan handle, and the local-only Aspire AppHost.

- **`src/Directory.Packages.props`**: central package management (`ManagePackageVersionsCentrally`). Every NuGet version lives here; `PackageReference` items in the csproj files have no `Version`. To add a package, add a `PackageVersion` line here and a version-less `PackageReference` in the project. It sits in `src/` because the Docker build context is `src/` (the Dockerfile copies it before `dotnet restore`); `tests/Directory.Packages.props` imports it. Dependabot updates it.

`Clercq.It.CI.slnf` is the solution without the AppHost. Use it for restore, build, test and format, as CI does.

```bash
dotnet format Clercq.It.CI.slnf                        # fix formatting, style and fixable analyzer findings
dotnet format Clercq.It.CI.slnf --verify-no-changes    # what CI runs; fails on any difference
dotnet build Clercq.It.CI.slnf -c Release              # fails on any warning
```

Analyzer policy:

- Fix the code rather than suppressing the rule.
- If a rule does not fit, change its severity in `.editorconfig` with a comment saying why. Do not use `#pragma`.
- Test projects relax a few naming and culture rules (`CA1707`, `CA1861`, `CA1304`, `CA1310`, `CA1311`). Logging-performance rules (`CA1848`, `CA1873`) are off.
- EF Core migrations are treated as generated code.

The one-time formatting commit is listed in `.git-blame-ignore-revs`, so `git blame` skips it. GitHub does this automatically; locally, run `git config blame.ignoreRevsFile .git-blame-ignore-revs` once.

The Docker image builds from `src/` only, so these root files do not apply there. The `Test .NET API` check enforces them on every PR.

### Frontend lint, format and types

The Next.js app in `src/ClercqIt.Web` has three gates (issue #240). The `Test Next.js Frontend` check runs all three on every PR, and `pnpm build` fails on lint or type errors too: `next.config.mjs` no longer sets `ignoreDuringBuilds` or `ignoreBuildErrors`.

| Command | Tool | Fix with |
| --- | --- | --- |
| `pnpm lint` | ESLint 9, `eslint.config.mjs`, zero warnings allowed | `pnpm lint:fix`, then fix the rest by hand |
| `pnpm format:check` | Prettier, `.prettierrc.json` (2 spaces from `.editorconfig`, width 100, Tailwind class order read from `app/globals.css`) | `pnpm format` |
| `pnpm typecheck` | `tsc --noEmit` | by hand |

ESLint config (flat): `eslint-config-next/core-web-vitals` (native flat config, including the React Compiler rules of `eslint-plugin-react-hooks` 7: no synchronous `setState` in effects, no ref reads during render), `typescript-eslint` `recommendedTypeChecked` (type-aware), `jsx-a11y` recommended, `eslint-plugin-security` recommended, and `eslint-config-prettier` last so formatting is Prettier's job. The `e2e/` Playwright tests and the Vitest unit tests are linted with the app.

Policy:

- No `any`. Type `fetch` JSON with an interface (`(await res.json()) as T`), or treat it as `unknown` and validate with zod.
- An `eslint-disable` comment must say why after `--`, for example `// eslint-disable-next-line security/detect-object-injection -- index is a numeric slot position`.
- Fix the code; do not loosen `eslint.config.mjs`, `.prettierrc.json` or `tsconfig.json` to get a check green.

### Quality gates

With no human review, agents are the only thing between a change and production, so every gate CI enforces is also known to the agent while it writes code (issue #234). Three layers do that, all under `.claude/`:

**Rules** (`.claude/rules/*.md`) load into Claude's context. A rule with `paths:` front matter loads only when Claude works on matching files; one without loads always.

| Rule | Loads for | Covers |
| --- | --- | --- |
| `backend.md` | `src/**/*.cs`, `tests/**/*.cs`, project files | layering, analyzer policy, format/build/test commands |
| `frontend.md` | `src/ClercqIt.Web/**` | ESLint, Prettier and TypeScript policy |
| `testing.md` | `src/**`, `tests/**` | test conventions and coverage ratchet |
| `infra.md` | `infra/**` | `terraform fmt`/`validate`, secrets in Terraform |
| `security.md` | always | no secrets, SAST, suppressions need a reason |
| `docs.md` | always | the docs rule and the docs check |

**Skills** (`.claude/skills/<name>/SKILL.md`) are commands Claude runs on request or by itself:

| Skill | What it does |
| --- | --- |
| `quality-check` | Runs every PR gate as CI does and prints a pass/fail table: docs check, pipeline script tests, `dotnet format`, Release build, unit and architecture tests, API integration tests and the coverage gate (when Docker runs), ESLint, Prettier, `tsc`, Vitest with coverage, `next build`, `terraform fmt` and `validate`. `--fast` skips the integration tests, coverage gate and `next build`. This is the definition-of-done command: a PR is opened only after a full run passes. |
| `fix-quality` | Runs the auto-fixers (`dotnet format`, `eslint --fix`, Prettier, `terraform fmt`), then `quality-check` again. |
| `write-tests` | Writes tests for a class, endpoint, component or diff in the house style ([testing](./testing.md#claude-code-skills)), runs them and reports what is covered. |

You can run the check yourself too: `.claude/skills/quality-check/quality-check.sh`.

**Hooks** (below) format and check files while Claude edits them.

Every gate in `quality-check` also runs in CI, so nothing depends on the agent running it. All of them are required checks in `.github/rulesets/main.json` (`Test .NET API`, `Test Next.js Frontend`, `Test deploy scripts`, `Docs updated`) except Terraform: `terraform fmt` and `validate` run in `Terraform Plan` (`infra.yml`), which only runs on PRs that change `infra/`, and a required check that does not run would block every other PR.

To add a gate: add the CI step, add a matching `step` line to `.claude/skills/quality-check/quality-check.sh`, mention it in the rule for its area, and update the tables here. To add a rule, create `.claude/rules/<area>.md` with `paths:` front matter for the files it is about. To add a skill, create `.claude/skills/<name>/SKILL.md` with `name` and `description` front matter; the description says when Claude should use it.

### Claude Code hooks

Four hook scripts keep agents inside these gates while they edit:

| Script | Event | What it does |
| --- | --- | --- |
| `.claude/hooks/frontend-format.sh` | `PostToolUse` on `Edit\|Write\|MultiEdit` | For a changed file in `src/ClercqIt.Web`: `prettier --write`, then `eslint --fix`, then `eslint`. Remaining ESLint errors go back to Claude (exit code 2). `.json`, `.css` and `.md` files only get Prettier. |
| `.claude/hooks/backend-format.sh` | `PostToolUse` on `Edit\|Write\|MultiEdit` | For a changed `.cs` file under `src/` or `tests/`: `dotnet format whitespace` on that file (about 5 seconds, no project load). For a `.tf` file under `infra/`: `terraform fmt`. A syntax error goes back to Claude. |
| `.claude/hooks/frontend-stop-check.sh` | `Stop` | Runs Prettier and ESLint on the frontend files changed versus `main`, plus `tsc --noEmit`. Any failure blocks ending the turn, once per stop. |
| `.claude/hooks/backend-stop-check.sh` | `Stop` | Runs `dotnet format Clercq.It.CI.slnf --verify-no-changes` (whitespace, style and analyzers) on the C# files changed versus `main`, and `terraform fmt -check` on changed Terraform files. Any failure blocks ending the turn, once per stop. |

They are registered in `.claude/settings.json`. Agents cannot edit that file (it counts as self-modification), so the owner adds this once, next to the existing `SessionStart` entry (issue #304):

```json
"PostToolUse": [
  {
    "matcher": "Edit|Write|MultiEdit",
    "hooks": [
      { "type": "command", "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/frontend-format.sh" },
      { "type": "command", "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/backend-format.sh" }
    ]
  }
],
"Stop": [
  {
    "hooks": [
      { "type": "command", "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/frontend-stop-check.sh" },
      { "type": "command", "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/backend-stop-check.sh" }
    ]
  }
]
```

Test a hook by hand by piping the JSON Claude Code sends, for example `echo '{"tool_input":{"file_path":"'"$PWD"'/src/Clercq.It.Domain/Entities/Blog.cs"}}' | CLAUDE_PROJECT_DIR=$PWD .claude/hooks/backend-format.sh`.

### Building and Running

```bash
# Build everything except the Aspire AppHost
dotnet build Clercq.It.CI.slnf

# Build specific project
dotnet build src/ClercqIt.Api/

# Run API in watch mode (auto-restart on changes)
dotnet watch run --project src/ClercqIt.Api/
```

## Docker Development

### Build and Run with Docker

```bash
# Build Docker image
docker build -t clercq-it ./src

# Run container
docker run -p 80:80 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=ClercqItDb;Username=clercq_user;Password=clercq_pass;" \
  clercq-it
```

The application will be available at `http://localhost`

## Troubleshooting

### Common Issues

#### Port Already in Use

```bash
# Find process using port 5000/7000
lsof -i :5000
lsof -i :7000

# Kill process
kill -9 <PID>
```

#### Database Connection Failed

- Verify PostgreSQL is running: `docker ps` or check local service
- Check connection string in `appsettings.json`
- Verify firewall settings allow port 5432

#### Migration Issues

```bash
# Reset migrations (CAUTION: drops all data)
dotnet ef database drop --startup-project ../ClercqIt.Api
dotnet ef migrations remove --startup-project ../ClercqIt.Api
dotnet ef migrations add InitialCreate --startup-project ../ClercqIt.Api
dotnet ef database update --startup-project ../ClercqIt.Api
```

#### Build Errors

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

#### Aspire Dashboard Not Starting

1. **Port Conflicts**: Aspire Dashboard typically uses port 15888
2. **Docker Requirements**: PostgreSQL requires Docker Desktop to be running
3. **Network Connectivity**: Ensure Docker networks allow inter-container communication

#### Aspire SDK Issues

Aspire 13 comes from the `Aspire.AppHost.Sdk/13.6.2` NuGet SDK; there is no workload to install (an old `aspire` workload can be removed with `dotnet workload uninstall aspire`). If restore of the AppHost fails, check that `dotnet --version` is 10.x (`global.json`).

If an AppHost error shows up in CI/CD or Docker builds, check that:
- The build is not trying to restore/build the `Clercq.It.AppHost` project
- The solution-level restore/build is not being used
- Only the specific production projects are being built

### Debugging

- Check Aspire Dashboard logs for service startup issues
- Verify PostgreSQL container is running: `docker ps`
- Test API health endpoint: `https://localhost:7000/health`

## IDE Configuration

### Visual Studio 2022
- Install ASP.NET Core workload
- Install Entity Framework Core tools
- Set ClercqIt.Api as startup project (or Clercq.It.AppHost for Aspire)

### VS Code

Install recommended extensions:
- C# for Visual Studio Code
- .NET Extension Pack
- REST Client (for API testing)
- PostgreSQL (for database browsing)

### Rider
- Enable .NET Core support
- Configure database connection
- Set run configuration for ClercqIt.Api (or Clercq.It.AppHost for Aspire)

## Next Steps

Once your development environment is set up:

1. Review the [Architecture Documentation](./architecture.md)
2. Understand [Versioning and Branching](./versioning.md)
3. Learn about [CI/CD Pipelines](./devops.md)
4. Explore the API endpoints at `https://localhost:7000/swagger` or `https://localhost:7000/scalar/v1`
5. Check the test coverage with `dotnet test --collect:"XPlat Code Coverage"`
6. Start developing new features following Clean Architecture patterns

## Additional Resources

- [Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Next.js Documentation](https://nextjs.org/docs)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
