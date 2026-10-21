#!/usr/bin/env bash
# Open a p1 bug issue when the verify stage of deploy.yml fails (issue #243).
# Production was not touched. Reads VERSION, SHA, FAILURES and RUN_URL from
# the environment; needs GH_TOKEN with issues: write.
set -euo pipefail

BODY=$(cat <<BODY
## What happened
Version \`$VERSION\` (commit $SHA) failed the verify stage, so it was not deployed to production. Production still runs the previous version.

${FAILURES:-See the run for the failing step.}

Run: $RUN_URL

## Acceptance criteria
- [ ] Root cause found and fixed in a PR.
- [ ] The next merge passes the verify stage and reaches production.
BODY
)

gh issue create --repo "$GITHUB_REPOSITORY" \
  --title "Verify stage failed for $VERSION" \
  --label "type:bug" --label "priority:p1" --label "status:ready" \
  --body "$BODY"
