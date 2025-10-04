# DevOps & CI/CD Pipeline

This document describes the Continuous Integration and Continuous Deployment (CI/CD) workflows used to test, build, and deploy the Clercq.It application.

## Overview

The CI/CD pipeline is built using **GitHub Actions** and follows **GitHub Flow** with **continuous deployment** enabled. The pipeline uses GitVersion for automatic semantic versioning and composite actions for reusability.

## Pipeline Workflows

The project uses two main workflows:

1. **Deploy Pipeline** (`deploy.yml`) - Unified pipeline for testing, building, infrastructure, and deployment
2. **Test Pipeline** (`test.yml`) - Runs tests on pull requests

## Composite Actions

Reusable logic is organized into composite actions in `.github/actions/`:

- **test-dotnet** - .NET API testing with xUnit and code coverage
- **test-frontend** - Next.js frontend testing with Jest and ESLint
- **build-docker** - Docker image building, migration script generation, and Docker Hub push
- **deploy-infra** - Terraform infrastructure deployment
- **migrate-database** - Database migration execution
- **deploy-container** - Scaleway container deployment and health checks

## Test Pipeline (`test.yml`)

### Triggers
- Pull requests to `main` or `develop`
- Manual workflow dispatch

### Jobs

#### test-dotnet
- Uses .NET 9.0
- Runs xUnit tests with code coverage
- Uploads coverage to Codecov
- Tests located in `/tests/` directory
- Implemented via `.github/actions/test-dotnet` composite action

#### test-frontend
- Uses Node.js 23 and pnpm
- Runs Jest tests and ESLint
- Builds Next.js application to verify compilation
- Implemented via `.github/actions/test-frontend` composite action

### Example

```bash
# Runs automatically on PR
# Or manually trigger with
gh workflow run test.yml
```

## Deploy Pipeline (`deploy.yml`)

### Triggers
- Push to `main` branch
- Manual workflow dispatch with optional parameters:
  - `version`: Specific version to deploy (skips build)
  - `skip-tests`: Skip test execution (not recommended)
  - `skip-infra`: Skip infrastructure deployment

### Jobs

The Deploy workflow consists of five sequential jobs that form a complete deployment pipeline:

#### 1. Test
- Runs .NET API tests using `test-dotnet` composite action
- Runs Next.js frontend tests using `test-frontend` composite action
- Can be skipped with `skip-tests` parameter (not recommended)

#### 2. Build
- Determines semantic version using GitVersion
- Generates idempotent migration SQL script
- Builds multi-platform Docker image (AMD64/ARM64)
- Pushes to Docker Hub with multiple tags:
  - Semantic version (e.g., `1.0.1`)
  - Short SHA (e.g., `abc1234`)
  - `latest`
- Generates build attestation for security
- Uses `build-docker` composite action
- Skipped if manual version is specified

#### 3. Deploy Infrastructure
- Applies Terraform infrastructure changes
- Creates/updates Scaleway resources (database, container, etc.)
- Uses `deploy-infra` composite action
- Can be skipped with `skip-infra` parameter
- Requires `production` environment

#### 4. Migrate Database
- Downloads migration script artifact from build job
- Connects to Scaleway RDB PostgreSQL
- Executes idempotent migration script
- Uses `migrate-database` composite action
- Requires `production` environment

#### 5. Deploy Container
- Validates Docker image exists
- Updates Scaleway container with new image
- Deploys container to production
- Runs post-deployment health checks
- Uses `deploy-container` composite action
- Requires `production` environment

### Docker Image Tags

```
echarnus/clercq-it:1.0.1      # Semantic version
echarnus/clercq-it:latest     # Main branch only
echarnus/clercq-it:abc1234    # Short SHA
```

### Example

```bash
# Runs automatically on push to main
gh workflow run deploy.yml

# Deploy specific version manually
gh workflow run deploy.yml -f version=1.0.1

# Deploy with custom options
gh workflow run deploy.yml -f skip-tests=true -f skip-infra=true
```

### Benefits of Unified Pipeline

- **Single workflow run**: All jobs share the same workflow context
- **Artifact sharing**: No cross-workflow artifact issues
- **Clear dependencies**: Jobs run in explicit order with `needs` declarations
- **Official actions only**: Uses only GitHub official actions, no third-party dependencies
- **Reusable logic**: Composite actions can be used in other workflows
- **Easier debugging**: Full pipeline visible in single workflow run
- Uses `production` environment with manual approval
- Requires explicit confirmation

### Infrastructure Components

- **Serverless Container**: Auto-scales 0-1 vCPU with 128MB memory
- **Serverless SQL**: PostgreSQL database with minimal resource allocation
- **Organization**: ClercqIt with Portfolio namespace
- **Cockpit Logging**: Scaleway Cockpit for centralized logs and metrics via OpenTelemetry
- **Custom Domain**: www.clercq.it configured for the application
- **Cost Optimization**: Infrastructure scales to zero when not in use

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
- `CUSTOM_DOMAIN`: Custom domain for Scaleway deployment (optional, defaults to www.clercq.it)

## Deployment Process

### Automatic Deployment (Main Branch)

1. Developer pushes to `main` branch
2. Deploy workflow triggers automatically
3. Test job runs all .NET and frontend tests
4. Build job creates Docker image with semantic version
5. Deploy Infrastructure job applies Terraform changes
6. Migrate Database job executes database migrations
7. Deploy Container job updates and deploys the container
8. Health checks verify deployment success

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
cd infrastructure/terraform
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
[![Deploy](https://github.com/Echarnus/Clercq.It/actions/workflows/deploy.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/deploy.yml)
```

### Deployment Status

Each deployment provides detailed status information:
- Version deployed
- Docker image used
- Deployment source (automatic vs manual)
- Environment information

## Security Features

### Build Security
- **Build attestation**: Signed build provenance for container images
- **Multi-platform builds**: Support for AMD64 and ARM64 architectures
- **Vulnerability scanning**: Automated security checks
- **Secret management**: Sensitive data stored in GitHub Secrets

### Container Security
- **Non-root execution**: Containers run with non-privileged users
- **Minimal attack surface**: Alpine-based images with minimal packages
- **Health checks**: Built-in container health monitoring

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
1. Regularly update dependencies
2. Monitor security alerts
3. Use least-privilege access for secrets
4. Review build logs for suspicious activity

## Troubleshooting

### Common Issues

#### Build Pipeline

**Issue**: Docker Hub push fails with "Username and password required"
- **Cause**: Missing `DOCKER_USERNAME` or `DOCKER_PASSWORD` secrets
- **Solution**: Configure Docker Hub credentials in repository secrets, or the pipeline will build locally without pushing

**Issue**: Tests fail during deployment
- **Cause**: Code compilation or test errors
- **Solution**: Run tests locally first with `dotnet test` and `pnpm test`

#### Deploy Pipeline

**Issue**: Deployment workflow job fails
- **Cause**: Missing secrets or configuration
- **Solution**: Verify all required secrets are configured (Docker Hub, Scaleway, Database Password)

**Issue**: Infrastructure deployment skipped but database migration fails
- **Cause**: Using `skip-infra=true` but infrastructure doesn't exist
- **Solution**: Run full deployment first, then you can skip infrastructure on subsequent deployments

**Issue**: Migration script not found
- **Cause**: Build job didn't complete successfully
- **Solution**: Check build job logs for errors in migration generation

**Issue**: Container deployment fails
- **Cause**: Container doesn't exist in Scaleway
- **Solution**: Ensure infrastructure deployment completed successfully (creates the container)

#### Scaleway Configuration

**Issue**: Scaleway authentication fails
- **Cause**: Missing or incorrect Scaleway credentials
- **Solution**: Verify `SCALEWAY_ACCESS_KEY`, `SCALEWAY_SECRET_KEY`, and `SCALEWAY_ORGANIZATION_ID` secrets

**Issue**: Terraform output fails with "No valid credential sources found"
- **Cause**: The `terraform output` command needs AWS credentials to access the S3 backend
- **Solution**: Ensure AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY environment variables are set (the composite actions handle this automatically)

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

### production Environment
- Used for infrastructure deployment, database migrations, and container deployment
- Requires manual approval from designated reviewers (optional)
- Protected with deployment protection rules
- All deployment jobs use this environment for production access

## Resources

- [GitHub Flow Documentation](https://docs.github.com/en/get-started/quickstart/github-flow)
- [GitVersion Documentation](https://gitversion.net/docs/learn/branching-strategies/githubflow/examples)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Scaleway Container Documentation](https://www.scaleway.com/en/docs/serverless/containers/)
- [Infrastructure Setup Guide](../infrastructure/README.md)
