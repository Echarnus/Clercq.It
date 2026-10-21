#!/bin/bash
# Prepares a Claude Code cloud session so agents can build, test and plan.
# Idempotent: every step is skipped when its tool is already present.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

cd "${CLAUDE_PROJECT_DIR:-$(pwd)}"

TERRAFORM_VERSION="1.6.6"
SCW_CLI_VERSION="2.40.0"
SUDO=""
if [ "$(id -u)" -ne 0 ]; then SUDO="sudo"; fi

# .NET SDK. Microsoft's download hosts are not reachable from the cloud
# environment, so use Ubuntu's dotnet-sdk-10.0 package. It builds the net9.0
# projects; DOTNET_ROLL_FORWARD lets net9.0 test hosts run on the 10.0 runtime.
if ! command -v dotnet >/dev/null 2>&1; then
  $SUDO apt-get update -qq
  DEBIAN_FRONTEND=noninteractive $SUDO apt-get install -y -qq dotnet-sdk-10.0
fi
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo 'export DOTNET_ROLL_FORWARD=Major'
    echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
    echo 'export DOTNET_NOLOGO=1'
    echo 'export PATH="$PATH:$HOME/.dotnet/tools"'
  } >> "$CLAUDE_ENV_FILE"
fi

# Terraform, for fmt/validate on infra changes.
if ! command -v terraform >/dev/null 2>&1; then
  curl -fsSL -o /tmp/terraform.zip \
    "https://releases.hashicorp.com/terraform/${TERRAFORM_VERSION}/terraform_${TERRAFORM_VERSION}_linux_amd64.zip"
  $SUDO unzip -o -q /tmp/terraform.zip -d /usr/local/bin
  rm -f /tmp/terraform.zip
fi

# Scaleway CLI, for reading secrets from Secret Manager (needs SCW_* env secrets).
if ! command -v scw >/dev/null 2>&1; then
  # Pinned: the GitHub API and "latest" redirects are not reachable from the cloud environment.
  if ! $SUDO curl -fsSL -o /usr/local/bin/scw \
    "https://github.com/scaleway/scaleway-cli/releases/download/v${SCW_CLI_VERSION}/scaleway-cli_${SCW_CLI_VERSION}_linux_amd64"; then
    echo "warning: could not download the Scaleway CLI; skipping" >&2
    $SUDO rm -f /usr/local/bin/scw
  else
    $SUDO chmod +x /usr/local/bin/scw
  fi
fi

# Restore .NET projects (never the Aspire AppHost, which needs the Aspire workload).
DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet restore src/ClercqIt.Api/ClercqIt.Api.csproj
for project in tests/ClercqIt.Api.Tests tests/Clercq.It.Application.Tests tests/Clercq.It.Domain.Tests tests/Clercq.It.ContentImport.Tests; do
  DOTNET_ROLL_FORWARD=Major DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet restore "$project"
done

# Frontend dependencies.
if ! command -v pnpm >/dev/null 2>&1; then
  npm install -g pnpm@10.12.4
fi
(cd src/ClercqIt.Web && pnpm install --frozen-lockfile)
