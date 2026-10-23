# DevOps & CI/CD Pipeline

This document describes the Continuous Integration and Continuous Deployment (CI/CD) workflows used to test, build, and deploy the Clercq.It application.

## Overview

The CI/CD pipeline is built using **GitHub Actions** and follows **trunk-based development** with **continuous deployment to production** (see [Versioning](./versioning.md)). The pipeline uses GitVersion for automatic semantic versioning.

## Pipeline Workflows

The project uses six main workflows:

1. **Test Pipeline** (`test.yml`) - Runs on every push and PR with security checks
2. **Build Pipeline** (`build.yml`) - Builds and publishes Docker images with vulnerability scanning
3. **Security Scanning** (`security-scan.yml`) - Comprehensive security analysis
4. **CodeQL Analysis** (`codeql.yml`) - Advanced semantic code security analysis
5. **Infrastructure Pipeline** (`infra.yml`) - Manages Terraform infrastructure
6. **Deploy Pipeline** (`deploy.yml`) - Verifies the image in the verify environment, then deploys the same digest to production
7. **Docs check** (`docs-check.yml`) - Fails a PR that changes code, infra or workflows without updating `docs/`
8. **Sync labels** (`labels.yml`) - Applies `.github/labels.yml` to the repository
9. **SAST** (`sast.yml`) - Semgrep and gitleaks on every PR and on `main`, blocking (see [Application security](./application-security.md#static-analysis-sast))

How issues, branches, PRs and reviews fit together is described in [Contributing](./contributing.md). The rules GitHub enforces on `main` and on release tags, and the weekly audit that checks them, are in [Enforced rules](./contributing.md#enforced-rules).

Additionally, **Dependabot** runs automatically to keep dependencies secure and up to date.

## Workflow and review pipelines

### Docs check (`docs-check.yml`)
Runs `.github/scripts/docs_check.py` on every PR to `main` (skipped for Dependabot). If the PR touches `src/`, `tests/`, `infra/`, `.github/workflows/`, `.github/actions/` or `.claude/`, it must also change a file under `docs/`, `CLAUDE.md` or `README.md`; areas with their own doc (infra, pipelines, container image, coverage thresholds, Keycloak realm, Claude Code hooks and skills) must change that doc; and changed docs must not link to missing files. The `docs:not-needed` label overrides the first two rules; the PR body must then say why. Details: [contributing.md](./contributing.md#docs-check).

### Sync labels (`labels.yml`)
Runs on pushes to `main` that change `.github/labels.yml`, or by hand. Creates or updates each label listed there; labels not in the file are left alone.

## Test Pipeline (`test.yml`)

### Triggers
- Called by `build.yml` on push to `main`
- Pull requests to `main`

### Jobs

#### test-dotnet
- Uses .NET 10
- Runs xUnit tests with code coverage
- Uploads coverage to Codecov
- Tests located in `/tests/` directory

#### test-frontend
- Uses Node.js 24 (LTS) and pnpm, the same Node major as the Docker image (`node:24-slim`)
- Runs Jest tests and ESLint
- Builds Next.js application to verify compilation


#### docker-build
- Pull requests only
- Builds the production image from `src/Dockerfile` for `linux/amd64` without pushing it, so a broken Dockerfile fails the PR instead of the build on `main`. The .NET stage copies `src/nuget.config` and `src/Directory.Packages.props` (central package versions) before `dotnet restore`; the build context is `src/`, so files at the repo root are not in the image.

#### deploy-scripts
- Pull requests only
- Runs `.github/scripts/tests/smoke-test.test.sh` (the smoke test against a fake healthy and broken site) and `rollback.test.sh` (a broken image is rolled back to the previous one, with a fake `scw` CLI)

### Example

```bash
# Runs automatically on push/PR
# Or manually trigger with
gh workflow run test.yml
```

## Build Pipeline (`build.yml`)

### Triggers
- Push to `main` branch only
- Manual workflow dispatch with optional branch selection
- Pull requests (test only, no build/push)

### Jobs

#### test
- Calls the test pipeline to ensure all tests pass

#### build
- Uses GitVersion for semantic versioning
- Builds `linux/amd64` only: Scaleway serverless containers run amd64, and an emulated arm64 build added 20+ minutes per merge
- Publishes to Docker Hub with multiple tags:
  - Semantic version (e.g., `1.0.1`)
  - Short SHA (e.g., `abc1234`)
  - `latest` (for main branch)
- Generates build attestation for security

### Third-party actions

`aquasecurity/trivy-action` is pinned to a commit SHA (v0.36.0). Its old unprefixed tags such as `0.28.0` no longer exist upstream, which is what broke every build on `main` from 10 March 2026. Prefer SHA pins for third-party actions.

### Docker Image Tags

```
echarnus/clercq-it:1.0.1      # Semantic version
echarnus/clercq-it:latest     # Main branch only
echarnus/clercq-it:abc1234    # Short SHA
```

### Example

```bash
# Runs automatically on push to main
# Or manually trigger with
gh workflow run build.yml
```

## Security Scanning Pipeline (`security-scan.yml`)

### Triggers
- Push to `main`
- Pull requests to `main`
- Daily schedule at 2:00 AM UTC
- Manual workflow dispatch

### Jobs

#### trivy-container-scan
- Builds Docker image for scanning
- Runs Trivy vulnerability scanner
- Uploads results to GitHub Security tab
- Scans for CRITICAL, HIGH, and MEDIUM severity vulnerabilities
- **Fails** when the image does not build, or on a fixable CRITICAL or HIGH finding not listed in `.trivyignore`

#### trivy-filesystem-scan
- Scans entire repository filesystem
- Detects IaC misconfigurations
- Identifies dependency vulnerabilities
- Uploads SARIF results to GitHub Security
- **Fails** on a fixable CRITICAL or HIGH finding not listed in `.trivyignore`

#### dotnet-security-scan
- Lists vulnerable NuGet packages (direct and transitive) for `Clercq.It.CI.slnf`
- **Fails on High or Critical**; Low and Moderate are listed only
- Uploads the list as an artifact

#### npm-security-scan
- Runs pnpm audit on Node.js dependencies
- Checks for known vulnerabilities
- Generates JSON audit reports
- Uploads reports as artifacts
- **Fails on High or Critical** (`pnpm audit --audit-level=high`, also in `test.yml`); accepted exceptions are in [Application security](./application-security.md#accepted-findings)

Secret scanning moved to gitleaks in `sast.yml` (see [Application security](./application-security.md#static-analysis-sast)).

#### security-scorecard
- Runs OpenSSF Scorecard evaluation on pushes to `main` and the daily schedule (not on PRs), with `ossf/scorecard-action` v2.4.4 pinned by commit SHA. Versions before 2.4.2 pull their image from `gcr.io`, which no longer serves it, so the job failed on every run (#286).
- Assesses security best practices
- Publishes results to GitHub Security
- Provides security posture visibility

### Example

```bash
# Runs automatically on push/PR and daily
# Or manually trigger with
gh workflow run security-scan.yml
```

## CodeQL Analysis Pipeline (`codeql.yml`)

### Triggers
- Push to `main`
- Pull requests to `main`
- Weekly schedule on Mondays at 8:00 AM UTC
- Manual workflow dispatch

### Jobs

#### analyze-csharp
- Initializes CodeQL for C# language
- Uses security-extended query suite
- Builds .NET projects
- Performs deep semantic analysis
- Uploads results to GitHub Security tab

#### analyze-javascript
- Initializes CodeQL for JavaScript/TypeScript
- Uses security-extended query suite
- Analyzes Next.js frontend code
- Detects security vulnerabilities
- Uploads results to GitHub Security tab

### Example

```bash
# Runs automatically on schedule and push/PR
# Or manually trigger with
gh workflow run codeql.yml
```

## Infrastructure Pipeline (`infra.yml`)

### Triggers
- Push to `main` branch with changes to `infra/**` files only
- Pull requests with changes to `infra/**` files
- Manual workflow dispatch

**Note**: The pipeline only runs when infrastructure files are modified to prevent conflicts with application deployments. This ensures Terraform doesn't revert container image versions set by the deploy pipeline.

### Jobs

#### terraform-check
- Validates Terraform formatting and configuration
- Runs on all infrastructure changes

#### terraform-plan
- Creates Terraform execution plan
- Runs on pull requests
- Uses `infrastructure-plan` environment
- A failing `terraform plan` fails the job (`set -o pipefail`, since the output is piped through `tee`). Before this fix the check stayed green on a broken plan. The plan is still posted as a PR comment when it fails.

#### terraform-apply
- Applies Terraform changes to production
- Runs on main branch push
- Uses `production` environment with manual approval
- Requires reviewer approval before execution

#### terraform-destroy
- Manual workflow to destroy infrastructure
- Uses `production` environment with manual approval
- Requires explicit confirmation

### Infrastructure Components

- **Serverless Container**: Auto-scales 0-1 vCPU with 512MB memory
- **Serverless SQL**: PostgreSQL database with minimal resource allocation
- **Object Storage**: S3-compatible bucket for blog images with public-read ACL
- **Organization**: ClercqIt with Portfolio namespace
- **Cockpit Logging**: Scaleway Cockpit for centralized logs and metrics via OpenTelemetry
- **Cost Optimization**: Infrastructure scales to zero when not in use

- **Feature flags**: `infra/terraform/feature_flags.tf` holds the production flags; each becomes a `FeatureFlags__<Name>` env var on the website and verify containers ([versioning.md](./versioning.md#feature-flags))
- **Verify environment**: `clercq-it-verify` container, `clercqit_verify` database, `clercq-it-verify-images` bucket (`verify.tf`, see [Verify stage](#verify-stage))

**Note**: Custom domains are managed manually in the Scaleway console and not via Terraform to avoid conflicts.

### Terraform Backend Configuration

The infrastructure uses Scaleway Object Storage as a Terraform backend for state management:

- **Backend Type**: S3-compatible (Scaleway Object Storage)
- **Bucket**: `clercq-it-terraform-state`
- **Region**: `fr-par` (Paris)
- **State File**: `portfolio/terraform.tfstate`

**Important**: This backend does not support state locking. Avoid concurrent Terraform operations.

**Credentials**: The backend uses AWS-compatible environment variables:
```bash
AWS_ACCESS_KEY_ID=$SCW_ACCESS_KEY
AWS_SECRET_ACCESS_KEY=$SCW_SECRET_KEY
```

These are automatically set in the GitHub Actions workflow using repository secrets.

**References**:
- [Scaleway Backend Guide](https://registry.terraform.io/providers/scaleway/scaleway/latest/docs/guides/backend_guide)
- [Terraform S3 Backend Documentation](https://developer.hashicorp.com/terraform/language/backend/s3)

### Example

```bash
# View plan for PR changes
gh pr view <pr-number> --comments

# Manually apply (after merge to main, with approval)
# Triggered automatically or via
gh workflow run infra.yml
```

## Deploy Pipeline (`deploy.yml`)

### Triggers
- Automatic: When build pipeline completes successfully on `main` branch
- Manual: Workflow dispatch with version parameter

### Jobs

#### deploy
- Validates the Docker image exists and resolves its digest
- Reads container IDs, database and bucket names for both environments from the Terraform outputs
- **Verify stage** (see [Verify stage](#verify-stage)): migrates the verify database, imports content into it, deploys the image to `clercq-it-verify` and smoke-tests it. A failure here opens a p1 issue and stops the run before production is touched
- Checks the tag still points at the verified digest
- Runs database migrations, then imports `content/` into the database (see [Content](./content.md#how-the-import-runs))
- Records the image the container runs now (`PREVIOUS_IMAGE`)
- Sets the new image and forces container recreation to bypass the image cache (scales min_scale: 0→1→0), with `.github/scripts/set-container-image.sh`
- Runs the smoke test (see [Smoke test and rollback](#smoke-test-and-rollback))
- On a failed smoke test: rolls back to `PREVIOUS_IMAGE`, opens a p1 bug issue and fails the run, so no release is tagged
- Reports deployment status

**Concurrency**: `deploy.yml` runs in the `production-deploy` concurrency group: one deploy at a time, and when several are waiting only the newest runs, so an older build that finishes late cannot replace a newer version. The `terraform-deploy` job in `infra.yml` has its own `production-terraform` group, so applies never race on the state.

#### release
- Runs only after an automatic deploy (not for manual deploys)
- Creates tag `prod-<version>` on the deployed commit and a GitHub Release with generated notes
- Uses the `prod-` prefix so GitVersion ignores the tag (see [Versioning](./versioning.md#production-releases))

**Note**: The deployment implements a cache-busting strategy to work around Scaleway's image caching behavior with `min_scale=0` containers. This ensures fresh images are always pulled from the registry by forcing container recreation through a temporary scale-up/scale-down cycle.

### Verify stage

Trunk-based means every merge to `main` goes to production. So before production is touched, `deploy.yml` runs the real image against real infrastructure. It is one pipeline, not a branch or a manual staging step:

```
merge to main
  └─ build.yml: tests → image pushed (tag = version, digest D) → migration.sql
       └─ deploy.yml (one job, stops at the first failure)
            1. resolve digest D of echarnus/clercq-it:<version>
            2. verify: migrate clercqit_verify → import content → deploy image to clercq-it-verify → smoke test
               (e2e #246 and DAST #239 join here)
               ✗ → issue "Verify stage failed for <version>" (p1), production untouched, no release
            3. tag still resolves to D?  ✗ → stop
            4. production: migrate → import content → deploy image → smoke test
               ✗ → roll back, issue "Production smoke test failed for <version>" (p1), no release
            5. release job: tag prod-<version>
```

| Gate | Fails when | Effect |
|------|------------|--------|
| Image check | The tag is missing or has no digest | Stop, p1 issue, production untouched |
| Verify migration / content import | `migration.sql` or a content file breaks against a real PostgreSQL | Stop, p1 issue, production untouched |
| Verify smoke test | `/version.json`, `/` or `/api/projects` fail on the verify URL | Stop, p1 issue, production untouched |
| Digest check | The tag moved after verification | Stop, production untouched |
| Production smoke test | Same checks on https://www.clercq.it | Roll back, p1 issue |

**What verify shares with production**: only the RDB instance and the container namespace. It has its own database (`clercqit_verify`, user `clercqit_verify_user`), bucket (`clercq-it-verify-images`), storage key, Cockpit token and secrets (`/clercq-it/verify`, see [Secrets](./secrets.md#verify-environment-path-clercq-itverify)). It scales to zero, so it costs almost nothing.

**Same image**: production is set to the same tag only after the pipeline checks that the tag still resolves to the digest that passed verification. Terraform ignores `registry_image` on both containers (`lifecycle.ignore_changes`), so an infra apply never swaps the running image for `:latest`.

**Manual deploys** (`workflow_dispatch`) also go through the verify container and its smoke test. As before, they skip migrations and the content import.

**Login on verify** uses its own public PKCE client, `clercqit-web-verify`, configured like `clercqit-web` (see [Cloud IAM](./ciam.md#3-create-clients)) with the verify URL as redirect URI. It has no secret. The smoke test does not need sign-in.

The verify URL is the `verify_container_url` Terraform output (a Scaleway container domain).

### Manual Deployment

```bash
# Deploy specific version
gh workflow run deploy.yml -f version=1.0.1

# Deploy latest version
gh workflow run deploy.yml -f version=latest
```

### Smoke test and rollback

Every deploy, automatic or manual, ends with `.github/scripts/smoke-test.sh https://www.clercq.it <version>`. It passes when, within 10 tries 15 seconds apart (to cover cold starts):

| Check | Passes when |
|-------|-------------|
| `/version.json` | `version` equals the version just deployed |
| `/` | HTTP 200 |
| `/api/projects` | HTTP 200 with a JSON body (so the API and database work) |

If it fails, the pipeline:

1. Sets the container back to the image it ran before the deploy (`set-container-image.sh`). If there was no previous image, it skips this step and says so in the issue.
2. Opens an issue `Production smoke test failed for <version>` labelled `type:bug`, `priority:p1`, `status:ready`, with the failed checks and the run link (`open-smoke-issue.sh`), so an agent picks it up.
3. Fails the run. The `release` job does not run, so the broken version gets no `prod-` tag.

#### Rolling back by hand

Redeploy an earlier version. It goes through the same smoke test:

```bash
gh workflow run deploy.yml -f version=1.0.189
```

Or directly with the Scaleway CLI, when the pipeline itself is broken:

```bash
scw container container list name=clercq-it-app            # find the container ID
.github/scripts/set-container-image.sh <container-id> echarnus/clercq-it:1.0.189
```

Released versions are listed as GitHub Releases (`prod-<version>` tags).

## Required Secrets

Application secrets live in **Scaleway Secret Manager** (see [Secrets](./secrets.md)). GitHub keeps only what the pipelines need to reach Scaleway and Docker Hub, plus seed values that are read once while the secrets are created.

### Scaleway (GitHub repository secrets)
- `SCALEWAY_ACCESS_KEY`: Scaleway API access key (also used as AWS_ACCESS_KEY_ID for the S3 state backend). Needs IAM manager rights, because Terraform creates IAM applications and policies.
- `SCALEWAY_SECRET_KEY`: Scaleway API secret key (also used as AWS_SECRET_ACCESS_KEY)
- `SCALEWAY_ORGANIZATION_ID`: Scaleway organization ID
- `SCALEWAY_PROJECT_ID`: Scaleway project ID

### Docker Hub (GitHub repository secrets)
- `DOCKER_USERNAME`: Docker Hub username
- `DOCKER_PASSWORD`: Docker Hub access token

### Seeds and transitional secrets (GitHub repository secrets)
- `DATABASE_PASSWORD`, `ADMINISTRATION_DATABASE_PASSWORD`, `KEYCLOAK_BASE_URL`, `KEYCLOAK_WEB_CLIENT_SECRET`, `KEYCLOAK_ADMIN_CLIENT_SECRET`: passed as `TF_VAR_*` and used only to seed the matching Secret Manager secret when it is first created. After that Terraform ignores them. `DATABASE_PASSWORD` is still read by the migration step in `deploy.yml` until the workflows read from Secret Manager directly.
- `KEYCLOAK_REALM`: optional, defaults to `clercqit`.

### Environment Variables

- `REGISTRY`: Docker registry (docker.io)
- `IMAGE_NAME`: Docker image name (echarnus/clercq-it)
- `CONTAINER_IMAGE`: Container image tag for deployment (optional)

## Deployment Process

### Automatic Deployment (Main Branch)

1. Developer pushes to `main` branch
2. Test pipeline runs automatically
3. If tests pass, build pipeline starts
4. Docker image is built with semantic version
5. Image is pushed to Docker Hub
6. Deploy pipeline is triggered automatically
7. The image is verified in the verify environment ([Verify stage](#verify-stage))
8. The same digest is deployed to production

### Manual Deployment

1. Trigger deploy workflow manually
2. Specify version to deploy
3. Pipeline validates image exists
4. Deployment proceeds with specified version

## Monitoring and Status

### Scaleway Cockpit

The application integrates with Scaleway Cockpit for centralized logging and monitoring:

- **Logs**: Application logs are forwarded to Cockpit using OpenTelemetry (OTLP)
- **Metrics**: Performance metrics are collected automatically
- **Dashboards**: Access Grafana dashboards for visualization
- **Traces**: Distributed tracing for request tracking

**Note**: Scaleway Cockpit is now enabled by default on all projects. After infrastructure deployment, access the Cockpit dashboard via the Scaleway console at:
- Navigate to **Observability** > **Cockpit** in your Scaleway project
- Use the project ID from Terraform outputs to locate your project

```bash
cd infra/terraform
terraform output cockpit_project_id
terraform output cockpit_token_id
```

The container is configured to send telemetry data automatically via environment variables:
- `OTEL_EXPORTER_OTLP_ENDPOINT`: Cockpit OTLP endpoint
- `OTEL_EXPORTER_OTLP_PROTOCOL`: gRPC protocol
- `OTEL_EXPORTER_OTLP_HEADERS`: Authentication token

### Build Status Badges

The README includes status badges for all workflows:

```markdown
[![Test](https://github.com/Echarnus/Clercq.It/actions/workflows/test.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/test.yml)
[![Build](https://github.com/Echarnus/Clercq.It/actions/workflows/build.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/build.yml)
[![Deploy](https://github.com/Echarnus/Clercq.It/actions/workflows/deploy.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/deploy.yml)
[![Infra](https://github.com/Echarnus/Clercq.It/actions/workflows/infra.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/infra.yml)
```

### Deployment Status

Each deployment provides detailed status information:
- Version deployed
- Docker image used
- Deployment source (automatic vs manual)
- Environment information

## Security Features

### Automated Security Scanning

The CI/CD pipeline includes comprehensive automated security scanning:

#### Dependency Security
- **Dependabot**: Automated dependency updates across all ecosystems
  - .NET NuGet packages
  - Node.js npm packages  
  - GitHub Actions
  - Docker base images
  - Terraform providers
- **npm audit**: Security audits for Node.js dependencies in test pipeline
- **dotnet list package --vulnerable**: Checks for known .NET vulnerabilities in test pipeline

#### Code Security Analysis
- **CodeQL**: Advanced semantic code analysis
  - Analyzes C# code for security vulnerabilities
  - Analyzes JavaScript/TypeScript code
  - Runs extended security query suite
  - Scheduled weekly scans
- **Security linting**: ESLint security rules for frontend code

#### Container Security
- **Trivy vulnerability scanner**: Comprehensive container security
  - Scans Docker images for OS vulnerabilities
  - Scans application dependencies
  - Filesystem scanning for IaC misconfigurations
  - Integrated into build pipeline with severity filtering
  - Results uploaded to GitHub Security tab
- **.NET vulnerable packages**: `dotnet list package --vulnerable`, blocking on High/Critical
- **Semgrep, gitleaks, CodeQL gate**: blocking SAST on every PR, see [Application security](./application-security.md#static-analysis-sast)

#### Secret Detection
- **gitleaks** (`sast.yml`): scans the PR's commits, and the full history on `main`; blocking
- **GitHub Secret Scanning**: Native GitHub protection

#### Security Posture
- **OpenSSF Scorecard**: Evaluates repository security practices
  - Automated scoring of security best practices
  - Published results for transparency
  - Regular automated assessments

### Build Security
- **Build attestation**: Signed build provenance for container images
- **Single platform**: `linux/amd64`, the architecture Scaleway serverless containers run
- **Vulnerability scanning**: Trivy scans on every build
- **Secret management**: Sensitive data stored in GitHub Secrets
- **Least privilege**: Minimal permissions for GitHub Actions

### Container Security
- **Non-root execution**: Containers run with non-privileged users
- **Minimal attack surface**: Alpine-based images with minimal packages
- **Health checks**: Built-in container health monitoring
- **Image attestation**: Signed provenance for supply chain security

### Security Workflows

#### security-scan.yml
Comprehensive security scanning workflow:
- **Triggers**: Push, PR, daily schedule, manual
- **Jobs**:
  - Trivy container scanning
  - Trivy filesystem scanning
  - .NET security analysis
  - npm security audit
  - OpenSSF Scorecard

#### codeql.yml
Advanced code analysis:
- **Triggers**: Push, PR, weekly schedule, manual
- **Languages**: C#, JavaScript/TypeScript
- **Analysis**: Security-extended queries

#### Dependabot
Automated dependency updates (`.github/dependabot.yml`):
- **Schedule**: Weekly on Mondays at 8:00 AM UTC
- **Ecosystems**: NuGet, npm (`src/ClercqIt.Web`), GitHub Actions, Docker, Terraform
- **Grouping**: all patch and minor updates for an ecosystem arrive as one grouped PR per week (e.g. `nuget-minor-patch`, `web-minor-patch`). Major updates arrive as separate PRs.
- **Review**: no default assignee or reviewer. The agent reviewer handles dependency PRs like any other PR: a grouped patch/minor PR is merged once CI is green. A major update PR is not merged directly; it is closed with a link to a planned upgrade issue, so the upgrade is done deliberately with code changes, docs and tests.
- **Labeling**: `dependencies` plus the ecosystem label (`dotnet`, `frontend`, `ci/cd`, `docker`, `infrastructure`)
- **Stale PRs**: to refresh a Dependabot PR whose base is far behind `main`, comment `@dependabot recreate`. Closing a Dependabot PR makes Dependabot skip that version; it only proposes the dependency again when a newer version is released.
- **Security updates**: grouping applies to version updates only; Dependabot security updates still arrive as one PR per dependency.
- **Ignored versions**: `FluentAssertions` `>= 8` is ignored for NuGet, because version 8 has a commercial licence and the tests stay on 7.x (#290). Patch and minor 7.x updates still arrive in the grouped PR. To add or drop an ignore rule, edit the `ignore` list of that ecosystem in `.github/dependabot.yml` and this line together.

## Best Practices

### Development
1. Always create short-lived work branches from `main` (`<type>/<issue>-<slug>`)
2. Write tests for new functionality
3. Keep commits focused and descriptive
4. Use conventional commit messages for better versioning

### Deployment
1. Test locally before pushing
2. Monitor deployment status and logs
3. Fixes go through a normal `fix/...` branch and PR; use manual deployment only to roll back to an earlier version
4. Every automatic deploy is tagged `prod-<version>` with a GitHub Release (see [Versioning](./versioning.md#production-releases))

### Security
1. **Keep the Dependabot queue short**: merge green grouped PRs weekly; plan majors as issues
2. **Monitor security alerts**: Check GitHub Security tab regularly
3. **Run local security scans**: Test before pushing changes
4. **Never commit secrets**: Use environment variables and GitHub Secrets
5. **Review security scan results**: Address findings before merging
6. **Use least-privilege access**: Minimal permissions for secrets and tokens
7. **Keep base images updated**: Review Docker base image updates
8. **Validate input**: Always validate user input in code
9. **Follow OWASP guidelines**: Apply security best practices

### Security Testing Locally

```bash
# Check .NET for vulnerable packages
cd src
dotnet list ../Clercq.It.CI.slnf package --vulnerable --include-transitive

# Run npm audit
cd src/ClercqIt.Web
pnpm audit

# Scan Docker image with Trivy (if installed)
docker build -t clercq-it:local ./src
trivy image clercq-it:local --severity HIGH,CRITICAL

# SAST and secrets: see docs/application-security.md#run-locally
```

## Troubleshooting

### Common Issues

#### Build Pipeline

**Issue**: Docker Hub push fails with "Username and password required"
- **Cause**: Missing `DOCKER_USERNAME` or `DOCKER_PASSWORD` secrets
- **Solution**: Configure Docker Hub credentials in repository secrets, or the pipeline will build locally without pushing

**Issue**: Tests fail during build
- **Cause**: Code compilation or test errors
- **Solution**: Run tests locally first with `dotnet test Clercq.It.CI.slnf` and `pnpm test`

**Issue**: Next.js build fails with "useSearchParams() should be wrapped in a suspense boundary"
- **Cause**: Next.js 15 requires components using `useSearchParams()` to be wrapped in a Suspense boundary for static page generation
- **Error message**: `⨯ useSearchParams() should be wrapped in a suspense boundary at page "/admin"`
- **Solution**: Extract the component logic that uses `useSearchParams()` into a separate component and wrap it with `<Suspense>` in the page's default export. For example:
  ```tsx
  function MyPageContent() {
    const searchParams = useSearchParams();
    // ... component logic
  }
  
  export default function MyPage() {
    return (
      <Suspense fallback={<div>Loading...</div>}>
        <MyPageContent />
      </Suspense>
    );
  }
  ```

#### Infrastructure Pipeline

**Issue**: Terraform fails with "Invalid index" for load_balancer
- **Cause**: Using incorrect Scaleway RDB instance attributes
- **Solution**: Use `load_balancer.0.ip` and `load_balancer.0.port` instead of deprecated `endpoint_ip` and `endpoint_port`

**Issue**: Scaleway authentication fails
- **Cause**: Missing or incorrect Scaleway credentials
- **Solution**: Verify `SCALEWAY_ACCESS_KEY`, `SCALEWAY_SECRET_KEY`, and `SCALEWAY_ORGANIZATION_ID` secrets

**Issue**: Terraform output fails with "No valid credential sources found"
- **Cause**: The `terraform output` command needs AWS credentials to access the S3 backend, but environment variables are not set
- **Solution**: Ensure AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY environment variables are set for any Terraform command that accesses the state (init, plan, apply, output, etc.)

#### Deploy Pipeline

**Issue**: Container not found during deployment
- **Cause**: The deployment pipeline retrieves container details (name and ID) from Terraform state. If infrastructure hasn't been deployed or Terraform state is unavailable, the container won't be found.
- **Solution**: 
  1. Ensure the infrastructure pipeline has run successfully at least once
  2. Verify Terraform state is accessible (check AWS credentials for S3 backend)
  3. The container is managed by Terraform (`clercq-it-app`) and deployment uses the container ID from Terraform outputs for reliable updates
- **Note**: This has been fixed to use container ID instead of name-based lookups for more reliable container discovery
- **Note**: Terraform outputs the regional ID (`fr-par/<uuid>`). The scw CLI only accepts the bare uuid, so deploy.yml strips the region prefix. The step prints the scw error when the lookup fails.

**Issue**: Deploy workflow not triggering
- **Cause**: Workflow name mismatch in trigger configuration
- **Solution**: Deploy workflow now only triggers after "build" workflow (removed "Deploy Infra" trigger to prevent conflicts)

**Issue**: Container image reverting to `:latest` tag
- **Cause**: Infrastructure pipeline was running on every push to main, resetting the container image to `:latest` via Terraform, conflicting with the deploy pipeline's version-specific updates
- **Solution**: Infrastructure pipeline now only runs when files in `infra/**` are modified, preventing it from reverting application deployments

**Issue**: Deploy fails due to missing Docker image
- **Cause**: Build pipeline didn't push to Docker Hub
- **Solution**: Ensure Docker Hub credentials are configured

**Issue**: Version not updating after deployment
- **Cause**: Scaleway Serverless Containers with `min_scale=0` cache Docker images. When containers scale down to zero and back up, they may use cached images instead of pulling fresh ones from the registry, even after `scw container container redeploy`.
- **Solution**: The deployment pipeline now implements a cache-busting strategy:
  1. Updates the container image tag
  2. Forces container recreation by temporarily scaling to `min_scale=1`, then back to `0`
  3. This forces Scaleway to pull a fresh image from the registry
  4. Includes post-deployment version verification to detect any caching issues
- **Note**: The force-recreation step adds ~20-30 seconds to deployment time but ensures the correct version is always deployed

**Issue**: Deploy fails with "gpg: cannot open '/dev/tty': No such device or address"
- **Cause**: Manual Terraform installation attempting to use GPG in non-interactive environment
- **Solution**: This has been fixed to use the official `hashicorp/setup-terraform` action instead of manual installation

**Issue**: Old version still running despite successful deployment (image caching)
- **Cause**: This is a known issue with Scaleway Serverless Containers that have `min_scale=0`. When containers scale to zero, Scaleway caches the Docker image. When they scale back up, they may reuse the cached image instead of pulling the latest version from the registry, even if the image tag has been updated.
- **Root Cause**: Scaleway's image caching optimization for containers with `min_scale=0` doesn't always invalidate the cache when the same tag points to a new image digest
- **Solution**: The deploy workflow now implements a force-recreation strategy that is automatically applied on every deployment:
  - Updates the container with the new image tag
  - Forces recreation by scaling to `min_scale=1` (creates new instance with fresh image)
  - Scales back to `min_scale=0` (preserves original scaling configuration)
  - Verifies the deployed version matches the expected version
- **Prevention**: 
  - Always use version-specific tags (e.g., `1.0.1`) instead of `latest` for production deployments
  - Monitor the deployment logs for version verification warnings
  - If version mismatch is detected, the container typically updates within 2-3 minutes as Scaleway's cache expires
- **Manual Fix**: If the issue persists, manually trigger a deployment with the specific version: `gh workflow run deploy.yml -f version=X.Y.Z`

**Issue**: Bad Gateway (502) errors during container startup
- **Cause**: nginx was auto-starting before the .NET API and Next.js services were ready, causing proxy errors
- **Solution**: The Dockerfile now:
  1. Overrides nginx's default ENTRYPOINT to prevent auto-start
  2. Waits for both backend services to be healthy before starting nginx
  3. Checks process health during startup to detect early failures
  4. Logs service output to /var/log for debugging
  5. Has increased resource limits (512MB RAM, 500m CPU) to support all services

**Issue**: .NET API fails to start with "Failed to resolve libhostfxr.so" error
- **Cause**: The .NET runtime couldn't locate its shared libraries because the dotnet binary was resolving paths incorrectly
- **Error message**: `Error: [/usr/bin/host/fxr] does not exist` and `Failed to resolve libhostfxr.so [not found]. Error code: 0x80008083`
- **Solution**: The Dockerfile now:
  1. Uses the full path to dotnet binary (`/usr/share/dotnet/dotnet`) instead of the symlink
  2. Sets `DOTNET_ROOT=/usr/share/dotnet` environment variable
  3. Sets `DOTNET_RUNNING_IN_CONTAINER=true` for proper container runtime detection

**Issue**: Container fails to start or services crash during startup
- **Cause**: Insufficient memory or CPU resources, or service configuration issues
- **Solution**: 
  1. Check container logs in Scaleway Console for detailed error messages
  2. The startup script now includes detailed logging and will show the last 50 lines of service logs on failure
  3. Verify resource limits are adequate (currently 512MB RAM, 500m CPU)
  4. Ensure ConnectionStrings__DefaultConnection environment variable is set correctly

### Debugging Commands

```bash
# View workflow runs
gh run list

# View specific run details
gh run view <run-id>

# Check workflow logs
gh run view <run-id> --log

# Re-run failed workflow
gh run rerun <run-id>

# Check repository secrets (names only)
gh secret list
```

## GitHub Environments

### infrastructure-plan Environment
- Used for Terraform planning in pull requests
- No protection rules
- Provides Scaleway credentials for planning

### production Environment
- Used for Terraform apply and destroy operations
- Requires manual approval from designated reviewers
- Protected with deployment protection rules

## Resources

### Security & Compliance
- [Security Compliance Guide](./security-compliance.md) - NIST, PCI DSS, ISO 27001 alignment
- [Security Policy](../SECURITY.md) - Vulnerability reporting and security measures
- [OpenSSF Scorecard](https://securityscorecards.dev/viewer/?uri=github.com/Echarnus/Clercq.It) - Live security score

### Development & Deployment
- [Trunk-based development](https://trunkbaseddevelopment.com/)
- [GitVersion Documentation](https://gitversion.net/docs/learn/branching-strategies/githubflow/examples)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Scaleway Container Documentation](https://www.scaleway.com/en/docs/serverless/containers/)
