# Docker Build Issues and Solutions

## SSL Certificate Issues in Local Development

### Issue Description
When building the Docker container locally, you may encounter SSL certificate validation errors during NuGet package restoration:

```
error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json
error NU1301: The SSL connection could not be established, see inner exception.
error NU1301: The remote certificate is invalid because of errors in the certificate chain: UntrustedRoot
```

### Root Cause
This issue occurs in some local Docker environments where the .NET HTTP client cannot validate certificates from NuGet.org, despite curl and other tools working fine. This is typically related to:
- Corporate firewalls or proxy settings
- Local Docker configuration
- Certificate chain validation in restricted environments

### Solutions

#### 1. CI/CD Environment (GitHub Actions)
The Dockerfile is optimized to work in GitHub Actions CI/CD environment, where certificate handling is typically correct. No changes should be needed.

#### 2. Local Development Workarounds

**Option A: Use Aspire for local development**
```bash
# Run with Aspire (recommended for local development)
dotnet run --project src/Clercq.It.AppHost
```

**Option B: Manual setup with local PostgreSQL**
```bash
# Build .NET API separately
cd src/ClercqIt.Api
dotnet build
dotnet run

# Build Next.js frontend separately  
cd src/ClercqIt.Web
npm install
npm run build
npm start
```

**Option C: Docker build with network configuration**
```bash
# Try building with host network
docker build --network=host -t clercq-it ./src

# Or with DNS configuration
docker build --dns=8.8.8.8 -t clercq-it ./src
```

### Build Verification

To verify the build works in your environment:

1. **Test in CI/CD**: The GitHub Actions build pipeline should work correctly
2. **Local development**: Use Aspire or manual setup as described above
3. **Production deployment**: The Dockerfile is designed for production container builds

### Notes

- This SSL issue is environment-specific and should not affect production deployments
- GitHub Actions has proper certificate handling and should build successfully
- The Dockerfile includes retry logic to handle temporary network issues