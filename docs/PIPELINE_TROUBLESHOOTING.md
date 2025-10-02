# Pipeline Troubleshooting Guide

This guide helps you troubleshoot common issues with the CI/CD pipelines after the recent fixes and improvements.

## 🚀 Quick Status Check

### Recently Fixed Issues ✅

1. **Build Pipeline**: Enhanced with better error reporting and build summaries
2. **Deploy Pipeline**: Completed with actual Scaleway deployment implementation  
3. **Test Pipeline**: Improved with detailed feedback and error handling
4. **Infrastructure Pipeline**: Already functional with comprehensive Terraform automation

### Pipeline Overview

- **Test Pipeline** (`test.yml`): Runs on push/PR, tests .NET and Next.js code
- **Build Pipeline** (`build.yml`): Runs on main branch, builds and pushes Docker images
- **Deploy Pipeline** (`deploy.yml`): Deploys to Scaleway containers after successful build
- **Infrastructure Pipeline** (`infra.yml`): Manages Scaleway infrastructure with Terraform

## 🔧 Common Issues & Solutions

### Build Pipeline Issues

#### Issue: "Aspire workload must be installed"
**Solution**: The workflow now automatically installs the Aspire workload, but if it fails:
```bash
# Locally test with:
dotnet workload install aspire
dotnet restore
dotnet build
```

#### Issue: "Docker push fails"
**Symptoms**: Build succeeds but Docker image isn't pushed
**Solution**: Configure Docker Hub secrets:
- `DOCKER_USERNAME`: Your Docker Hub username
- `DOCKER_PASSWORD`: Your Docker Hub access token

**Note**: The pipeline will build successfully without Docker credentials, but images won't be pushed.

### Deploy Pipeline Issues

#### Issue: "Scaleway credentials not found"
**Solution**: Configure required secrets in GitHub repository settings:
- `SCALEWAY_ACCESS_KEY`: Your Scaleway API access key
- `SCALEWAY_SECRET_KEY`: Your Scaleway API secret key  
- `SCALEWAY_ORGANIZATION_ID`: Your Scaleway organization ID

#### Issue: "Container not found"
**Symptoms**: Deployment fails with container discovery error
**Solution**: 
1. Run the infrastructure pipeline first to create the container
2. Ensure container name matches "clercq-it" or update the deploy script
3. Check Scaleway console to verify container exists

#### Issue: "Health checks fail"
**Symptoms**: Deployment succeeds but health checks timeout
**Solution**:
1. Check if container is starting properly in Scaleway console
2. Verify application starts correctly (check logs)
3. Ensure port 80 is properly exposed in the container

### Test Pipeline Issues

#### Issue: Frontend tests fail with JSX warnings
**Solution**: These are warnings, not errors. Tests still pass. To fix warnings:
```json
// In babel.config.js or jest.config.js
{
  "presets": [
    ["@babel/preset-react", { "runtime": "automatic" }]
  ]
}
```

#### Issue: Database-dependent tests skip
**Symptoms**: Some integration tests are skipped
**Solution**: This is expected behavior. Tests that require database setup are skipped in CI and should be run locally with proper database configuration.

### Infrastructure Pipeline Issues

#### Issue: "Environment not found"
**Solution**: Create required GitHub environments:
1. Go to repository Settings → Environments
2. Create `infrastructure-plan` (no protection rules)
3. Create `production` (with required reviewers and branch protection)

## 🧪 Testing Your Pipeline

### Manual Testing

1. **Test Workflow Syntax**:
   ```bash
   # Run the pipeline test workflow
   gh workflow run pipeline-test.yml -f test_scope=all
   ```

2. **Test Local Build**:
   ```bash
   # Test .NET build
   dotnet workload install aspire
   dotnet restore
   dotnet build --configuration Release
   dotnet test --configuration Release
   
   # Test frontend build
   cd src/ClercqIt.Web
   pnpm install --frozen-lockfile
   pnpm run build
   
   cd ../../tests/ClercqIt.Web.Tests  
   pnpm install --frozen-lockfile
   pnpm test
   ```

3. **Test Docker Build**:
   ```bash
   cd src
   docker build -t test-clercq-it .
   ```

### Monitoring Pipeline Runs

1. **GitHub Actions Tab**: Check workflow run details and logs
2. **Step Summaries**: Each step now provides detailed status information
3. **Build Artifacts**: Docker images are tagged with version and SHA
4. **Deployment Status**: Deploy workflow provides container URLs and health status

## 📊 Pipeline Status Indicators

### Successful Pipeline Run Indicators

- ✅ **Test Pipeline**: All .NET and frontend tests pass
- ✅ **Build Pipeline**: Docker image built and pushed (or local build if no credentials)
- ✅ **Deploy Pipeline**: Container updated and health checks pass
- ✅ **Infrastructure Pipeline**: Terraform apply completes successfully

### What Each Pipeline Outputs

#### Test Pipeline
- Build summaries for both .NET and Next.js
- Test result summaries
- Clear indication of any failures

#### Build Pipeline  
- Docker image tags and versions
- Push status (successful push vs. local build)
- GitVersion information

#### Deploy Pipeline
- Container URL and status
- Health check results
- Links to Scaleway console
- Deployment rollback information if needed

#### Infrastructure Pipeline
- Terraform outputs in JSON format
- Infrastructure component summaries
- Resource creation/update status

## 🆘 Getting Help

### Check These First

1. **Workflow Logs**: Review the detailed logs in GitHub Actions
2. **Step Summaries**: Check the summary section for structured information
3. **Secrets Configuration**: Verify all required secrets are set
4. **Environment Setup**: Ensure GitHub environments are properly configured

### Debug Commands

```bash
# Check workflow status
gh run list

# View specific run
gh run view <run-id>

# Check repository secrets (names only)
gh secret list

# View workflow files
gh workflow list
```

### Still Need Help?

1. Check the logs for specific error messages
2. Verify all prerequisites are met (secrets, environments, etc.)
3. Test components locally before running in CI
4. Review the pipeline documentation in `/docs/devops.md`

## 🔄 Pipeline Dependencies

Understanding the pipeline flow helps with troubleshooting:

```
Code Push → Test Pipeline → Build Pipeline → Deploy Pipeline
     ↓
Infrastructure Changes → Infrastructure Pipeline
```

- **Test Pipeline** must pass before Build Pipeline runs
- **Build Pipeline** must complete before Deploy Pipeline runs  
- **Infrastructure Pipeline** runs independently when infrastructure files change
- **Deploy Pipeline** can be triggered manually with specific versions