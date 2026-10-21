#!/bin/bash
# PostToolUse hook (Edit|Write|MultiEdit), issue #234.
# Formats the C# or Terraform file Claude just changed. Fast on purpose
# (whitespace only, no project load); style and analyzer findings are caught by
# backend-stop-check.sh. See docs/development.md#claude-code-hooks.
set -uo pipefail

file=$(jq -r '.tool_input.file_path // empty')
root="$CLAUDE_PROJECT_DIR"

case "$file" in
  */bin/* | */obj/* | */.terraform/*) exit 0 ;;
  "$root"/src/* | "$root"/tests/* | "$root"/infra/*) ;;
  *) exit 0 ;;
esac
[ -f "$file" ] || exit 0
cd "$root" || exit 0

case "$file" in
  *.cs)
    command -v dotnet > /dev/null || exit 0
    if ! out=$(dotnet format whitespace --folder --include "$file" 2>&1); then
      echo "dotnet format failed on $file:" >&2
      echo "$out" >&2
      exit 2
    fi
    ;;
  *.tf | *.tfvars)
    command -v terraform > /dev/null || exit 0
    if ! out=$(terraform fmt "$file" 2>&1); then
      echo "terraform fmt failed on $file (syntax error?):" >&2
      echo "$out" >&2
      exit 2
    fi
    ;;
esac
exit 0
