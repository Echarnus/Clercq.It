#!/usr/bin/env bash
# Tests for check-pr-conventions.sh (issue #245).
set -uo pipefail

SCRIPT="$(dirname "$0")/../check-pr-conventions.sh"
failures=0

expect() {
  local want="$1" branch="$2" body="$3" got
  BRANCH="$branch" BODY="$body" bash "$SCRIPT" > /dev/null 2>&1 && got=pass || got=fail
  if [ "$got" = "$want" ]; then
    echo "ok   $want: $branch"
  else
    echo "FAIL expected $want, got $got: branch='$branch' body='$body'"
    failures=$((failures + 1))
  fi
}

expect pass "fix/231-admin-login" "Closes #231"
expect pass "chore/245-enforce-trunk-rules" $'Closes #245\n\n## What changed'
expect pass "security/237-blocking-sast" "Fixes #237"
expect pass "dependabot/nuget/FluentAssertions-8.11.0" ""
expect fail "feature/231-admin-login" "Closes #231"
expect fail "fix/admin-login" "Closes #231"
expect fail "fix/231-Admin_Login" "Closes #231"
expect fail "release/1.2" "Closes #231"
expect fail "develop" "Closes #231"
expect fail "claude/some-session" "Closes #231"
expect fail "fix/231-admin-login" "Part of #231"
expect fail "fix/231-admin-login" ""

if [ "$failures" -ne 0 ]; then
  echo "$failures test(s) failed"
  exit 1
fi
echo "All PR convention tests passed"
