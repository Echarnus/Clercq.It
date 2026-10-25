# Testing

How the backend and the frontend are tested, including API integration tests against real Postgres and Keycloak, and the coverage gates CI enforces (issues #242, #244 and #246). Browser end-to-end tests in CI come with #314 (Playwright).

## Stack

| Concern | Choice |
| --- | --- |
| Test framework | xUnit 2 |
| Assertions | FluentAssertions **7** (stay on 7: version 8 moved to a commercial licence) |
| Test doubles | Moq, the only mocking library for new tests (aliased as `MoqIt` for `It`, to avoid the clash with xUnit) |
| API integration | Testcontainers (PostgreSQL 17, Keycloak 26.4), Respawn, `WebApplicationFactory<Program>` |
| Coverage | `coverlet.collector`, reports with ReportGenerator (local tool in `.config/dotnet-tools.json`) |
| Architecture rules | NetArchTest, in `tests/Clercq.It.Architecture.Tests` ([rules](./architecture.md#architecture-rules)) |

## Projects

One test project per production project:

| Production project | Tests |
| --- | --- |
| `Clercq.It.Domain` | `tests/Clercq.It.Domain.Tests`: every entity invariant and value object |
| `Clercq.It.Application` | `tests/Clercq.It.Application.Tests`: every command/query handler and validator, with repositories and storage mocked |
| `ClercqIt.Api` (and Infrastructure) | `tests/ClercqIt.Api.Tests`: every endpoint against real PostgreSQL and Keycloak containers ([below](#api-integration-tests)) |
| `Clercq.It.ContentImport` | `tests/Clercq.It.ContentImport.Tests` |
| All layers | `tests/Clercq.It.Architecture.Tests` |

A test class is named after the class it tests plus `Tests`, and its namespace mirrors that class's namespace: `CreateBlogCommandHandler` in `Clercq.It.Application.Features.Blogs.Commands` is tested by `CreateBlogCommandHandlerTests` in `Clercq.It.Application.Tests.Features.Blogs.Commands`. The files currently sit flat in each test project.

## Conventions

- Name tests `Method_State_ExpectedResult`, for example `Handle_WhenImageUploadFails_ShouldNotSaveBlog`. Older tests use `Handle_ShouldX`; keep them, use the full pattern for new ones.
- Arrange / Act / Assert, each marked with a comment.
- Every behaviour change ships with tests. A bug fix starts with a test that fails without the fix.
- Every Application handler has at least one happy-path test and one failure test (a dependency that throws, a missing entity, or an empty result for queries).
- Every new or changed endpoint gets integration tests for its happy path, validation (400), not found (404) where it applies, and auth: anonymous (401), no role and wrong role (403). Protected endpoints are listed in `EndpointAuthorizationTests`.
- Assert on behaviour (returned DTO, calls made or not made), not on implementation details.

Package versions for test projects come from `src/Directory.Packages.props` like the rest (see [development](./development.md#code-style)).

## Claude Code skills

| Skill | What it does |
| --- | --- |
| `write-tests` (`.claude/skills/write-tests/SKILL.md`) | Given a class, endpoint, component or diff, lists the behaviours to cover, writes tests in this page's style in the right project, runs them and reports what is covered. First used on `ValidationExceptionHandler` (`tests/ClercqIt.Api.Tests/ValidationExceptionHandlerTests.cs`). |
| `quality-check` (`.claude/skills/quality-check/SKILL.md`) | Runs every gate a PR needs, including `dotnet test` with coverage and the thresholds in `tests/coverage-thresholds.json`, and prints a pass/fail table. |

## Running

```bash
# All tests
dotnet test Clercq.It.CI.slnf

# With coverage and the same gate CI uses
dotnet test Clercq.It.CI.slnf --collect:"XPlat Code Coverage" --results-directory ./coverage
dotnet tool restore
dotnet tool run reportgenerator -reports:"./coverage/**/coverage.cobertura.xml" -targetdir:./coverage/report -reporttypes:"JsonSummary;Html"
python3 .github/scripts/check_coverage.py ./coverage/report/Summary.json tests/coverage-thresholds.json
```

Open `coverage/report/index.html` for line-by-line coverage.

## API integration tests

`tests/ClercqIt.Api.Tests` boots the real API (`WebApplicationFactory<Program>`, environment `Production`) against containers that Testcontainers starts once for the whole test run (`CustomWebApplicationFactory`, shared by the `Api` xUnit collection):

- **PostgreSQL** `postgres:17-alpine`. The fixture applies the real EF migrations. Test classes that write data call `ResetDatabaseAsync()` in `InitializeAsync`, which empties every table except `__EFMigrationsHistory` with Respawn.
- **Keycloak** `keycloak/keycloak:26.4`, importing the local realm `src/Clercq.It.AppHost/KeycloakRealms/clercqit-realm.json`. Tests sign in as real realm users with the password grant on the local-only `clercqit-tests` client (`clercqit-web` itself only allows the authorization code flow with PKCE) and send the real access token, so signature, issuer, lifetime and role checks all run. `CreateClientForUserAsync("blogger")` returns an authenticated client.

| User | Roles | Use it for |
| --- | --- | --- |
| `admin` | all, including `MyZen.Owner` | MyZen endpoints (`MyZenEndpointTests` mocks the MyZen gateway) |
| `blogger` | `Admin.View`, `Blogs.Contributor` | blogs and images |
| `projects` | `Admin.View`, `Projects.Contributor` | projects |
| `certs` | `Admin.View`, `Certifications.Contributor` | certifications |
| `test` | none | 403 tests |

Only object storage is faked (a Moq `IObjectStorageService` that returns `CustomWebApplicationFactory.FakeImageUrl`). MyZen itself is never called: its client is tested without Docker against a scripted MyZen (`tests/ClercqIt.Api.Tests/MyZen/MyZenGatewayTests.cs`). Each request gets its own client IP, so the per-IP rate limits don't trip in a run; rate limiting itself is not tested here.

Running them needs Docker:

```bash
dotnet test tests/ClercqIt.Api.Tests
```

In CI they are a separate step, `API integration tests`, after `Unit tests` in the `Test .NET API` job (Docker is built into `ubuntu-latest`). In a Claude Code cloud session the Docker daemon is installed but not running: start it with `dockerd > /tmp/dockerd.log 2>&1 &`. If Docker Hub answers `429 Too Many Requests`, pull through Google's mirror and retag, for example `docker pull mirror.gcr.io/keycloak/keycloak:26.4 && docker tag mirror.gcr.io/keycloak/keycloak:26.4 keycloak/keycloak:26.4`. `quay.io` is not reachable from the cloud session.

## Container smoke test

The `Container smoke test` job in `.github/workflows/test.yml` starts the production image on every PR against a throwaway Postgres with migrations and content applied, and runs `.github/scripts/smoke-test.sh` against `http://localhost`. It catches what unit and integration tests cannot: a broken image, a missing runtime library, nginx or `src/start.sh` failing. Details and the local commands: [devops.md](./devops.md#container-test-container-smoke-test). ZAP (#239) and e2e (#246) will run against the same container.

## Coverage

The `Test .NET API` job fails when line coverage is below `tests/coverage-thresholds.json`, per assembly and in total. It prints a table in the job summary and uploads the HTML report as the `coverage-report` artifact. Coverage also still goes to Codecov.

Thresholds (21 October 2026, the measured coverage rounded down; raised when the integration tests in #244 started covering Infrastructure, again in #247 once compiler-generated code was excluded, and in #359 when coverlet 10 started measuring async methods and tests for object storage, content images and transactions were added):

| Assembly | Minimum line coverage |
| --- | ---: |
| Total | 98% |
| `Clercq.It.Domain` | 100% |
| `Clercq.It.Application` | 98% |
| `ClercqIt.Api` | 98% |
| `Clercq.It.ContentImport` | 96% |
| `Clercq.It.ServiceDefaults` | 97% |
| `Clercq.It.Infrastructure` | 96% |

Domain and Application together are above the 80% target.

`coverlet.collector` 10 counts async method bodies (their compiler-generated state machines hold the code you wrote); 6.0.4 skipped them on .NET 10, so async code looked covered when it was not.

Compiler-generated code is not counted: `tests/coverage.runsettings` (applied to every `dotnet test` through `RunSettingsFilePath` in `Directory.Build.props`) excludes `[GeneratedCode]` and `[CompilerGenerated]` code and the .NET 10 OpenAPI XML-comment generator (`Microsoft.AspNetCore.OpenApi.Generated`), which otherwise adds about 250 uncoverable lines to `ClercqIt.Api`.

Ratchet rule: when a PR raises coverage, raise the matching number in the same PR. Never lower a number without saying why in the PR body. A new production assembly needs a threshold, or the gate fails.

## Frontend unit and component tests

The frontend tests live next to the code in `src/ClercqIt.Web` (issue #246). The separate Jest test package that used to sit under `tests/` is gone.

| Concern | Choice |
| --- | --- |
| Runner | Vitest, config in `src/ClercqIt.Web/vitest.config.mts` |
| DOM | jsdom, with React Testing Library and `@testing-library/user-event` |
| Matchers | `@testing-library/jest-dom` (set up in `test/setup.ts`) |
| API mocking | MSW: default handlers in `test/msw/handlers.ts`, fixtures in `test/msw/fixtures.ts` |
| Coverage | `@vitest/coverage-v8` |
| Property-based tests | `fast-check` |

Conventions:

- A test sits beside the file it tests: `lib/api/adminApi.ts` is tested by `lib/api/adminApi.test.ts`, `components/admin/TagInput.tsx` by `TagInput.test.tsx`.
- Mock the network with MSW, never by stubbing `fetch`. Unhandled requests fail the test. Override a handler for one test with `server.use(...)`; handlers reset after each test.
- Query the DOM the way a user does: by role and accessible name, label or text. If an element has no accessible name, give it one (an `aria-label`) rather than reaching for test ids.
- Unit-test `lib/`, hooks and components with logic (forms, inputs, conditional rendering). Whole pages, routing and the real API belong to Playwright e2e (`e2e/`).
- `components/ui/` is generated shadcn/ui code and is excluded from coverage.

### Property-based tests (fuzzing)

Code that handles untrusted input also gets property-based tests with [fast-check](https://fast-check.dev/) (issue #402). Instead of a few hand-picked examples, a property test states a rule that must hold for every input, and fast-check generates hundreds of inputs (and shrinks a failure to the smallest one) to try to break it. They run with the other Vitest tests, so CI runs them on every PR.

| Test | Property |
| --- | --- |
| `lib/auth.property.test.ts` | the post-sign-in return address is always a same-origin path under `/admin` |
| `app/blogs/lib/filters.property.test.ts` | the blog filters never throw and match exactly the posts they should |
| `components/markdown.property.test.tsx` | rendered Markdown never contains scripts, event handlers or `javascript:` URLs |

Name them `<file>.property.test.ts(x)` beside the code they test. Add one when you write a parser, a validator, a sanitiser or a redirect check. These tests are also what OpenSSF Scorecard's Fuzzing check looks for (an import of `fast-check`); .NET fuzzers are not detected by Scorecard.

Running, from `src/ClercqIt.Web`:

```bash
pnpm test               # all unit tests once
pnpm test:watch         # watch mode
pnpm test:coverage      # with the coverage gate CI uses; HTML report in coverage/
```

### Frontend coverage

The `Test Next.js Frontend` job runs `pnpm test:coverage` and fails below the thresholds in `vitest.config.mts`. It uploads the report as the `frontend-coverage` artifact.

Thresholds over `lib/`, `hooks/` and `components/` (without `components/ui/`), measured coverage rounded down. Started 21 October 2026; raised 23 October 2026 with the MyZen tab (#345) and again with tests for `useIsClient` and `StoreProvider` (#351; Vite 8 counts a few more statements than Vite 7), and on 24 October 2026 with the admin sidebar (#449):

| Scope | Lines | Branches | Functions | Statements |
| --- | ---: | ---: | ---: | ---: |
| `lib/api/**` | 100% | 95% | 100% | 99% |
| Total | 81% | 74% | 76% | 81% |

The same ratchet rule as the backend applies: raise the numbers when coverage rises, and never lower one without a reason in the PR body.
