---
name: write-tests
description: Write tests for a Clercq.It class, endpoint, component or diff in the house style (xUnit, FluentAssertions 7, Moq; Vitest, Testing Library, MSW), run them, and report what is covered. Use when asked to add tests, when a change has none, or when a coverage gate fails.
---

# Write tests

The house style is in docs/testing.md. Read it first; this skill is the checklist for applying it (issue #288).

## 1. Find what to test

- Given a class or file: test that.
- Given a diff or a branch: `git diff origin/main...HEAD --name-only` and list every changed production file under `src/`. Each one needs tests unless it is generated code (`components/ui/`, EF migrations).
- Read the code under test and list its behaviours: happy path, each failure path (a dependency throws, a missing entity, invalid input, empty result) and each branch.

## 2. Pick the project and the kind of test

| Code | Tests go in | Kind |
| --- | --- | --- |
| `src/Clercq.It.Domain` | `tests/Clercq.It.Domain.Tests` | Unit: entity invariants, value objects |
| `src/Clercq.It.Application` | `tests/Clercq.It.Application.Tests` | Unit: handlers and validators, repositories and storage mocked with Moq |
| `src/ClercqIt.Api` endpoints, Infrastructure | `tests/ClercqIt.Api.Tests` | Integration: `CustomWebApplicationFactory` with real Postgres and Keycloak (needs Docker). Small API classes with no endpoint (handlers, extensions) get unit tests in the same project |
| `src/Clercq.It.ContentImport` | `tests/Clercq.It.ContentImport.Tests` | Unit |
| `src/ClercqIt.Web` | next to the file, `*.test.ts(x)` | Vitest + Testing Library; network mocked with MSW (`test/msw/`), never by stubbing `fetch` |

New endpoints also need: 400 for invalid input, 404 where it applies, 401 anonymous, 403 wrong role, and an entry in `EndpointAuthorizationTests`.

## 3. Write them in the house style

- Class `<ClassUnderTest>Tests`, namespace mirroring the class's namespace with `.Tests` after the project name.
- Test names `Method_State_ExpectedResult`.
- `// Arrange`, `// Act`, `// Assert` comments.
- FluentAssertions 7 (`.Should()`), never version 8. Moq is the only mocking library: alias `It` as `MoqIt` where xUnit's `It` would clash, as the existing tests do.
- Assert on behaviour (returned values, calls made or not made, HTTP status and body), not on private details.
- A bug fix starts with a test that fails without the fix: run it before the fix and show it failing.

## 4. Run them

```bash
dotnet test Clercq.It.CI.slnf -c Release --filter "FullyQualifiedName~<TestClass>"
cd src/ClercqIt.Web && pnpm vitest run <path/to/file.test.tsx>
```

Then run the `quality-check` skill: it runs the whole suite with coverage and the gates in `tests/coverage-thresholds.json` and `vitest.config.mts`. If coverage rose, raise the thresholds in the same PR (never lower them).

## 5. Report

Tell the user which behaviours are now covered, which tests were added (file and test names), and anything left untested and why.
