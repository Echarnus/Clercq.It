#!/bin/bash
# Stop hook, issue #234. Blocks ending a turn while C# files changed versus main
# fail dotnet format (whitespace, style and analyzers) or Terraform files fail
# terraform fmt. See docs/development.md#claude-code-hooks.
set -uo pipefail

input=$(cat)
# Avoid a loop: if this stop was already blocked once, let it through.
[ "$(jq -r '.stop_hook_active // false' <<< "$input")" = "true" ] && exit 0

cd "$CLAUDE_PROJECT_DIR" || exit 0
base=$(git merge-base HEAD origin/main 2>/dev/null || echo HEAD)
changed() {
  { git diff --name-only "$base"; git ls-files --others --exclude-standard; } |
    grep -E "$1" | grep -vE '/(bin|obj|\.terraform)/' | sort -u |
    while read -r f; do [ -f "$f" ] && echo "$f"; done
}
mapfile -t cs < <(changed '^(src|tests)/.*\.cs$' | grep -v '^src/Clercq\.It\.AppHost/')
mapfile -t tf < <(changed '^infra/.*\.(tf|tfvars)$')

failed=0
report=""
if [ ${#cs[@]} -gt 0 ] && command -v dotnet > /dev/null &&
  ! out=$(dotnet format Clercq.It.CI.slnf --verify-no-changes --include "${cs[@]}" 2>&1); then
  failed=1; report+=$'dotnet format (fix with: dotnet format Clercq.It.CI.slnf):\n'"$out"$'\n'
fi
if [ ${#tf[@]} -gt 0 ] && command -v terraform > /dev/null &&
  ! out=$(terraform fmt -check -diff "${tf[@]}" 2>&1); then
  failed=1; report+=$'terraform fmt:\n'"$out"$'\n'
fi

if [ "$failed" -eq 1 ]; then
  echo "Backend checks fail on changed files. Fix them before finishing:" >&2
  echo "$report" >&2
  exit 2
fi
exit 0
