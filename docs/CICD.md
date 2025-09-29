# CI/CD Pipeline Documentation

This document describes the Continuous Integration and Continuous Deployment (CI/CD) pipeline for the Clercq.It project.

## Overview

The CI/CD pipeline is built using GitHub Actions and follows **GitHub Flow** with **continuous deployment** enabled. The pipeline uses GitVersion for automatic semantic versioning and consists of three main workflows:

1. **Test Pipeline** (`test.yml`) - Runs on every push and PR
2. **Build Pipeline** (`build.yml`) - Builds and publishes Docker images
3. **Deploy Pipeline** (`deploy.yml`) - Deploys to production

## Branching Strategy (GitHub Flow)

### Branch Types

- **`main`** - Production branch, triggers deployment
- **`develop`** - Development branch for feature integration  
- **`feature/*`** - Feature branches merged via Pull Requests
- **`hotfix/*`** - Hotfix branches for urgent production fixes

### Workflow

1. Create feature branches from `main` or `develop`
2. Develop and test features locally
3. Create Pull Request to `main` or `develop`
4. Automated tests run on PR
5. After approval and merge, automated deployment occurs (if merging to `main`)

## GitVersion Configuration

The project uses GitVersion with GitHub Flow mode for automatic semantic versioning:

```yaml
mode: ContinuousDeployment
```

### Versioning Strategy

- **main branch**: Patch increments (1.0.0 → 1.0.1)
- **develop branch**: Minor increments with alpha tag (1.0.0-alpha.1)
- **feature branches**: Inherit increment with feature tag (1.0.1-feature.branch-name.1)
- **hotfix branches**: Patch increment with beta tag (1.0.1-beta.1)

## Pipeline Details

### 🧪 Test Pipeline (`test.yml`)

**Triggers:**
- Push to `main`, `develop`, or `feature/*` branches
- Pull requests to `main` or `develop`

**Jobs:**
- **test-dotnet**: Runs .NET API tests from `/tests/` directory
  - Uses .NET 9.0
  - Runs xUnit tests with code coverage
  - Uploads coverage to Codecov
- **test-frontend**: Runs Next.js frontend tests
  - Uses Node.js 23 and pnpm
  - Runs Jest tests and ESLint
  - Builds Next.js application

### 🏗️ Build Pipeline (`build.yml`)

**Triggers:**
- Push to `main` branch only
- Manual workflow dispatch with optional branch selection
- Pull requests (test only, no build/push)

**Jobs:**
- **test**: Calls test pipeline
- **build**: Builds and pushes Docker images
  - Uses GitVersion for semantic versioning
  - Multi-platform build (AMD64/ARM64)
  - Publishes to Docker Hub with multiple tags:
    - Semantic version (e.g., `1.0.1`)
    - Short SHA (e.g., `abc1234`)
    - `latest` (for main branch)
  - Generates build attestation for security

**Docker Image Tags:**
- `echarnus/clercq-it:1.0.1` (semantic version)
- `echarnus/clercq-it:latest` (main branch only)
- `echarnus/clercq-it:abc1234` (short SHA)

### 🚀 Deploy Pipeline (`deploy.yml`)

**Triggers:**
- Automatic: When build pipeline completes successfully on `main` branch
- Manual: Workflow dispatch with version parameter

**Jobs:**
- **deploy**: Deploys to Scaleway production environment
  - Validates Docker image exists
  - Updates container configuration
  - Performs health checks
  - Reports deployment status

**Manual Deployment:**
```bash
# Deploy specific version
gh workflow run deploy.yml -f version=1.0.1

# Deploy latest version
gh workflow run deploy.yml -f version=latest
```

## Environment Variables and Secrets

### Required Secrets

- `DOCKER_USERNAME`: Docker Hub username
- `DOCKER_PASSWORD`: Docker Hub password or token
- `SCALEWAY_ACCESS_KEY`: Scaleway API access key
- `SCALEWAY_SECRET_KEY`: Scaleway API secret key
- `SCALEWAY_ORGANIZATION_ID`: Scaleway organization ID
- `DATABASE_PASSWORD`: Secure PostgreSQL database password

### Environment Variables

- `REGISTRY`: Docker registry (docker.io)
- `IMAGE_NAME`: Docker image name (echarnus/clercq-it)
- `CONTAINER_IMAGE`: Container image tag for deployment (optional)
- `CUSTOM_DOMAIN`: Custom domain for Scaleway deployment (optional)

### Infrastructure-Specific Configuration

For detailed infrastructure setup and PR configuration, see:
- [Infrastructure PR Configuration Guide](./INFRASTRUCTURE_PR_GUIDE.md)
- [GitHub Environments Setup](./GITHUB_ENVIRONMENTS.md)
- [Infrastructure Secrets Documentation](../infrastructure/SECRETS.md)

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

## Troubleshooting

### Common Issues

1. **Build failures**: Check test results and build logs
2. **Docker push failures**: Verify Docker Hub credentials
3. **Deployment failures**: Check Scaleway connectivity and permissions
4. **Version conflicts**: Ensure GitVersion configuration is correct

### Debugging Commands

```bash
# View workflow runs
gh run list --workflow=test.yml

# View specific run details
gh run view <run-id>

# Re-run failed workflow
gh run rerun <run-id>

# Check repository secrets
gh secret list
```

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

### Common Pipeline Issues

#### Build Pipeline (`build.yml`)

**Issue**: Docker Hub push fails with "Username and password required"
- **Cause**: Missing `DOCKER_USERNAME` or `DOCKER_PASSWORD` secrets
- **Solution**: Configure Docker Hub credentials in repository secrets, or the pipeline will build locally without pushing

**Issue**: Tests fail during build
- **Cause**: Code compilation or test errors
- **Solution**: Run tests locally first with `dotnet test` and `pnpm test`

#### Infrastructure Pipeline (`infra.yml`)

**Issue**: Terraform fails with "Invalid index" for load_balancer
- **Cause**: Using incorrect Scaleway RDB instance attributes
- **Solution**: This has been fixed to use `endpoint_ip` and `endpoint_port`

**Issue**: Scaleway authentication fails
- **Cause**: Missing or incorrect Scaleway credentials
- **Solution**: Verify `SCALEWAY_ACCESS_KEY`, `SCALEWAY_SECRET_KEY`, and `SCALEWAY_ORGANIZATION_ID` secrets

#### Deploy Pipeline (`deploy.yml`)

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

### Pipeline Status

Current pipeline status and known issues:

- ✅ **Test Pipeline**: Working correctly
- ✅ **Build Pipeline**: Fixed to handle missing Docker credentials gracefully
- ✅ **Infrastructure Pipeline**: Fixed Terraform configuration issues
- ✅ **Deploy Pipeline**: Fixed workflow name references

## Resources

- [GitHub Flow Documentation](https://docs.github.com/en/get-started/quickstart/github-flow)
- [GitVersion Documentation](https://gitversion.net/docs/learn/branching-strategies/githubflow/examples)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Scaleway Container Documentation](https://www.scaleway.com/en/docs/serverless/containers/)
- [Secrets Setup Guide](./SECRETS_SETUP.md) - How to configure required secrets