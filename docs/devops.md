# DevOps & CI/CD Pipeline

This document describes the Continuous Integration and Continuous Deployment (CI/CD) workflows used to test, build, and deploy the Clercq.It application.

## Overview

The CI/CD pipeline is built using **GitHub Actions** and follows **GitHub Flow** with **continuous deployment** enabled. The pipeline uses GitVersion for automatic semantic versioning.

## Pipeline Workflows

The project uses four main workflows:

1. **Test Pipeline** (`test.yml`) - Runs on every push and PR
2. **Build Pipeline** (`build.yml`) - Builds and publishes Docker images
3. **Infrastructure Pipeline** (`infra.yml`) - Manages Terraform infrastructure
4. **Deploy Pipeline** (`deploy.yml`) - Deploys to production

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

## Infrastructure Pipeline (`infra.yml`)

### Triggers
- Push to `main` branch with infrastructure changes
- Pull requests with infrastructure changes
- Manual workflow dispatch

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

- **Serverless Container**: Auto-scales 0-1 vCPU with 128MB memory
- **Serverless SQL**: PostgreSQL database with minimal resource allocation
- **Organization**: ClercqIt with Portfolio namespace
- **Cost Optimization**: Infrastructure scales to zero when not in use

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
- Updates Scaleway container configuration
- Performs health checks
- Reports deployment status

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
- `SCALEWAY_ACCESS_KEY`: Scaleway API access key
- `SCALEWAY_SECRET_KEY`: Scaleway API secret key
- `SCALEWAY_ORGANIZATION_ID`: Scaleway organization ID
- `DATABASE_PASSWORD`: Secure PostgreSQL database password

### Environment Variables

- `REGISTRY`: Docker registry (docker.io)
- `IMAGE_NAME`: Docker image name (echarnus/clercq-it)
- `CONTAINER_IMAGE`: Container image tag for deployment (optional)
- `CUSTOM_DOMAIN`: Custom domain for Scaleway deployment (optional)

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

**Issue**: Tests fail during build
- **Cause**: Code compilation or test errors
- **Solution**: Run tests locally first with `dotnet test` and `pnpm test`

#### Infrastructure Pipeline

**Issue**: Terraform fails with "Invalid index" for load_balancer
- **Cause**: Using incorrect Scaleway RDB instance attributes
- **Solution**: This has been fixed to use `endpoint_ip` and `endpoint_port`

**Issue**: Scaleway authentication fails
- **Cause**: Missing or incorrect Scaleway credentials
- **Solution**: Verify `SCALEWAY_ACCESS_KEY`, `SCALEWAY_SECRET_KEY`, and `SCALEWAY_ORGANIZATION_ID` secrets

#### Deploy Pipeline

**Issue**: Deploy workflow not triggering
- **Cause**: Workflow name mismatch in trigger configuration
- **Solution**: This has been fixed to use correct workflow names: `build` and `Deploy Infra`

**Issue**: Deploy fails due to missing Docker image
- **Cause**: Build pipeline didn't push to Docker Hub
- **Solution**: Ensure Docker Hub credentials are configured

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

For detailed environment configuration, see the [GitHub Environments Guide](./GITHUB_ENVIRONMENTS.md).

## Resources

- [GitHub Flow Documentation](https://docs.github.com/en/get-started/quickstart/github-flow)
- [GitVersion Documentation](https://gitversion.net/docs/learn/branching-strategies/githubflow/examples)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Scaleway Container Documentation](https://www.scaleway.com/en/docs/serverless/containers/)
- [Infrastructure Setup Guide](../infrastructure/README.md)
