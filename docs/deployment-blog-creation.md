# Blog Creation Feature - Deployment Guide

This guide provides instructions for deploying the new blog creation feature with authentication and object storage.

## Overview

The blog creation feature adds the ability to create blog posts through the API with:
- JWT authentication for secure access
- Markdown support for blog content
- Image uploads to Scaleway Object Storage
- Input validation with FluentValidation

## Changes Summary

### Infrastructure

#### Terraform Resources Added

1. **Object Storage Bucket** (`scaleway_object_bucket.blog_images`)
   - Bucket name: `clercq-it-blog-images`
   - Region: `fr-par`
   - Public read access via ACL

2. **New Variables** (in `variables.tf`)
   - `scaleway_access_key` - S3 access key
   - `scaleway_secret_key` - S3 secret key
   - `jwt_secret_key` - JWT signing key (legacy, not used with Quasr.io)

3. **Container Environment Variables** (in `main.tf`)
   - `ObjectStorage__Endpoint`
   - `ObjectStorage__BucketName`
   - `ObjectStorage__Region`
   - `ObjectStorage__AccessKey`
   - `ObjectStorage__SecretKey`
   - `Authentication__JwtSecretKey` (legacy, not used with Quasr.io)

### Application Code

#### New Files

- `src/Clercq.It.Domain/Abstractions/IObjectStorageService.cs`
- `src/Clercq.It.Infrastructure/Configuration/ObjectStorageSettings.cs`
- `src/Clercq.It.Infrastructure/Configuration/AuthenticationSettings.cs`
- `src/Clercq.It.Infrastructure/Services/ObjectStorageService.cs`
- `src/Clercq.It.Application/Features/Blogs/Commands/CreateBlogCommand.cs`
- `src/Clercq.It.Application/Features/Blogs/Commands/CreateBlogCommandHandler.cs`
- `src/Clercq.It.Application/Features/Blogs/Commands/CreateBlogCommandValidator.cs`

#### Modified Files

- `src/ClercqIt.Api/Program.cs` - Added JWT authentication
- `src/ClercqIt.Api/Features/BlogsEndpoints.cs` - Added POST endpoint
- `src/ClercqIt.Api/appsettings.json` - Added configuration sections
- `src/Clercq.It.Infrastructure/DependencyInjection.cs` - Registered S3 client and services

#### New Dependencies

- `Microsoft.AspNetCore.Authentication.JwtBearer` (9.0.9)
- `AWSSDK.S3` (4.0.7.7)
- `FluentValidation` (12.0.0)
- `Microsoft.Extensions.Configuration.Binder` (9.0.9)

## Deployment Steps

### 1. Set Up Scaleway Object Storage

#### Create Access Keys

1. Log in to Scaleway Console
2. Navigate to **IAM** > **API Keys**
3. Create a new API key or use existing one
4. Save the **Access Key** and **Secret Key**

#### Configure Bucket (via Terraform)

The bucket will be created automatically by Terraform. No manual setup required.

### 2. Configure GitHub Secrets

Add the following secrets to your GitHub repository:

```
SCW_ACCESS_KEY       - Scaleway access key (for Terraform and Object Storage)
SCW_SECRET_KEY       - Scaleway secret key (for Terraform and Object Storage)
JWT_SECRET_KEY       - (Legacy) Not used with Quasr.io authentication
DATABASE_PASSWORD    - PostgreSQL database password
```

> **Note:** The `JWT_SECRET_KEY` secret is from a legacy implementation and is no longer used. The application now uses Quasr.io for authentication, which manages its own JWT signing keys.

### 3. Update Terraform Variables

Update your Terraform workflow to pass the new variables:

```yaml
# In .github/workflows/infra.yml
env:
  TF_VAR_scaleway_access_key: ${{ secrets.SCW_ACCESS_KEY }}
  TF_VAR_scaleway_secret_key: ${{ secrets.SCW_SECRET_KEY }}
  TF_VAR_jwt_secret_key: ${{ secrets.JWT_SECRET_KEY }}
  TF_VAR_database_password: ${{ secrets.DATABASE_PASSWORD }}
```

### 4. Deploy Infrastructure

```bash
# Option 1: Via GitHub Actions
# Push to main branch or manually trigger the infrastructure workflow

# Option 2: Manually via Terraform
cd infra/terraform

# Initialize Terraform
terraform init

# Plan changes
terraform plan -var="scaleway_access_key=$SCW_ACCESS_KEY" \
  -var="scaleway_secret_key=$SCW_SECRET_KEY" \
  -var="jwt_secret_key=$JWT_SECRET_KEY" \
  -var="database_password=$DB_PASSWORD"

# Apply changes
terraform apply -var="scaleway_access_key=$SCW_ACCESS_KEY" \
  -var="scaleway_secret_key=$SCW_SECRET_KEY" \
  -var="jwt_secret_key=$JWT_SECRET_KEY" \
  -var="database_password=$DB_PASSWORD"
```

### 5. Deploy Application

The application will be deployed automatically via the deploy pipeline when:
1. Code is pushed to `main` branch
2. Docker image is built successfully
3. Terraform has configured the container with environment variables

## Verification

### 1. Check Infrastructure

```bash
# Verify bucket exists
aws s3 ls s3://clercq-it-blog-images --endpoint-url=https://s3.fr-par.scw.cloud

# Verify container environment variables
# Check Scaleway Console > Containers > clercq-it-app > Environment Variables
```

### 2. Test API Endpoint

```bash
# Get all blogs (no auth required)
curl https://www.clercq.it/api/blogs

# Test authentication (should return 401)
curl -X POST https://www.clercq.it/api/blogs \
  -F "shortDescription=Test" \
  -F "longDescription=Test" \
  -F "tags=Test" \
  -F "image=@test.jpg"

# Expected: 401 Unauthorized
```

### 3. Check Logs

```bash
# Via Scaleway Console
# Navigate to Cockpit > Logs
# Filter by: container_name = "clercq-it-app"
```

## Security Considerations

### JWT Token Generation

> **Note:** This section describes an older implementation. The application now uses **Quasr.io** for authentication and token generation. See [CIAM Documentation](./ciam.md) for current implementation details.

<details>
<summary>Legacy JWT Implementation (for reference only)</summary>

The example below shows how JWT tokens were manually generated before Quasr.io integration. This code is **not used** in the current implementation.

**Example Token Generation:**

```csharp
public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(string userId, string email)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Authentication:JwtSecretKey"]!));
        
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Authentication:Issuer"],
            audience: _configuration["Authentication:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Authentication:ExpirationMinutes"] ?? "60")),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

</details>

### Object Storage Security

- Bucket has **public-read** ACL - anyone can view uploaded images
- Write access requires valid S3 credentials (configured in container)
- Consider implementing image size limits and virus scanning for production

### Environment Variables

All sensitive configuration is stored in environment variables, not in code:
- S3 access credentials
- Database password
- Quasr.io API key

Never commit these values to version control.

## Rollback Plan

If issues occur after deployment:

### 1. Disable Blog Creation

```bash
# Temporarily remove authentication requirement
# Edit BlogsEndpoints.cs and comment out:
# .RequireAuthorization()

# Or deploy a hotfix that returns 503 Service Unavailable
```

### 2. Revert Infrastructure

```bash
cd infra/terraform

# Revert to previous state
terraform apply -target=-scaleway_object_bucket.blog_images

# Or use git to checkout previous version
git checkout <previous-commit> infra/terraform/
terraform apply
```

### 3. Redeploy Previous Version

```bash
# Via GitHub Actions
gh workflow run deploy.yml -f version=<previous-version>

# Via Docker
docker pull echarnus/clercq-it:<previous-version>
# Update container image in Scaleway Console
```

## Monitoring

### Metrics to Monitor

1. **API Response Times**
   - POST /api/blogs endpoint latency
   - Object storage upload time

2. **Error Rates**
   - 401 Unauthorized (authentication failures)
   - 400 Bad Request (validation errors)
   - 500 Internal Server Error (object storage errors)

3. **Object Storage**
   - Bucket storage size
   - Number of objects
   - Bandwidth usage

4. **Costs**
   - Scaleway Object Storage costs (per GB stored and transferred)
   - Container runtime costs

### Alerts to Configure

- High error rate on POST /api/blogs
- Object storage unavailable
- JWT token validation failures spike
- Unusual file upload sizes

## Cost Estimation

### Scaleway Object Storage Pricing (as of 2024)

- **Storage**: €0.01 per GB per month
- **Outbound Transfer**: €0.01 per GB
- **Requests**: Negligible for low volume

**Example Monthly Cost:**
- 100 blog posts with 1MB images = 0.1 GB storage = €0.001/month
- 10,000 image views at 1MB each = 10 GB transfer = €0.10/month
- **Total**: ~€0.10/month for moderate usage

## Troubleshooting

### Issue: Object Storage Not Configured

**Error:** "Object storage is not configured"

**Solution:**
1. Verify environment variables are set in container
2. Check Scaleway Console > Containers > Environment Variables
3. Ensure `ObjectStorage__AccessKey` is not empty

### Issue: 401 Unauthorized

**Error:** 401 response on POST request

**Solution:**
1. Verify JWT token is included in Authorization header
2. Check token format: `Bearer <token>`
3. Verify JWT secret key matches between token generation and validation
4. Check token expiration

### Issue: Image Upload Fails

**Error:** S3 error during upload

**Solution:**
1. Verify S3 credentials are correct
2. Check bucket exists: `aws s3 ls --endpoint-url=https://s3.fr-par.scw.cloud`
3. Verify network connectivity from container to S3 endpoint
4. Check bucket ACL permissions

## Next Steps

After successful deployment:

1. **Implement Token Generation**
   - Create admin endpoint for token generation
   - Or use external identity provider (Auth0, Scaleway IAM)

2. **Add Rate Limiting**
   - Prevent abuse of blog creation endpoint
   - Consider ASP.NET Core Rate Limiting middleware

3. **Implement Image Optimization**
   - Resize images before upload
   - Generate thumbnails
   - Convert to WebP format

4. **Add Blog Management**
   - UPDATE endpoint for editing blogs
   - DELETE endpoint for removing blogs
   - PATCH endpoint for publishing/unpublishing

5. **Frontend Integration**
   - Create blog creation UI
   - Implement markdown editor
   - Add image preview and upload progress

## Support

For issues or questions:
- Check logs in Scaleway Cockpit
- Review API documentation: `/docs/api-blog-creation.md`
- Check architecture: `/docs/architecture.md`
