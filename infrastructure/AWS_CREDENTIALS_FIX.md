# AWS Credentials Fix for Terraform Plan/Apply Steps

## Issue Summary

The infrastructure deployment workflow was missing AWS credentials in the Terraform `plan` and `apply` steps. While `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` environment variables were correctly set during `terraform init`, they were absent from the subsequent plan and apply operations.

### Symptoms
- Potential state access issues during `terraform plan`
- Potential state update issues during `terraform apply`
- Inconsistent credential configuration across workflow steps

## Root Cause

The Terraform S3 backend (used for Scaleway Object Storage) requires AWS-compatible credentials throughout the **entire Terraform lifecycle**, not just during initialization:

1. **`terraform init`** - Connects to S3 backend and initializes state
2. **`terraform plan`** - Reads current state from S3 backend to compare with desired state
3. **`terraform apply`** - Updates state in S3 backend after making changes

The workflow only provided AWS credentials during `init`, but Terraform needs them for all state operations.

## The Fix

### Changes Made to `.github/workflows/infra.yml`

Added AWS credentials to two additional steps:

#### 1. Terraform Plan Step (terraform-plan job)
```yaml
- name: Terraform Plan
  id: plan
  run: |
    cd infrastructure/terraform
    terraform plan -no-color -out=tfplan 2>&1 | tee tfplan.txt
  env:
    AWS_ACCESS_KEY_ID: ${{ secrets.SCALEWAY_ACCESS_KEY }}              # ← Added
    AWS_SECRET_ACCESS_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}          # ← Added
    AWS_SDK_LOAD_CONFIG: "false"                                       # ← Added
    AWS_EC2_METADATA_DISABLED: "true"                                  # ← Added
    SCW_ACCESS_KEY: ${{ secrets.SCALEWAY_ACCESS_KEY }}
    SCW_SECRET_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}
    # ... other variables
```

#### 2. Terraform Apply Step (terraform-deploy job)
```yaml
- name: Terraform Apply
  id: apply
  run: |
    cd infrastructure/terraform
    echo "🚀 Starting infrastructure deployment..."
    terraform apply -auto-approve -no-color
  env:
    AWS_ACCESS_KEY_ID: ${{ secrets.SCALEWAY_ACCESS_KEY }}              # ← Added
    AWS_SECRET_ACCESS_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}          # ← Added
    AWS_SDK_LOAD_CONFIG: "false"                                       # ← Added
    AWS_EC2_METADATA_DISABLED: "true"                                  # ← Added
    SCW_ACCESS_KEY: ${{ secrets.SCALEWAY_ACCESS_KEY }}
    SCW_SECRET_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}
    # ... other variables
```

### Environment Variables Added

All four variables added to each step:

- **`AWS_ACCESS_KEY_ID`** - Scaleway access key (AWS-compatible format)
- **`AWS_SECRET_ACCESS_KEY`** - Scaleway secret key (AWS-compatible format)
- **`AWS_SDK_LOAD_CONFIG`** - Prevents AWS SDK from loading default config files
- **`AWS_EC2_METADATA_DISABLED`** - Prevents AWS SDK from querying EC2 metadata service

## Why This Works

### AWS SDK Credential Chain

The Terraform S3 backend uses the AWS SDK, which needs credentials for every operation that accesses the backend:

```
Terraform Plan
    ↓ Needs to read state from S3
AWS SDK looks for credentials
    ✅ Finds AWS_ACCESS_KEY_ID & AWS_SECRET_ACCESS_KEY
    ↓ Uses Scaleway credentials
Connects to Scaleway Object Storage
    ↓ Backend configured with endpoints block
Reads terraform.tfstate
    ↓ Compares with desired state
Generates plan
```

```
Terraform Apply
    ↓ Needs to update state in S3
AWS SDK looks for credentials
    ✅ Finds AWS_ACCESS_KEY_ID & AWS_SECRET_ACCESS_KEY
    ↓ Uses Scaleway credentials
Connects to Scaleway Object Storage
    ↓ Backend configured with endpoints block
Updates terraform.tfstate
    ↓ State reflects actual infrastructure
Apply complete
```

### Consistent Configuration

Now all Terraform operations have the same credential configuration:

| Step | AWS Credentials | SCW Credentials | Status |
|------|----------------|-----------------|--------|
| `terraform init` | ✅ | ✅ | Was working |
| `terraform plan` | ✅ | ✅ | **Fixed** |
| `terraform apply` | ✅ | ✅ | **Fixed** |

## Expected Behavior After Fix

### Before Fix
- ✅ `terraform init` succeeds (had credentials)
- ⚠️ `terraform plan` may have state access issues
- ⚠️ `terraform apply` may have state update issues
- ❌ Inconsistent credential configuration

### After Fix
- ✅ `terraform init` succeeds
- ✅ `terraform plan` reads state reliably
- ✅ `terraform apply` updates state reliably
- ✅ Consistent credential configuration across all steps

## Backend Configuration Context

This fix works in conjunction with the S3 backend configuration in `infrastructure/terraform/main.tf`:

```hcl
backend "s3" {
  bucket   = "clercq-it-terraform-state"
  key      = "portfolio/terraform.tfstate"
  region   = "fr-par"
  endpoints = {
    s3 = "https://s3.fr-par.scw.cloud"
  }
  skip_credentials_validation = true
  skip_region_validation      = true
  skip_metadata_api_check     = true
  skip_s3_checksum            = true
}
```

The `endpoints` block directs all S3 API calls to Scaleway's Object Storage, while the AWS-compatible credentials allow the standard AWS SDK to authenticate.

## Files Modified

### `.github/workflows/infra.yml`

**Changes:**
1. Added AWS credentials to `terraform-plan` job's plan step (line ~73)
2. Added AWS credentials to `terraform-deploy` job's apply step (line ~168)

**Lines Added:** 8 (4 environment variables × 2 steps)

## Validation

### YAML Syntax Check
```bash
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/infra.yml'))"
# ✅ Valid YAML
```

### Credential Configuration Verification
```bash
grep -n "AWS_ACCESS_KEY_ID\|AWS_SECRET_ACCESS_KEY" .github/workflows/infra.yml
# Should show 4 occurrences (init, plan, init, apply)
```

## Related Documentation

- [TERRAFORM_INIT_AWS_STS_FIX.md](./TERRAFORM_INIT_AWS_STS_FIX.md) - Why AWS credentials are needed for S3 backend
- [SCW_ENV_VARS_FIX.md](./SCW_ENV_VARS_FIX.md) - Complete Scaleway environment variables setup
- [STATE_MANAGEMENT_FLOW.md](./STATE_MANAGEMENT_FLOW.md) - How Terraform state management works
- [SECRETS.md](./SECRETS.md) - Required GitHub Secrets configuration
- [Terraform S3 Backend Documentation](https://developer.hashicorp.com/terraform/language/settings/backends/s3)

## Summary

This fix ensures AWS credentials are available for all Terraform operations that need to access the S3 backend for state management. The changes are minimal and surgical - adding only 4 environment variables to 2 workflow steps.

**Impact:** Ensures reliable state access during plan and apply operations  
**Risk:** Very low - uses existing credentials, no new configuration  
**Compatibility:** 100% backward compatible  
**Testing:** YAML syntax validated
