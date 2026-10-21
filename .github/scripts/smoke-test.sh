#!/usr/bin/env bash
# Post-deploy smoke test for production (issue #232).
#
# Usage: smoke-test.sh <base-url> <expected-version>
#
# Passes when, within SMOKE_ATTEMPTS tries (SMOKE_DELAY seconds apart):
#   - <base-url>/version.json reports <expected-version>
#   - <base-url>/ returns 200
#   - <base-url>/api/projects returns 200 with a JSON body
# Retries cover cold starts of the min_scale=0 container and image caching.
set -uo pipefail

BASE_URL="${1:?base url required}"
BASE_URL="${BASE_URL%/}"
EXPECTED_VERSION="${2:?expected version required}"
ATTEMPTS="${SMOKE_ATTEMPTS:-10}"
DELAY="${SMOKE_DELAY:-15}"

failures=()

check_version() {
  local body version
  body=$(curl -fsS --max-time 20 "$BASE_URL/version.json") || { failures+=("version.json: request failed"); return 1; }
  version=$(jq -r '.version // empty' <<<"$body" 2>/dev/null)
  if [[ "$version" != "$EXPECTED_VERSION" ]]; then
    failures+=("version.json: expected $EXPECTED_VERSION, got ${version:-nothing}")
    return 1
  fi
}

check_home() {
  local status
  status=$(curl -sS -o /dev/null -w '%{http_code}' --max-time 20 "$BASE_URL/") || status=000
  if [[ "$status" != "200" ]]; then
    failures+=("/: expected 200, got $status")
    return 1
  fi
}

check_projects() {
  local tmp status
  tmp=$(mktemp)
  status=$(curl -sS -o "$tmp" -w '%{http_code}' --max-time 20 -H 'Accept: application/json' "$BASE_URL/api/projects") || status=000
  if [[ "$status" != "200" ]]; then
    failures+=("/api/projects: expected 200, got $status")
    rm -f "$tmp"
    return 1
  fi
  if ! jq -e . "$tmp" > /dev/null 2>&1; then
    failures+=("/api/projects: body is not JSON")
    rm -f "$tmp"
    return 1
  fi
  rm -f "$tmp"
}

for attempt in $(seq 1 "$ATTEMPTS"); do
  failures=()
  check_version
  check_home
  check_projects
  if [[ ${#failures[@]} -eq 0 ]]; then
    echo "Smoke test passed on attempt $attempt/$ATTEMPTS ($BASE_URL, version $EXPECTED_VERSION)"
    exit 0
  fi
  echo "Attempt $attempt/$ATTEMPTS failed:"
  printf '  - %s\n' "${failures[@]}"
  if [[ "$attempt" -lt "$ATTEMPTS" ]]; then
    sleep "$DELAY"
  fi
done

echo "Smoke test failed after $ATTEMPTS attempts:"
printf '  - %s\n' "${failures[@]}"
if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  {
    echo "failures<<SMOKE_EOF"
    printf -- '- %s\n' "${failures[@]}"
    echo "SMOKE_EOF"
  } >> "$GITHUB_OUTPUT"
fi
exit 1
