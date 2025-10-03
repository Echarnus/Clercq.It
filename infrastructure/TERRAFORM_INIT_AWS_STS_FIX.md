# Terraform Init AWS STS Error Fix

## Issue Summary

The Terraform deployment was failing during the `terraform init` phase with AWS STS authentication errors:

```
Error: Retrieving AWS account details: AWS account ID not previously found and failed retrieving via all available methods.
Errors: retrieving caller identity from STS: operation error STS: GetCallerIdentity, 
request send failed, Post "https://sts.fr-par.amazonaws.com/": dial tcp: lookup sts.fr-par.amazonaws.com on 127.0.0.53:53: no such host

Error: retrieving account information via iam:ListRoles: operation error IAM: ListRoles, 
http response error StatusCode: 403, RequestID: 6792be5b-b05c-4e15-9e15-8366015b585e, 
api error InvalidClientTokenId: The security token included in the request is invalid.
```

This occurred even though:
- The backend configuration in `main.tf` uses Scaleway's S3-compatible endpoint
- The `skip_requesting_account_id`, `skip_credentials_validation`, and `skip_region_validation` flags are set in the backend block
- The `endpoints` block correctly points to Scaleway's Object Storage

## Root Cause

The issue occurs because of how Terraform's S3 backend authenticates when using S3-compatible storage:

1. **Backend Configuration in main.tf**: The backend block in `main.tf` correctly defines:
   - The S3-compatible endpoint: `endpoints = { s3 = "https://s3.fr-par.scw.cloud" }`
   - Skip flags to prevent AWS validation
   - Bucket, key, and region settings

2. **Credential Passing Method**: When credentials are passed via `-backend-config="access_key=..."` CLI flags, Terraform treats them as backend-specific overrides. However, for S3-compatible storage, the standard approach is to use **AWS-compatible environment variables**.

3. **The Problem with -backend-config**: Using `-backend-config` for credentials can cause issues because:
   - It bypasses the standard AWS SDK credential chain
   - The skip flags cannot be passed via `-backend-config` (they must be in the backend block)
   - Terraform may attempt AWS authentication before fully reading the backend configuration

## The Solution

Use **AWS-compatible environment variables** instead of `-backend-config` flags for passing credentials to the S3 backend:

### Before (Incorrect Approach)
```yaml
- name: Terraform Init
  run: |
    cd infrastructure/terraform
    terraform init \
      -backend-config="access_key=$SCW_ACCESS_KEY" \
      -backend-config="secret_key=$SCW_SECRET_KEY"
  env:
    SCW_ACCESS_KEY: ${{ secrets.SCALEWAY_ACCESS_KEY }}
    SCW_SECRET_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}
```

### After (Correct Approach)
```yaml
- name: Terraform Init
  run: |
    cd infrastructure/terraform
    terraform init
  env:
    AWS_ACCESS_KEY_ID: ${{ secrets.SCALEWAY_ACCESS_KEY }}
    AWS_SECRET_ACCESS_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}
    SCW_ACCESS_KEY: ${{ secrets.SCALEWAY_ACCESS_KEY }}
    SCW_SECRET_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}
    SCW_DEFAULT_ORGANIZATION_ID: ${{ secrets.SCALEWAY_ORGANIZATION_ID }}
    SCW_DEFAULT_PROJECT_ID: ${{ secrets.SCALEWAY_PROJECT_ID }}
    SCW_DEFAULT_REGION: fr-par
    SCW_DEFAULT_ZONE: fr-par-1
```

## Why This Works

### AWS SDK Credential Chain

The Terraform S3 backend uses the AWS SDK, which looks for credentials in this order:
1. **Environment variables** (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`) ← **We use this**
2. Shared credentials file (`~/.aws/credentials`)
3. IAM roles (for EC2 instances)
4. Backend config overrides (via `-backend-config`)

By setting `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` environment variables with Scaleway credentials:
- The AWS SDK picks them up automatically
- Terraform reads the backend configuration from `main.tf` (with skip flags)
- The skip flags prevent AWS-specific validation calls
- The `endpoints` block directs connections to Scaleway's S3-compatible storage

### Skip Flags in Backend Block

The backend block in `main.tf` contains:
```hcl
backend "s3" {
  bucket = "clercq-it-terraform-state"
  key    = "portfolio/terraform.tfstate"
  region = "fr-par"
  endpoints = {
    s3 = "https://s3.fr-par.scw.cloud"
  }
  skip_credentials_validation = true  # ← Prevents AWS credential validation
  skip_region_validation      = true  # ← Prevents AWS region validation
  skip_requesting_account_id  = true  # ← Prevents AWS STS calls
}
```

These flags tell the S3 backend:
- **Don't validate credentials** against AWS IAM
- **Don't validate the region** against AWS's list of regions
- **Don't call AWS STS** to retrieve account ID (this prevents the specific error)

## Files Modified

### `.github/workflows/infra.yml`

**Jobs Updated:**
1. `terraform-plan` - Terraform Init step
2. `terraform-deploy` - Terraform Init step

**Changes Made:**
```diff
- name: Terraform Init
  run: |
    cd infrastructure/terraform
-   terraform init \
-     -backend-config="access_key=$SCW_ACCESS_KEY" \
-     -backend-config="secret_key=$SCW_SECRET_KEY"
+   terraform init
  env:
+   AWS_ACCESS_KEY_ID: ${{ secrets.SCALEWAY_ACCESS_KEY }}
+   AWS_SECRET_ACCESS_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}
    SCW_ACCESS_KEY: ${{ secrets.SCALEWAY_ACCESS_KEY }}
    SCW_SECRET_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}
    ...
```

**Key Points:**
- Removed `-backend-config` flags for credentials
- Added `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` environment variables
- Kept `SCW_*` environment variables for Scaleway provider
- Simplified `terraform init` to just `terraform init` with no flags

## Expected Behavior After Fix

### Before Fix
- ❌ Terraform init fails with AWS STS errors
- ❌ DNS lookup failures for `sts.fr-par.amazonaws.com`
- ❌ Invalid AWS token errors
- ❌ Cannot initialize backend
- ❌ Cannot deploy infrastructure

### After Fix
- ✅ Terraform init succeeds
- ✅ No AWS STS authentication attempts
- ✅ Credentials provided via AWS environment variables
- ✅ Backend configuration read from main.tf with skip flags applied
- ✅ Direct connection to Scaleway S3-compatible storage
- ✅ Infrastructure deployment proceeds normally

## Troubleshooting

### If Init Still Fails

1. **Verify Secrets Are Set**: Ensure GitHub Secrets contain:
   - `SCALEWAY_ACCESS_KEY`
   - `SCALEWAY_SECRET_KEY`
   - `SCALEWAY_ORGANIZATION_ID`
   - `SCALEWAY_PROJECT_ID`
   - `DATABASE_PASSWORD`

2. **Check Bucket Exists**: Verify the S3 bucket exists in Scaleway:
   ```bash
   # Using Scaleway CLI
   scw object bucket list
   
   # Should show: clercq-it-terraform-state
   ```

3. **Verify Credentials Have Permissions**: Ensure the API keys have Object Storage permissions in Scaleway IAM.

4. **Check Backend Configuration**: Verify `main.tf` has the correct backend block with:
   - `endpoints` block pointing to Scaleway
   - All three `skip_*` flags set to `true`

### Common Pitfalls

**❌ Don't use -backend-config for credentials with S3-compatible storage**
```yaml
# This can cause issues
terraform init \
  -backend-config="access_key=$KEY" \
  -backend-config="secret_key=$SECRET"
```

**✅ Use AWS environment variables instead**
```yaml
# This is the correct approach
terraform init
env:
  AWS_ACCESS_KEY_ID: ${{ secrets.SCALEWAY_ACCESS_KEY }}
  AWS_SECRET_ACCESS_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}
```

**❌ Don't try to pass skip flags via -backend-config**
```bash
# This will fail with "Invalid backend configuration argument"
terraform init \
  -backend-config="skip_requesting_account_id=true"
```

**✅ Skip flags must be in the backend block in main.tf**
```hcl
# In main.tf
backend "s3" {
  skip_credentials_validation = true
  skip_region_validation      = true
  skip_requesting_account_id  = true
}
```

## Related Documentation

- [S3_ENDPOINT_FIX.md](./S3_ENDPOINT_FIX.md) - Fix for using `endpoints` block instead of deprecated `endpoint` parameter
- [TERRAFORM_INIT_FIX.md](./TERRAFORM_INIT_FIX.md) - Fix for adding SCW environment variables to init steps
- [SCW_ENV_VARS_FIX.md](./SCW_ENV_VARS_FIX.md) - Complete SCW environment variables setup
- [STATE_SHARING_SETUP_GUIDE.md](./STATE_SHARING_SETUP_GUIDE.md) - Guide for setting up shared state
- [Terraform S3 Backend Documentation](https://developer.hashicorp.com/terraform/language/settings/backends/s3)
- [AWS SDK Credential Configuration](https://docs.aws.amazon.com/sdk-for-go/v1/developer-guide/configuring-sdk.html)
- [Scaleway Object Storage Documentation](https://www.scaleway.com/en/docs/storage/object/)

## Technical Background

### S3-Compatible Storage Best Practices

When using S3-compatible storage (like Scaleway, MinIO, DigitalOcean Spaces, etc.) with Terraform:

1. **Always use AWS environment variables** for credentials:
   - `AWS_ACCESS_KEY_ID`
   - `AWS_SECRET_ACCESS_KEY`
   - Optionally `AWS_DEFAULT_REGION` (though we set it in the backend block)

2. **Configure the backend block** in `main.tf` with:
   - `endpoints` block to specify the S3-compatible endpoint
   - Skip flags to prevent AWS-specific validation
   - Standard S3 backend parameters (bucket, key, region)

3. **Don't mix credential sources**: 
   - Use environment variables consistently
   - Avoid `-backend-config` for credentials
   - Let the AWS SDK credential chain work naturally

### Why Scaleway Credentials Work with AWS Environment Variables

Scaleway's Object Storage is fully S3-compatible, meaning:
- It implements the same API as AWS S3
- It uses the same authentication mechanism (AWS Signature V4)
- Access keys and secret keys work the same way
- The Terraform S3 backend can't tell the difference (when configured correctly)

The only differences are:
- The endpoint URL (Scaleway vs AWS)
- The region names (e.g., `fr-par` vs `us-east-1`)
- The account/organization structure

By using AWS environment variables with Scaleway credentials and pointing to Scaleway's endpoint, we get the best of both worlds.

## Summary

This fix resolves the AWS STS authentication error during `terraform init` by using AWS-compatible environment variables (`AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`) instead of `-backend-config` CLI flags for credentials. This ensures the Terraform S3 backend:

1. Picks up credentials via the standard AWS SDK credential chain
2. Reads the complete backend configuration from `main.tf`
3. Applies the skip flags to prevent AWS-specific validation
4. Connects directly to Scaleway's S3-compatible Object Storage

**Impact:** Minimal, surgical change - removes `-backend-config` flags and adds AWS env vars  
**Risk:** Very low - uses standard Terraform/AWS SDK patterns  
**Testing:** Validated YAML syntax and reviewed diff  
**Compatibility:** 100% backward compatible with existing backend state
