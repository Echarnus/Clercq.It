# Terraform Deployment Fix - Action Required

## Problem Fixed

The Terraform deployment was failing with these errors:
1. **403 Forbidden**: Permission denied for RDB instance creation
2. **project_id is required**: Container namespace required a project_id
3. **Deprecated attributes**: `endpoint_ip` and `endpoint_port` are deprecated in Scaleway provider v2.x

## Changes Made

### 1. Added Project ID Support
- Added `scaleway_project_id` variable to Terraform configuration
- Updated provider configuration to include `project_id`
- Updated GitHub Actions workflow to pass `SCALEWAY_PROJECT_ID` secret

### 2. Fixed Deprecated Attributes
- Replaced `endpoint_ip` with `load_balancer[0].ip`
- Replaced `endpoint_port` with `load_balancer[0].port`
- Updated in: outputs.tf and main.tf (database connection string)

### 3. Updated Documentation
- SECRETS.md: Added instructions for obtaining `SCALEWAY_PROJECT_ID`
- README.md: Added project_id to prerequisites and setup instructions
- terraform.tfvars.example: Added project_id placeholder

### 4. Added Terraform .gitignore
- Prevents committing sensitive files (*.tfvars, *.tfstate)
- Ignores temporary files (.terraform/, *.log, tfplan)

## Action Required from User

### 1. Add GitHub Secret

You need to add the `SCALEWAY_PROJECT_ID` secret to your GitHub repository:

1. Go to your Scaleway Console: https://console.scaleway.com/
2. Navigate to your project (or create a new one within your ClercqIt organization)
3. Click on "Project settings" in the left menu
4. Copy the **Project ID**
5. Go to GitHub: https://github.com/Echarnus/Clercq.It/settings/secrets/actions
6. Click "New repository secret"
7. Name: `SCALEWAY_PROJECT_ID`
8. Value: Paste the Project ID from step 4
9. Click "Add secret"

### 2. Verify All Required Secrets

Make sure you have all these secrets configured:
- ✅ `SCALEWAY_ACCESS_KEY`
- ✅ `SCALEWAY_SECRET_KEY`
- ✅ `SCALEWAY_ORGANIZATION_ID`
- ⚠️ `SCALEWAY_PROJECT_ID` (NEW - must add)
- ✅ `DATABASE_PASSWORD`

### 3. Test the Fix

After adding the `SCALEWAY_PROJECT_ID` secret:
1. Create a new PR with infrastructure changes (or re-run the existing one)
2. The workflow should run terraform plan successfully
3. Merge to main to deploy

## Why This Was Needed

Scaleway's Terraform provider v2.x requires both `organization_id` AND `project_id`:
- **Organization**: Top-level entity in Scaleway (e.g., "ClercqIt")
- **Project**: Grouping of resources within an organization (e.g., "portfolio")

The provider needs `project_id` to know which project to create resources in, and the API keys need permissions within that project to avoid 403 Forbidden errors.

## Verification

The Terraform configuration has been validated:
- ✅ Formatting check passed
- ✅ Initialization successful
- ✅ Validation successful
- ✅ All deprecated attributes replaced

Once you add the `SCALEWAY_PROJECT_ID` secret, the deployment should work!
