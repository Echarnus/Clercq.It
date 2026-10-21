#!/bin/bash
# PostToolUse hook (Edit|Write|MultiEdit), issue #240.
# Formats and lints the frontend file Claude just changed. ESLint errors that
# --fix cannot solve go back to Claude (exit 2, details on stderr).
# Registered in .claude/settings.json; see docs/development.md#claude-code-hooks.
set -uo pipefail

file=$(jq -r '.tool_input.file_path // empty')
web="$CLAUDE_PROJECT_DIR/src/ClercqIt.Web"

case "$file" in
  "$web"/node_modules/* | "$web"/.next/*) exit 0 ;;
  "$web"/*) ;;
  *) exit 0 ;;
esac
[ -f "$file" ] || exit 0
cd "$web" || exit 0

case "$file" in
  *.ts | *.tsx | *.js | *.mjs)
    npx prettier --write --log-level warn "$file" >&2
    npx eslint --fix --cache "$file" > /dev/null 2>&1
    if ! out=$(npx eslint --cache --max-warnings 0 "$file" 2>&1); then
      echo "ESLint errors remain in $file. Fix the code; do not change the ESLint config or add a disable comment without a reason:" >&2
      echo "$out" >&2
      exit 2
    fi
    ;;
  *.json | *.css | *.md)
    npx prettier --write --log-level warn "$file" >&2
    ;;
esac
exit 0
