# Terraform State Sharing Setup Guide

This guide explains how to configure Terraform state sharing between GitHub Actions and Scaleway, so your infrastructure state persists across deployments.

## Overview

Your infrastructure **already has state sharing configured**! The configuration uses:
- **Storage**: Scaleway Object Storage (S3-compatible bucket)
- **Bucket Name**: `clercq-it-terraform-state`
- **Backend**: Terraform S3 backend

This allows multiple workflow runs and team members to share the same infrastructure state.

## What's Already Configured

### 1. Terraform Backend Configuration ✅

Location: `infrastructure/terraform/main.tf`

```hcl
backend "s3" {
  bucket = "clercq-it-terraform-state"
  key    = "portfolio/terraform.tfstate"
  region = "fr-par"
  endpoints = {
    s3 = "https://s3.fr-par.scw.cloud"
  }
  skip_credentials_validation = true
  skip_region_validation      = true
  skip_metadata_api_check  = true
}
```

This tells Terraform to store state in a Scaleway Object Storage bucket instead of locally.

### 2. GitHub Actions Workflow ✅

Location: `.github/workflows/infra.yml`

The workflow automatically:
- Uses your Scaleway credentials from GitHub Secrets
- Initializes Terraform with backend credentials
- Reads/writes state to the S3 bucket

### 3. Backend Setup Script ✅

Location: `infrastructure/scripts/setup-backend.sh`

Automatically creates the S3 bucket if it doesn't exist.

## What You Need to Do

### Step 1: Create Scaleway Object Storage Bucket

You have **two options**:

#### Option A: Let GitHub Actions Create It Automatically (Recommended)

The workflow will automatically create the bucket on the first run using the `setup-backend.sh` script.

**Requirements:**
- Your GitHub Secrets must be configured (see Step 2)
- The workflow will handle everything

#### Option B: Create Manually in Scaleway Console

1. Go to [Scaleway Console](https://console.scaleway.com/object-storage/buckets)
2. Click **"Create bucket"**
3. Fill in the details:
   - **Name**: `clercq-it-terraform-state`
   - **Region**: `fr-par` (Paris, France)
   - **Visibility**: Private (default)
4. Click **"Create bucket"**

### Step 2: Configure GitHub Secrets

Navigate to your repository: **Settings → Secrets and variables → Actions**

You need these secrets (most likely already configured):

#### Required Secrets:

1. **`SCALEWAY_ACCESS_KEY`**
   - Your Scaleway API access key
   - Get from: [Scaleway Console → API Keys](https://console.scaleway.com/iam/api-keys)

2. **`SCALEWAY_SECRET_KEY`**
   - Your Scaleway API secret key
   - Get from: Same location (shown only once when creating)

3. **`SCALEWAY_ORGANIZATION_ID`**
   - Your organization ID
   - Get from: [Scaleway Console → Organization Settings](https://console.scaleway.com/)

4. **`SCALEWAY_PROJECT_ID`**
   - Your project ID
   - Get from: Scaleway Console → Project Settings

5. **`DATABASE_PASSWORD`**
   - Password for your database user
   - Must be strong (8-128 chars, uppercase, lowercase, digit, special char)

For detailed instructions on getting these values, see [SECRETS.md](./SECRETS.md).

### Step 3: Verify Configuration

After setting up secrets, test the configuration:

1. **Check if secrets are set:**
   ```bash
   # In GitHub repository settings
   Settings → Secrets and variables → Actions → Repository secrets
   ```
   You should see all 5 secrets listed.

2. **Trigger a workflow run:**
   - Go to **Actions** tab
   - Select **"Deploy Infra"** workflow
   - Click **"Run workflow"** → **"Run workflow"**

3. **Check the logs:**
   - Look for "Setup Backend Bucket" step
   - Should show: ✅ Bucket already exists (if manual) or ✅ Bucket created successfully

4. **Verify state is saved:**
   After the workflow completes, check Scaleway:
   - Go to Object Storage → Buckets → `clercq-it-terraform-state`
   - You should see: `portfolio/terraform.tfstate`

## How It Works

### State Sharing Flow

```
GitHub Actions Workflow
    ↓
Reads secrets (SCW_ACCESS_KEY, SCW_SECRET_KEY)
    ↓
Terraform init with backend credentials
    ↓
Downloads existing state from S3 bucket (if exists)
    ↓
Runs terraform plan/apply
    ↓
Uploads updated state to S3 bucket
    ↓
Next workflow run uses the updated state ✅
```

### Why This Matters

**Without state sharing:**
- ❌ Each workflow run starts fresh
- ❌ Terraform doesn't know what exists
- ❌ Tries to recreate resources → 409 Conflict errors
- ❌ Can't make updates, only create/destroy

**With state sharing (current setup):**
- ✅ State persists between runs
- ✅ Terraform knows what's deployed
- ✅ Can update existing resources
- ✅ Multiple team members can deploy safely
- ✅ State locking prevents conflicts

## Scaleway Configuration Requirements

### Object Storage Setup

Your Scaleway account needs:

1. **Object Storage enabled** (free tier available)
2. **Bucket in fr-par region** (already configured in code)
3. **API keys with Object Storage permissions**

### API Key Permissions

Your Scaleway API key needs these permissions:

- ✅ **ObjectStorageFullAccess** (for state storage)
- ✅ **ContainersFullAccess** (for serverless containers)
- ✅ **RelationalDatabasesFullAccess** (for PostgreSQL)

To check/update permissions:
1. Go to [API Keys](https://console.scaleway.com/iam/api-keys)
2. Click on your API key
3. Verify permissions under "Attached policies"

## GitHub Actions Configuration

The workflow automatically configures everything. Here's what happens:

### Environment Variables Set

```yaml
env:
  SCW_ACCESS_KEY: ${{ secrets.SCALEWAY_ACCESS_KEY }}
  SCW_SECRET_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}
  SCW_DEFAULT_ORGANIZATION_ID: ${{ secrets.SCALEWAY_ORGANIZATION_ID }}
  SCW_DEFAULT_PROJECT_ID: ${{ secrets.SCALEWAY_PROJECT_ID }}
  SCW_DEFAULT_REGION: fr-par
  SCW_DEFAULT_ZONE: fr-par-1
```

### Terraform Init Command

```bash
terraform init \
  -backend-config="access_key=$SCW_ACCESS_KEY" \
  -backend-config="secret_key=$SCW_SECRET_KEY"
```

This passes your Scaleway credentials to Terraform for accessing the S3 bucket.

## Troubleshooting

### Issue: "Bucket does not exist"

**Solution:**
1. Run the workflow once to create the bucket automatically
2. Or create it manually (see Step 1, Option B)
3. Ensure bucket name is exactly: `clercq-it-terraform-state`

### Issue: "403 Forbidden" accessing bucket

**Solution:**
1. Verify `SCALEWAY_ACCESS_KEY` and `SCALEWAY_SECRET_KEY` are correct
2. Check API key has ObjectStorage permissions
3. Ensure you're using the correct project/organization

### Issue: "Backend initialization required"

**Solution:**
1. This is normal on first run
2. The workflow automatically runs `terraform init`
3. If persists, check GitHub Actions logs for errors

### Issue: State file is empty or missing

**Solution:**
1. Run `terraform apply` once to create initial state
2. Check Scaleway console that file exists in bucket
3. Verify workflow has write permissions

## Security Best Practices

1. **Never commit state files to Git**
   - Already in `.gitignore`
   - State contains sensitive data

2. **Rotate API keys regularly**
   - Update GitHub secrets when rotating

3. **Use separate environments**
   - Consider separate buckets for dev/staging/prod
   - Current setup: single production environment

4. **Enable bucket versioning (optional)**
   - Protects against accidental state deletion
   - Can be enabled in Scaleway console

5. **Monitor bucket access**
   - Check Scaleway audit logs periodically

## Advanced: State Locking

Scaleway Object Storage supports state locking via DynamoDB-compatible tables, but it's not configured yet. State locking prevents concurrent modifications.

**Current status**: Not configured (single-user deployments work fine)

**To add state locking** (optional, advanced):
1. Create a DynamoDB-compatible table in Scaleway
2. Add `dynamodb_table` to backend configuration
3. See [Terraform S3 Backend docs](https://developer.hashicorp.com/terraform/language/settings/backends/s3)

## Related Documentation

- [SECRETS.md](./SECRETS.md) - GitHub Secrets setup guide
- [BACKEND_STATE_FIX.md](./BACKEND_STATE_FIX.md) - Original backend configuration
- [S3_ENDPOINT_FIX.md](./S3_ENDPOINT_FIX.md) - Recent endpoint fix
- [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) - General troubleshooting

## Summary

**Your infrastructure is already configured for state sharing!** 🎉

What you have:
- ✅ Terraform backend configured to use Scaleway Object Storage
- ✅ GitHub Actions workflow configured with credentials
- ✅ Automatic bucket creation on first run

What you need to do:
1. Ensure GitHub Secrets are configured (likely already done)
2. Run the workflow once (bucket will be created automatically)
3. State will persist across all future runs

The state file (`terraform.tfstate`) will be stored in Scaleway Object Storage at:
```
s3://clercq-it-terraform-state/portfolio/terraform.tfstate
```

This allows your GitHub Actions workflows to share infrastructure state, preventing conflicts and enabling proper infrastructure updates.
