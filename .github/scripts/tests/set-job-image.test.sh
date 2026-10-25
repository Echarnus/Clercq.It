#!/usr/bin/env bash
# Checks that set-job-image.sh updates only the scheduled jobs (by name prefix),
# handles both shapes of `scw jobs definition list` output, and does nothing when
# there are no jobs. Uses a fake `scw` CLI that records its calls.
# Run: bash .github/scripts/tests/set-job-image.test.sh
set -uo pipefail

DIR="$(cd "$(dirname "$0")" && pwd)"
SCRIPT="$DIR/../set-job-image.sh"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

mkdir -p "$WORK/bin"
cat > "$WORK/bin/scw" <<'FAKE'
#!/usr/bin/env bash
if [[ "$1 $2 $3" == "jobs definition list" ]]; then
  cat "$SCW_LIST"
else
  echo "$*" >> "$SCW_CALLS"
fi
FAKE
chmod +x "$WORK/bin/scw"
export PATH="$WORK/bin:$PATH" SCW_CALLS="$WORK/calls" SCW_LIST="$WORK/list.json"

failed=0
check() { # check <description> <expected calls>
  if [[ "$(cat "$SCW_CALLS" 2>/dev/null)" == "$2" ]]; then
    echo "ok   $1"
  else
    echo "FAIL $1"; echo "     expected: $2"; echo "     got:      $(cat "$SCW_CALLS" 2>/dev/null)"; failed=1
  fi
  rm -f "$SCW_CALLS"
}

echo '[{"id":"aaa","name":"clercq-it-job-myzen-keepalive"},{"id":"bbb","name":"someone-elses-job"},{"id":"ccc","name":"clercq-it-job-other"}]' > "$SCW_LIST"
"$SCRIPT" echarnus/clercq-it@sha256:abc > /dev/null
check "updates every clercq-it job and nothing else" \
  $'jobs definition update aaa image-uri=docker.io/echarnus/clercq-it@sha256:abc\njobs definition update ccc image-uri=docker.io/echarnus/clercq-it@sha256:abc'

echo '{"job_definitions":[{"id":"ddd","name":"clercq-it-job-wrapped"}]}' > "$SCW_LIST"
"$SCRIPT" echarnus/clercq-it:1.2.3 > /dev/null
check "reads a list wrapped in job_definitions" "jobs definition update ddd image-uri=docker.io/echarnus/clercq-it:1.2.3"

echo '[{"id":"eee","name":"someone-elses-job"}]' > "$SCW_LIST"
"$SCRIPT" echarnus/clercq-it:1.2.3 > /dev/null
check "does nothing when no scheduled job exists" ""

echo '[{"id":"fff","name":"clercq-it-job-x"}]' > "$SCW_LIST"
"$SCRIPT" registry.example.com/team/app:9 > /dev/null
check "keeps an image that already names its registry" "jobs definition update fff image-uri=registry.example.com/team/app:9"

JOB_NAME_PREFIX=custom- "$SCRIPT" echarnus/clercq-it:1.2.3 > /dev/null
check "does nothing for a prefix nothing matches" ""

if "$SCRIPT" > /dev/null 2>&1; then
  echo "FAIL a missing image argument should fail"; failed=1
else
  echo "ok   a missing image argument fails"
fi

exit "$failed"
