---
paths:
  - "src/**"
  - "tests/**"
---

# Testing rules

Full conventions: docs/testing.md.

- Every behaviour change ships with tests in the same PR. A bug fix starts with a test that fails without the fix.
- One test project per production project; `YTests` tests `Y`, and its namespace mirrors `Y`'s (`<Project>.Tests.<same sub-namespace>`).
- Name tests `Method_State_ExpectedResult`; Arrange / Act / Assert with comments.
- Stack: xUnit, FluentAssertions 7 (do not upgrade to 8: licence), Moq with `MoqIt` for `Moq.It`.
- Every Application handler has a happy-path test and a failure test.
- Run `dotnet test Clercq.It.CI.slnf` before pushing. CI fails below `tests/coverage-thresholds.json`; when your change raises coverage, raise the threshold in the same PR, and never lower one without a reason in the PR body.
