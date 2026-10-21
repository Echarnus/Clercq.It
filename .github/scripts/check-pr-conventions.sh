#!/usr/bin/env bash
# Checks a pull request against the trunk-based conventions (issue #245):
# the head branch is <type>/<issue>-<slug> (or a bot branch) and the body
# links its issue with "Closes #<n>". See docs/contributing.md#enforced-rules.
#
# Usage: BRANCH=<head ref> BODY=<pr body> check-pr-conventions.sh
set -euo pipefail

BRANCH="${BRANCH:?BRANCH is required}"
BODY="${BODY:-}"

TYPES="feat|fix|chore|infra|content|docs|security"
BOT_PREFIXES="dependabot/|renovate/"

if [[ "$BRANCH" =~ ^($BOT_PREFIXES) ]]; then
  echo "Bot branch $BRANCH: naming and issue link not required."
  exit 0
fi

failed=0

if [[ "$BRANCH" =~ ^($TYPES)/([0-9]+)-[a-z0-9]+(-[a-z0-9]+)*$ ]]; then
  echo "Branch name OK: $BRANCH (issue #${BASH_REMATCH[2]})"
else
  echo "::error::Branch '$BRANCH' does not match <type>/<issue>-<slug>, with type one of ${TYPES//|/, } and a lowercase kebab-case slug (e.g. fix/231-admin-login)."
  failed=1
fi

if grep -Eiq '(^|[^[:alnum:]])(close[sd]?|fix(e[sd])?|resolve[sd]?) #[0-9]+' <<< "$BODY"; then
  echo "Issue link OK."
else
  echo "::error::The PR body must link its issue, e.g. 'Closes #231' on the first line."
  failed=1
fi

exit "$failed"
