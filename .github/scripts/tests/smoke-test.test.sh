#!/usr/bin/env bash
# Tests for .github/scripts/smoke-test.sh against a fake site (fake-site.py).
# Run: bash .github/scripts/tests/smoke-test.test.sh
set -uo pipefail

DIR="$(cd "$(dirname "$0")" && pwd)"
SMOKE="$DIR/../smoke-test.sh"
PORT=18765
failed=0

run_case() {
  local mode="$1" expected_exit="$2" server_pid actual_exit
  MODE="$mode" python3 "$DIR/fake-site.py" "$PORT" &
  server_pid=$!
  for _ in $(seq 1 50); do
    curl -s -o /dev/null "http://127.0.0.1:$PORT/" && break
    sleep 0.1
  done
  SMOKE_ATTEMPTS=2 SMOKE_DELAY=0 GITHUB_OUTPUT= "$SMOKE" "http://127.0.0.1:$PORT" 1.2.3 > /tmp/smoke-$mode.log 2>&1
  actual_exit=$?
  kill "$server_pid" 2>/dev/null
  wait "$server_pid" 2>/dev/null
  if [[ "$actual_exit" == "$expected_exit" ]]; then
    echo "ok   $mode (exit $actual_exit)"
  else
    echo "FAIL $mode: expected exit $expected_exit, got $actual_exit"
    sed 's/^/     /' /tmp/smoke-$mode.log
    failed=1
  fi
}

run_case healthy 0
run_case old-version 1
run_case broken-home 1
run_case broken-api 1
run_case html-api 1

exit "$failed"
