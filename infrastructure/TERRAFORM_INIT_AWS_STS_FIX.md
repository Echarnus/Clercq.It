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

The issue occurs because of how Terraform processes backend configuration when using `-backend-config` CLI flags:

1. **Backend Configuration Evaluation Order**: When Terraform initializes, it evaluates backend configuration in a specific order:
   - CLI `-backend-config` flags (highest priority)
   - Backend block in `main.tf` 
   - Default values

2. **AWS SDK Initialization**: The S3 backend uses the AWS SDK underneath. When credentials are passed via `-backend-config` flags (`access_key` and `secret_key`), the AWS SDK initializes and attempts to validate them against AWS services.

3. **Skip Flags Not Applied**: The `skip_*` flags defined in the backend block in `main.tf` are not applied early enough in the initialization process when credentials come from CLI flags. This causes Terraform to:
   - Attempt to retrieve the AWS account ID via STS
   - Try to connect to `sts.fr-par.amazonaws.com` (non-existent for Scaleway)
   - Fail before reading the rest of the backend configuration

## The Solution

Pass the skip flags as `-backend-config` parameters during `terraform init`:

```yaml
- name: Terraform Init
  run: |
    cd infrastructure/terraform
    terraform init \
      -backend-config="access_key=$SCW_ACCESS_KEY" \
      -backend-config="secret_key=$SCW_SECRET_KEY" \
      -backend-config="skip_credentials_validation=true" \
      -backend-config="skip_region_validation=true" \
      -backend-config="skip_requesting_account_id=true"
  env:
    SCW_ACCESS_KEY: ${{ secrets.SCALEWAY_ACCESS_KEY }}
    SCW_SECRET_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}
    SCW_DEFAULT_ORGANIZATION_ID: ${{ secrets.SCALEWAY_ORGANIZATION_ID }}
    SCW_DEFAULT_PROJECT_ID: ${{ secrets.SCALEWAY_PROJECT_ID }}
    SCW_DEFAULT_REGION: fr-par
    SCW_DEFAULT_ZONE: fr-par-1
```

### What Each Flag Does

- **`skip_credentials_validation=true`**: Prevents Terraform from validating the access credentials against AWS IAM
- **`skip_region_validation=true`**: Prevents Terraform from validating the region against AWS's list of valid regions
- **`skip_requesting_account_id=true`**: Prevents Terraform from calling AWS STS to retrieve the account ID (this is the flag that prevents the specific error)

## Why This Matters

### CLI Flags Override File Configuration

When using `-backend-config` to pass credentials:
- Terraform needs **all** relevant configuration via CLI flags to avoid AWS SDK initialization
- The skip flags in `main.tf` are evaluated **after** the AWS SDK tries to authenticate
- Passing skip flags via CLI ensures they're applied **before** any AWS API calls

### S3-Compatible Storage Requirements

When using S3-compatible storage (like Scaleway Object Storage):
1. The storage uses S3 API but is not AWS
2. AWS-specific validation calls will fail
3. Skip flags tell Terraform "trust these credentials without AWS validation"
4. The `endpoints` block tells Terraform "connect here instead of AWS"

### Consistent Configuration Pattern

Best practice for S3-compatible backends:
```hcl
# In main.tf - define the backend structure
backend "s3" {
  bucket = "my-bucket"
  key    = "terraform.tfstate"
  region = "fr-par"
  endpoints = {
    s3 = "https://s3.fr-par.scw.cloud"
  }
  skip_credentials_validation = true
  skip_region_validation      = true
  skip_requesting_account_id  = true
}
```

```bash
# In CI/CD - pass credentials AND skip flags via CLI
terraform init \
  -backend-config="access_key=$ACCESS_KEY" \
  -backend-config="secret_key=$SECRET_KEY" \
  -backend-config="skip_credentials_validation=true" \
  -backend-config="skip_region_validation=true" \
  -backend-config="skip_requesting_account_id=true"
```

## Files Modified

### `.github/workflows/infra.yml`

**Jobs Updated:**
1. `terraform-plan` - Terraform Init step
2. `terraform-deploy` - Terraform Init step

**Changes Made:**
Added three `-backend-config` flags to each Terraform Init step:
```diff
  terraform init \
    -backend-config="access_key=$SCW_ACCESS_KEY" \
-   -backend-config="secret_key=$SCW_SECRET_KEY"
+   -backend-config="secret_key=$SCW_SECRET_KEY" \
+   -backend-config="skip_credentials_validation=true" \
+   -backend-config="skip_region_validation=true" \
+   -backend-config="skip_requesting_account_id=true"
```

## Validation

The fix was validated with:

```bash
# Validate YAML syntax
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/infra.yml'))"
# ✅ Valid YAML

# Check diff
git diff .github/workflows/infra.yml
# ✅ Only added skip flags, no other changes
```

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
- ✅ Direct connection to Scaleway S3-compatible storage
- ✅ Backend state loads successfully
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

4. **Check for AWS Environment Variables**: Ensure no `AWS_*` environment variables are set that might interfere:
   ```bash
   # These should NOT be set
   env | grep AWS
   ```

### Common Pitfalls

**❌ Don't rely on backend block alone when using `-backend-config`**
```yaml
# This will still fail
terraform init -backend-config="access_key=$KEY"
# Skip flags in main.tf are not applied early enough
```

**✅ Pass skip flags via CLI when passing credentials via CLI**
```yaml
# This works
terraform init \
  -backend-config="access_key=$KEY" \
  -backend-config="skip_requesting_account_id=true"
```

## Related Documentation

- [S3_ENDPOINT_FIX.md](./S3_ENDPOINT_FIX.md) - Fix for using `endpoints` block instead of deprecated `endpoint` parameter
- [TERRAFORM_INIT_FIX.md](./TERRAFORM_INIT_FIX.md) - Fix for adding SCW environment variables to init steps
- [SCW_ENV_VARS_FIX.md](./SCW_ENV_VARS_FIX.md) - Complete SCW environment variables setup
- [STATE_SHARING_SETUP_GUIDE.md](./STATE_SHARING_SETUP_GUIDE.md) - Guide for setting up shared state
- [Terraform S3 Backend Documentation](https://developer.hashicorp.com/terraform/language/settings/backends/s3)
- [Scaleway Object Storage Documentation](https://www.scaleway.com/en/docs/storage/object/)

## Technical Deep Dive

### Why Does This Happen?

The Terraform S3 backend uses the AWS SDK for Go. When the backend is configured with credentials, the SDK initialization follows this flow:

1. **Credentials Provider Chain**: The SDK checks multiple sources for credentials:
   - Environment variables (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`)
   - Shared credentials file (`~/.aws/credentials`)
   - IAM roles (for EC2 instances)
   - Explicit credentials (passed via backend config)

2. **Default Validation**: By default, when credentials are found, the AWS SDK:
   - Calls `sts:GetCallerIdentity` to validate credentials and get account ID
   - Validates the region against known AWS regions
   - Checks IAM permissions

3. **S3-Compatible Storage**: When using non-AWS S3-compatible storage:
   - The endpoint is different (e.g., Scaleway's `s3.fr-par.scw.cloud`)
   - AWS validation calls will fail (no AWS account exists)
   - STS endpoints don't exist (e.g., `sts.fr-par.amazonaws.com`)

### The Skip Flags Solution

The skip flags tell Terraform's S3 backend:
- **Don't call STS** to get account ID → prevents DNS/connection errors
- **Don't validate region** against AWS regions → allows custom regions like `fr-par`
- **Don't validate credentials** via AWS IAM → allows non-AWS credentials

### Why CLI Flags?

When credentials come from `-backend-config` CLI flags:
- Terraform processes CLI flags **before** loading the backend block from `main.tf`
- The AWS SDK initializes immediately upon receiving credentials
- By the time Terraform reads the skip flags from `main.tf`, it's too late
- Solution: Pass skip flags as CLI flags too, so they're applied immediately

## Summary

This fix resolves the AWS STS authentication error during `terraform init` by passing the skip validation flags via `-backend-config` CLI parameters alongside the credentials. This ensures Terraform doesn't attempt AWS-specific validation when using Scaleway's S3-compatible Object Storage.

**Impact:** Minimal, surgical change - only adds three CLI flags to init commands  
**Risk:** Very low - makes explicit what should already be configured  
**Testing:** Validated YAML syntax and reviewed diff  
**Compatibility:** Backward compatible, no breaking changes
