#!/bin/bash
# Runs every gate a PR needs, as CI runs them, and prints a pass/fail table
# (issue #234). Usage: quality-check.sh [--fast]
#   --fast  skip the API integration tests, the coverage gates and next build
# Exit code 1 when any gate fails. See docs/development.md#quality-gates.
set -uo pipefail

fast=0
[ "${1:-}" = "--fast" ] && fast=1
root=$(git rev-parse --show-toplevel)
cd "$root" || exit 1
export CHECKPOINT_DISABLE=1
log_dir=$(mktemp -d)
rows=()
failed=0

# step <name> <command...>: runs the command from the repo root, logs its output.
step() {
  local name=$1; shift
  local log="$log_dir/$(echo "$name" | tr -c 'a-zA-Z0-9' '_').log"
  echo "▶ $name" >&2
  if (cd "$root" && "$@") > "$log" 2>&1; then
    rows+=("| $name | ✅ pass | |")
  else
    rows+=("| $name | ❌ fail | \`$log\` |")
    failed=1
  fi
}
skip() { rows+=("| $1 | ⏭️ skipped | $2 |"); }

# Docs and pipeline scripts
step "Docs check" python3 .github/scripts/docs_check.py
step "Pipeline script tests" python3 -m unittest discover -s .github/scripts/tests -q

# Backend
if command -v dotnet > /dev/null; then
  rm -rf "$root/coverage"
  step "dotnet format" dotnet format Clercq.It.CI.slnf --verify-no-changes
  step "dotnet build (warnings as errors)" dotnet build Clercq.It.CI.slnf -c Release
  step "Unit and architecture tests" dotnet test Clercq.It.CI.slnf -c Release --no-build \
    --filter "FullyQualifiedName!~ClercqIt.Api.Tests" --collect:"XPlat Code Coverage" --results-directory ./coverage
  if [ $fast -eq 1 ]; then
    skip "API integration tests" "--fast"
    skip "Backend coverage gate" "--fast"
  elif ! docker info > /dev/null 2>&1; then
    skip "API integration tests" "Docker is not running (\`dockerd > /tmp/dockerd.log 2>&1 &\`)"
    skip "Backend coverage gate" "needs the integration tests"
  else
    step "API integration tests" dotnet test tests/ClercqIt.Api.Tests -c Release --no-build \
      --collect:"XPlat Code Coverage" --results-directory ./coverage
    step "Backend coverage gate" bash -c 'dotnet tool restore > /dev/null &&
      dotnet tool run reportgenerator -reports:"./coverage/**/coverage.cobertura.xml" \
        -targetdir:./coverage/report -reporttypes:JsonSummary > /dev/null &&
      python3 .github/scripts/check_coverage.py ./coverage/report/Summary.json tests/coverage-thresholds.json'
  fi
else
  skip "Backend" "dotnet is not installed"
fi

# Frontend
if command -v pnpm > /dev/null; then
  web=src/ClercqIt.Web
  step "pnpm install" pnpm --dir $web install --frozen-lockfile
  step "ESLint" pnpm --dir $web lint
  step "Prettier" pnpm --dir $web format:check
  step "TypeScript" pnpm --dir $web typecheck
  step "Vitest with coverage gate" pnpm --dir $web test:coverage
  if [ $fast -eq 1 ]; then skip "next build" "--fast"; else step "next build" pnpm --dir $web build; fi
else
  skip "Frontend" "pnpm is not installed"
fi

# Infra
if command -v terraform > /dev/null; then
  step "terraform fmt" terraform -chdir=infra/terraform fmt -check -recursive
  step "terraform validate" bash -c 'cd infra/terraform && terraform init -backend=false -input=false > /dev/null && terraform validate'
else
  skip "Terraform" "terraform is not installed"
fi

echo
echo "| Gate | Result | Log |"
echo "| --- | --- | --- |"
printf '%s\n' "${rows[@]}"
exit $failed
