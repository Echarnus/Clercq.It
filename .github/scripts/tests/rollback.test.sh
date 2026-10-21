#!/usr/bin/env bash
# Simulates a deploy of a broken image: the smoke test fails, so the deploy
# rolls the container back to the previous image. Uses a fake `scw` CLI that
# records its calls, and fake-site.py as the broken site.
# Run: bash .github/scripts/tests/rollback.test.sh
set -uo pipefail

DIR="$(cd "$(dirname "$0")" && pwd)"
SCRIPTS="$DIR/.."
WORK="$(mktemp -d)"
PORT=18766
trap 'kill "$server_pid" 2>/dev/null; rm -rf "$WORK"' EXIT

# Fake scw: log every call.
mkdir -p "$WORK/bin"
cat > "$WORK/bin/scw" <<'FAKE'
#!/usr/bin/env bash
echo "$*" >> "$SCW_CALLS"
FAKE
chmod +x "$WORK/bin/scw"
export PATH="$WORK/bin:$PATH" SCW_CALLS="$WORK/calls" SETTLE_SECONDS=0

MODE=broken-api python3 "$DIR/fake-site.py" "$PORT" &
server_pid=$!
for _ in $(seq 1 50); do curl -s -o /dev/null "http://127.0.0.1:$PORT/" && break; sleep 0.1; done

# Same sequence as deploy.yml: deploy new image, smoke test, roll back on failure.
"$SCRIPTS/set-container-image.sh" ctr-1 echarnus/clercq-it:1.2.3 > /dev/null
if SMOKE_ATTEMPTS=1 SMOKE_DELAY=0 GITHUB_OUTPUT= "$SCRIPTS/smoke-test.sh" "http://127.0.0.1:$PORT" 1.2.3 > /dev/null; then
  echo "FAIL smoke test passed against a broken site"
  exit 1
fi
"$SCRIPTS/set-container-image.sh" ctr-1 echarnus/clercq-it:1.2.2 > /dev/null

last_image=$(grep -o 'registry-image=[^ ]*' "$SCW_CALLS" | tail -1)
if [[ "$last_image" == "registry-image=echarnus/clercq-it:1.2.2" ]]; then
  echo "ok   broken image rolled back to the previous image"
else
  echo "FAIL expected rollback to 1.2.2, scw calls were:"
  sed 's/^/     /' "$SCW_CALLS"
  exit 1
fi
