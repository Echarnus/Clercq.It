---
paths:
  - "src/ClercqIt.Web/**"
  - "tests/ClercqIt.Web.Tests/**"
---

# Frontend rules

Full policy: docs/development.md#frontend-lint-format-and-types.

- `pnpm lint` (ESLint 9, zero warnings), `pnpm format:check` (Prettier) and `pnpm typecheck` (`tsc --noEmit`) must pass; CI blocks the PR otherwise. `pnpm build` no longer ignores lint or type errors.
- No `any`. Type JSON from `fetch` with an interface (`(await res.json()) as T`) or validate it with zod (`const data: unknown = await res.json()`).
- No `eslint-disable` without a reason after `--` on the same comment.
- Never change `eslint.config.mjs`, `.prettierrc.json` or `tsconfig.json` just to make a check or hook pass; fix the code.
- Promise-returning handlers in JSX: wrap with `void`, e.g. `onSubmit={(e) => void form.handleSubmit(onSubmit)(e)}`.
- Interactive elements are `<button>` or `<a>`, not clickable `<div>`s (jsx-a11y).
