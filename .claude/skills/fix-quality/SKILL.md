---
name: fix-quality
description: Apply the Clercq.It auto-fixers (dotnet format, ESLint --fix, Prettier, terraform fmt) to the working tree, then re-run quality-check. Use when quality-check or CI fails on formatting, style or fixable lint findings.
---

# Fix quality

1. Run the auto-fixers from the repo root (each only touches what it owns):

   ```bash
   dotnet format Clercq.It.CI.slnf   # C# whitespace, style and fixable analyzer findings
   pnpm --dir src/ClercqIt.Web lint:fix                          # ESLint --fix
   pnpm --dir src/ClercqIt.Web format                            # Prettier --write
   terraform -chdir=infra/terraform fmt -recursive               # Terraform
   ```

2. Review `git diff`: the fixers must not change behaviour. Revert anything unrelated to your change that they touched in files you did not otherwise edit, unless the check fails on it.

3. Fix what the tools cannot by hand: remaining ESLint errors, analyzer warnings, type errors. Do not add `eslint-disable`, `#pragma` or `.editorconfig` severity changes to silence them (see `.claude/rules/frontend.md` and `.claude/rules/backend.md`).

4. Run the `quality-check` skill again until it passes.
