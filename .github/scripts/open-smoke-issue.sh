#!/usr/bin/env bash
# Open a p1 bug issue after a failed post-deploy smoke test (issue #232), so
# an agent picks it up. Reads VERSION, SHA, SITE_URL, FAILURES, RUN_URL and
# PREVIOUS_IMAGE from the environment; needs GH_TOKEN with issues: write.
set -euo pipefail

if [[ -n "${PREVIOUS_IMAGE:-}" ]]; then
  ROLLBACK="Production was rolled back to \`$PREVIOUS_IMAGE\`."
else
  ROLLBACK="There was no previous image to roll back to, so production may be broken."
fi

BODY=$(cat <<BODY
## What happened
The post-deploy smoke test failed for version \`$VERSION\` (commit $SHA) against $SITE_URL.

${FAILURES:-No failure details were captured.}

$ROLLBACK

Run: $RUN_URL

## Acceptance criteria
- [ ] Root cause found and fixed in a PR.
- [ ] The next deploy passes the smoke test.
BODY
)

gh issue create --repo "$GITHUB_REPOSITORY" \
  --title "Production smoke test failed for $VERSION" \
  --label "type:bug" --label "priority:p1" --label "status:ready" \
  --body "$BODY"
