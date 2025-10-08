# DevOps & CI/CD Pipeline

This document describes the Continuous Integration and Continuous Deployment (CI/CD) workflows used to test, build, and deploy the Clercq.It application.

## Overview

The CI/CD pipeline is built using **GitHub Actions** and follows **GitHub Flow** with **continuous deployment** enabled. The pipeline uses GitVersion for automatic semantic versioning.

## Pipeline Workflows

The project uses six main workflows:

1. **Test Pipeline** (`test.yml`) - Runs on every push and PR with security checks
2. **Build Pipeline** (`build.yml`) - Builds and publishes Docker images with vulnerability scanning
3. **Security Scanning** (`security-scan.yml`) - Comprehensive security analysis
4. **CodeQL Analysis** (`codeql.yml`) - Advanced semantic code security analysis
5. **Infrastructure Pipeline** (`infra.yml`) - Manages Terraform infrastructure
6. **Deploy Pipeline** (`deploy.yml`) - Deploys to production

Additionally, **Dependabot** runs automatically to keep dependencies secure and up to date.

## Test Pipeline (`test.yml`)

### Triggers
- Push to `main`, `develop`, or `feature/*` branches
- Pull requests to `main` or `develop`

### Jobs

#### test-dotnet
- Uses .NET 9.0
- Runs xUnit tests with code coverage
- Uploads coverage to Codecov
- Tests located in `/tests/` directory

#### test-frontend
- Uses Node.js 23 and pnpm
- Runs Jest tests and ESLint
- Builds Next.js application to verify compilation

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
- Multi-platform build (AMD64/ARM64)
- Publishes to Docker Hub with multiple tags:
  - Semantic version (e.g., `1.0.1`)
  - Short SHA (e.g., `abc1234`)
  - `latest` (for main branch)
- Generates build attestation for security

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
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Daily schedule at 2:00 AM UTC
- Manual workflow dispatch

### Jobs

#### trivy-container-scan
- Builds Docker image for scanning
- Runs Trivy vulnerability scanner
- Uploads results to GitHub Security tab
- Scans for CRITICAL, HIGH, and MEDIUM severity vulnerabilities
- Fails on CRITICAL and HIGH vulnerabilities

#### trivy-filesystem-scan
- Scans entire repository filesystem
- Detects IaC misconfigurations
- Identifies dependency vulnerabilities
- Uploads SARIF results to GitHub Security

#### dotnet-security-scan
- Installs security-scan tool for .NET
- Analyzes .NET projects for security issues
- Checks for known vulnerable packages
- Generates vulnerability reports
- Uploads reports as artifacts

#### npm-security-scan
- Runs pnpm audit on Node.js dependencies
- Checks for known vulnerabilities
- Generates JSON audit reports
- Uploads reports as artifacts
- Fails on moderate or higher severity issues

#### secrets-scan
- Uses TruffleHog to scan for leaked secrets
- Checks git history for credentials
- Only reports verified secrets
- Prevents credential exposure

#### semgrep-scan
- Runs Semgrep static analysis security scanner
- Uses multiple security rulesets:
  - Security audit patterns
  - Secret detection rules
  - OWASP Top 10 checks
  - CWE Top 25 vulnerability patterns
- Uploads SARIF results to GitHub Security
- Supports C#, JavaScript, TypeScript, and more

#### security-scorecard
- Runs OpenSSF Scorecard evaluation
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
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
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
- Validates Docker image exists
- Updates Scaleway container configuration with new image tag
- Forces container recreation to bypass image cache (scales min_scale: 0→1→0)
- Performs health checks
- Verifies deployed version matches expected version
- Reports deployment status

**Note**: The deployment implements a cache-busting strategy to work around Scaleway's image caching behavior with `min_scale=0` containers. This ensures fresh images are always pulled from the registry by forcing container recreation through a temporary scale-up/scale-down cycle.

### Manual Deployment

```bash
# Deploy specific version
gh workflow run deploy.yml -f version=1.0.1

# Deploy latest version
gh workflow run deploy.yml -f version=latest
```

### Health Checks

The pipeline includes comprehensive health checks:
- Endpoint availability testing
- Retry logic with exponential backoff
- Detailed error reporting

## Required Secrets

### Docker Hub
- `DOCKER_USERNAME`: Docker Hub username
- `DOCKER_PASSWORD`: Docker Hub password or token

### Scaleway
- `SCALEWAY_ACCESS_KEY`: Scaleway API access key (also used as AWS_ACCESS_KEY_ID for S3 backend)
- `SCALEWAY_SECRET_KEY`: Scaleway API secret key (also used as AWS_SECRET_ACCESS_KEY for S3 backend)
- `SCALEWAY_ORGANIZATION_ID`: Scaleway organization ID
- `SCALEWAY_PROJECT_ID`: Scaleway project ID
- `DATABASE_PASSWORD`: Secure PostgreSQL database password

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
7. New version is deployed to production

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
- **Semgrep**: Static analysis security scanning
  - Multi-language support (C#, JavaScript, TypeScript, and more)
  - Security audit rulesets
  - OWASP Top 10 detection
  - CWE Top 25 vulnerability patterns
  - Secret detection patterns
  - Results uploaded to GitHub Security tab
- **Security linting**: ESLint security rules for frontend code

#### Container Security
- **Trivy vulnerability scanner**: Comprehensive container security
  - Scans Docker images for OS vulnerabilities
  - Scans application dependencies
  - Filesystem scanning for IaC misconfigurations
  - Integrated into build pipeline with severity filtering
  - Results uploaded to GitHub Security tab
- **.NET security scanning**: Automated checks with security-scan tool
  - Analyzes .NET projects for security issues
  - Excludes development dependencies
  - Generates vulnerability reports

#### Secret Detection
- **TruffleHog**: Scans for accidentally committed secrets
  - Runs on every push
  - Checks verified secrets only
  - Prevents credential leaks
- **GitHub Secret Scanning**: Native GitHub protection

#### Security Posture
- **OpenSSF Scorecard**: Evaluates repository security practices
  - Automated scoring of security best practices
  - Published results for transparency
  - Regular automated assessments

### Build Security
- **Build attestation**: Signed build provenance for container images
- **Multi-platform builds**: Support for AMD64 and ARM64 architectures
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
  - Secret scanning with TruffleHog
  - Semgrep static analysis
  - OpenSSF Scorecard

#### codeql.yml
Advanced code analysis:
- **Triggers**: Push, PR, weekly schedule, manual
- **Languages**: C#, JavaScript/TypeScript
- **Analysis**: Security-extended queries

#### Dependabot
Automated dependency updates:
- **Schedule**: Weekly on Mondays at 8:00 AM UTC
- **Ecosystems**: NuGet, npm, GitHub Actions, Docker, Terraform
- **Auto-assignment**: PRs assigned to repository owner
- **Labeling**: Categorized by dependency type

## Best Practices

### Development
1. Always create feature branches from `main` or `develop`
2. Write tests for new functionality
3. Keep commits focused and descriptive
4. Use conventional commit messages for better versioning

### Deployment
1. Test locally before pushing
2. Monitor deployment status and logs
3. Use manual deployment for hotfixes
4. Keep production deployments during low-traffic hours

### Security
1. **Review Dependabot PRs promptly**: Keep dependencies up to date
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
dotnet list ClercqIt.Api/ClercqIt.Api.csproj package --vulnerable --include-transitive

# Run npm audit
cd src/ClercqIt.Web
pnpm audit

# Scan Docker image with Trivy (if installed)
docker build -t clercq-it:local ./src
trivy image clercq-it:local --severity HIGH,CRITICAL

# Scan for secrets with TruffleHog (if installed)
trufflehog filesystem . --only-verified
```

## Troubleshooting

### Common Issues

#### Build Pipeline

**Issue**: Docker Hub push fails with "Username and password required"
- **Cause**: Missing `DOCKER_USERNAME` or `DOCKER_PASSWORD` secrets
- **Solution**: Configure Docker Hub credentials in repository secrets, or the pipeline will build locally without pushing

**Issue**: Tests fail during build
- **Cause**: Code compilation or test errors
- **Solution**: Run tests locally first with `dotnet test` and `pnpm test`

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

- [GitHub Flow Documentation](https://docs.github.com/en/get-started/quickstart/github-flow)
- [GitVersion Documentation](https://gitversion.net/docs/learn/branching-strategies/githubflow/examples)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Scaleway Container Documentation](https://www.scaleway.com/en/docs/serverless/containers/)
- [Infrastructure Setup Guide](../infra/README.md)
