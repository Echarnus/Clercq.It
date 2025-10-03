# Infrastructure Deployment Fix Summary

## Issue
The infrastructure deployment was failing with:
```
Error: scaleway-sdk-go: http error 409 Conflict: Namespace already exist
```

## Root Cause
The Terraform state was **not persisting between GitHub Actions workflow runs**. This meant:
- Every deployment started with a fresh/empty state
- Terraform didn't know that resources already existed
- It tried to create resources that were already in Scaleway
- Result: 409 Conflict errors

## Solution
Added **remote state backend** using Scaleway Object Storage (S3-compatible):

### 1. Backend Configuration (`main.tf`)
```hcl
backend "s3" {
  bucket   = "clercq-it-terraform-state"
  key      = "portfolio/terraform.tfstate"
  region = "fr-par"
  endpoint = "https://s3.fr-par.scw.cloud"
  ...
}
```

### 2. Backend Setup Script (`scripts/setup-backend.sh`)
- Automatically creates S3 bucket if needed
- Runs before Terraform init in workflow
- Handles both AWS CLI and manual setup

### 3. Workflow Updates (`.github/workflows/infra.yml`)
- Added "Setup Backend Bucket" step
- Updated "Terraform Init" to pass backend credentials
- Improved import logic for existing resources

## How It Works

### Before (Broken)
1. Workflow runs
2. Terraform init with **local state** (in runner)
3. State is empty, Terraform doesn't know resources exist
4. Tries to create namespace → **409 Conflict!**
5. Runner terminates, state is lost

### After (Fixed)
1. Workflow runs
2. Setup backend bucket
3. Terraform init with **remote state** (in S3)
4. State is loaded from S3
5. Terraform sees existing resources → **No conflict!**
6. State is saved back to S3 for next run

## Key Benefits

✅ **State Persists** - Survives workflow runs
✅ **No Conflicts** - Terraform knows what exists
✅ **Team Collaboration** - Shared state
✅ **Best Practice** - Standard Terraform pattern
✅ **Minimal Cost** - S3 storage ~€0.01/month

## Files Changed

1. `infrastructure/terraform/main.tf` - Added backend configuration
2. `infrastructure/terraform/backend.tf` - Documentation
3. `infrastructure/scripts/setup-backend.sh` - Setup script
4. `.github/workflows/infra.yml` - Workflow updates
5. `infrastructure/README.md` - Updated setup instructions
6. `infrastructure/BACKEND_STATE_FIX.md` - Detailed documentation

## Next Steps for User

The backend bucket needs to be created once before the first deployment:

**Option 1: Automatic (via workflow)**
- The workflow will automatically create the bucket on first run
- No manual action needed!

**Option 2: Manual (if needed)**
```bash
export SCW_ACCESS_KEY="your-key"
export SCW_SECRET_KEY="your-secret"
bash infrastructure/scripts/setup-backend.sh
```

## Testing

After merging this PR:
1. Trigger the infrastructure workflow
2. It will create the backend bucket (if needed)
3. Initialize Terraform with remote state
4. Import existing namespace (if needed)
5. Apply changes successfully without 409 errors

## Deprecation Warnings

The warnings about `endpoint_ip` are **informational only**:
- ✅ Correct for non-HA database instances
- ✅ Don't block deployment
- ⚠️ Accept them or upgrade to HA cluster (5-10x cost increase)

---

**Status**: Ready for deployment 🚀
