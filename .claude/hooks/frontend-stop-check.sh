#!/bin/bash
# Stop hook, issue #240. Blocks ending a turn while frontend files changed
# versus main fail Prettier, ESLint or tsc. See docs/development.md#claude-code-hooks.
set -uo pipefail

input=$(cat)
# Avoid a loop: if this stop was already blocked once, let it through.
[ "$(jq -r '.stop_hook_active // false' <<< "$input")" = "true" ] && exit 0

cd "$CLAUDE_PROJECT_DIR" || exit 0
base=$(git merge-base HEAD origin/main 2>/dev/null || echo HEAD)
mapfile -t changed < <(
  { git diff --name-only "$base"; git ls-files --others --exclude-standard; } |
    grep -E '^src/ClercqIt\.Web/.*\.(ts|tsx|js|mjs|json|css|md)$' |
    grep -vE '/(node_modules|\.next)/|pnpm-lock\.yaml$' | sort -u |
    while read -r f; do [ -f "$f" ] && echo "${f#src/ClercqIt.Web/}"; done
)
[ ${#changed[@]} -eq 0 ] && exit 0

cd src/ClercqIt.Web || exit 0
failed=0
report=""
if ! out=$(npx prettier --check "${changed[@]}" 2>&1); then
  failed=1; report+=$'Prettier:\n'"$out"$'\n'
fi
mapfile -t code < <(printf '%s\n' "${changed[@]}" | grep -E '\.(ts|tsx|js|mjs)$')
if [ ${#code[@]} -gt 0 ] && ! out=$(npx eslint --cache --max-warnings 0 "${code[@]}" 2>&1); then
  failed=1; report+=$'ESLint:\n'"$out"$'\n'
fi
if ! out=$(npx tsc --noEmit 2>&1); then
  failed=1; report+=$'tsc:\n'"$out"$'\n'
fi

if [ "$failed" -eq 1 ]; then
  echo "Frontend checks fail on changed files. Fix them before finishing:" >&2
  echo "$report" >&2
  exit 2
fi
exit 0
