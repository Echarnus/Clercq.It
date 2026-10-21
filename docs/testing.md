# Testing

How the backend and the frontend are tested, and the coverage gates CI enforces (issues #242 and #246). Backend integration tests against real infrastructure come with #244 (Testcontainers); Playwright e2e in CI comes with #246.

## Stack

| Concern | Choice |
| --- | --- |
| Test framework | xUnit 2 |
| Assertions | FluentAssertions **7** (stay on 7: version 8 moved to a commercial licence) |
| Test doubles | Moq (aliased as `MoqIt` for `It`, to avoid the clash with xUnit) |
| Coverage | `coverlet.collector`, reports with ReportGenerator (local tool in `.config/dotnet-tools.json`) |
| Architecture rules | NetArchTest, in `tests/Clercq.It.Architecture.Tests` ([rules](./architecture.md#architecture-rules)) |

## Projects

One test project per production project:

| Production project | Tests |
| --- | --- |
| `Clercq.It.Domain` | `tests/Clercq.It.Domain.Tests`: every entity invariant and value object |
| `Clercq.It.Application` | `tests/Clercq.It.Application.Tests`: every command/query handler and validator, with repositories and storage mocked |
| `ClercqIt.Api` | `tests/ClercqIt.Api.Tests`: endpoints and auth, through `WebApplicationFactory` |
| `Clercq.It.ContentImport` | `tests/Clercq.It.ContentImport.Tests` |
| All layers | `tests/Clercq.It.Architecture.Tests` |

A test class is named after the class it tests plus `Tests`, and its namespace mirrors that class's namespace: `CreateBlogCommandHandler` in `Clercq.It.Application.Features.Blogs.Commands` is tested by `CreateBlogCommandHandlerTests` in `Clercq.It.Application.Tests.Features.Blogs.Commands`. The files currently sit flat in each test project.

## Conventions

- Name tests `Method_State_ExpectedResult`, for example `Handle_WhenImageUploadFails_ShouldNotSaveBlog`. Older tests use `Handle_ShouldX`; keep them, use the full pattern for new ones.
- Arrange / Act / Assert, each marked with a comment.
- Every behaviour change ships with tests. A bug fix starts with a test that fails without the fix.
- Every Application handler has at least one happy-path test and one failure test (a dependency that throws, a missing entity, or an empty result for queries).
- Assert on behaviour (returned DTO, calls made or not made), not on implementation details.

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

Open `coverage/report/index.html` for line-by-line coverage. In a Claude Code cloud session, set `DOTNET_ROLL_FORWARD=Major` first (the session-start hook does this).

## Coverage

The `Test .NET API` job fails when line coverage is below `tests/coverage-thresholds.json`, per assembly and in total. It prints a table in the job summary and uploads the HTML report as the `coverage-report` artifact. Coverage also still goes to Codecov.

Starting thresholds (21 October 2026, the measured coverage rounded down):

| Assembly | Minimum line coverage |
| --- | ---: |
| Total | 73% |
| `Clercq.It.Domain` | 100% |
| `Clercq.It.Application` | 98% |
| `ClercqIt.Api` | 89% |
| `Clercq.It.ContentImport` | 73% |
| `Clercq.It.ServiceDefaults` | 97% |
| `Clercq.It.Infrastructure` | 19% (repositories and storage get integration tests in #244) |

Domain and Application together are above the 80% target.

Ratchet rule: when a PR raises coverage, raise the matching number in the same PR. Never lower a number without saying why in the PR body. A new production assembly needs a threshold, or the gate fails.

## Frontend unit and component tests

The frontend tests live next to the code in `src/ClercqIt.Web` (issue #246). The separate `tests/ClercqIt.Web.Tests` Jest package is gone.

| Concern | Choice |
| --- | --- |
| Runner | Vitest, config in `src/ClercqIt.Web/vitest.config.mts` |
| DOM | jsdom, with React Testing Library and `@testing-library/user-event` |
| Matchers | `@testing-library/jest-dom` (set up in `test/setup.ts`) |
| API mocking | MSW: default handlers in `test/msw/handlers.ts`, fixtures in `test/msw/fixtures.ts` |
| Coverage | `@vitest/coverage-v8` |

Conventions:

- A test sits beside the file it tests: `lib/api/adminApi.ts` is tested by `lib/api/adminApi.test.ts`, `components/admin/TagInput.tsx` by `TagInput.test.tsx`.
- Mock the network with MSW, never by stubbing `fetch`. Unhandled requests fail the test. Override a handler for one test with `server.use(...)`; handlers reset after each test.
- Query the DOM the way a user does: by role and accessible name, label or text. If an element has no accessible name, give it one (an `aria-label`) rather than reaching for test ids.
- Unit-test `lib/`, hooks and components with logic (forms, inputs, conditional rendering). Whole pages, routing and the real API belong to Playwright e2e (`e2e/`).
- `components/ui/` is generated shadcn/ui code and is excluded from coverage.

Running, from `src/ClercqIt.Web`:

```bash
pnpm test               # all unit tests once
pnpm test:watch         # watch mode
pnpm test:coverage      # with the coverage gate CI uses; HTML report in coverage/
```

### Frontend coverage

The `Test Next.js Frontend` job runs `pnpm test:coverage` and fails below the thresholds in `vitest.config.mts`. It uploads the report as the `frontend-coverage` artifact.

Starting thresholds (21 October 2026, measured coverage rounded down) over `lib/`, `hooks/` and `components/` (without `components/ui/`):

| Scope | Lines | Branches | Functions | Statements |
| --- | ---: | ---: | ---: | ---: |
| `lib/api/**` | 100% | 95% | 100% | 99% |
| Total | 56% | 47% | 43% | 56% |

The same ratchet rule as the backend applies: raise the numbers when coverage rises, and never lower one without a reason in the PR body.
