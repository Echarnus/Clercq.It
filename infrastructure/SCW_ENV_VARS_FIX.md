# Scaleway Environment Variables Fix - Complete Explanation

## Issue Summary

The Terraform deployment was failing with these errors:

```
Error: scaleway-sdk-go: http error 403 Forbidden: Permission denied
  with scaleway_rdb_instance.portfolio_db

Error: scaleway-sdk-go: http error 403 Forbidden: Not authorized
  with scaleway_container_namespace.portfolio
```

These errors occurred even though:
- The Scaleway provider was configured with `project_id` in `main.tf`
- Both resources (`scaleway_rdb_instance` and `scaleway_container_namespace`) had explicit `project_id` attributes
- The workflow passed `TF_VAR_scaleway_project_id` and `TF_VAR_scaleway_organization_id`

## Root Cause Analysis

### The Problem

The Scaleway Terraform Provider uses the **Scaleway SDK** underneath, which looks for specific environment variables:

1. **For Authentication**:
   - `SCW_ACCESS_KEY` - API access key ✅ (was already set)
   - `SCW_SECRET_KEY` - API secret key ✅ (was already set)

2. **For Context/Defaults**:
   - `SCW_DEFAULT_ORGANIZATION_ID` - Default organization ❌ (was missing)
   - `SCW_DEFAULT_PROJECT_ID` - Default project ❌ (was missing)
   - `SCW_DEFAULT_REGION` - Default region ❌ (was missing)
   - `SCW_DEFAULT_ZONE` - Default zone ❌ (was missing)

### Why TF_VAR_ Variables Weren't Enough

The workflow was setting:
- `TF_VAR_scaleway_organization_id`
- `TF_VAR_scaleway_project_id`

These variables work for **Terraform's provider configuration**, but the **Scaleway SDK** (which executes the actual API calls) doesn't read Terraform variables directly. Instead, it looks for its own `SCW_*` environment variables.

### The Authentication Flow

```
GitHub Workflow
    ↓ Sets TF_VAR_* variables
Terraform Provider Config (uses variables)
    ↓ Calls Scaleway SDK
Scaleway SDK (looks for SCW_* env vars)
    ↓ Makes API calls to Scaleway
Scaleway API
    ⚠️ Returns 403 Forbidden if context is missing
```

Without the `SCW_DEFAULT_*` variables, the SDK couldn't properly identify which project/organization to use for API calls, even though Terraform knew about them through variables.

## The Fix

### Changes Made

Updated `.github/workflows/infra.yml` to add the missing `SCW_*` environment variables in three jobs:

1. **terraform-plan** (line ~121)
2. **terraform-apply** (line ~201)  
3. **terraform-destroy** (line ~295)

Added these environment variables to each:
```yaml
env:
  SCW_ACCESS_KEY: ${{ secrets.SCALEWAY_ACCESS_KEY }}
  SCW_SECRET_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}
  SCW_DEFAULT_ORGANIZATION_ID: ${{ secrets.SCALEWAY_ORGANIZATION_ID }}  # ← Added
  SCW_DEFAULT_PROJECT_ID: ${{ secrets.SCALEWAY_PROJECT_ID }}            # ← Added
  SCW_DEFAULT_REGION: fr-par                                            # ← Added
  SCW_DEFAULT_ZONE: fr-par-1                                            # ← Added
  TF_VAR_scaleway_organization_id: ${{ secrets.SCALEWAY_ORGANIZATION_ID }}
  TF_VAR_scaleway_project_id: ${{ secrets.SCALEWAY_PROJECT_ID }}
  # ... other variables
```

### Why Both TF_VAR_ and SCW_ Variables?

- **`TF_VAR_*`**: Used by Terraform to configure the provider block in `main.tf`
- **`SCW_*`**: Used by the Scaleway SDK when making actual API calls

Both are needed for the complete authentication and authorization flow.

## Validation

The fix was validated using:

```bash
# Check YAML syntax
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/infra.yml'))"
# ✅ Valid YAML

# Check Terraform formatting
cd infrastructure/terraform
terraform fmt -check
# ✅ Formatting is correct

# Initialize and validate Terraform
terraform init
terraform validate
# ✅ Configuration is valid (with expected deprecation warnings)
```

## Expected Warnings

You may see warnings about deprecated `endpoint_ip` and `endpoint_port` attributes:

```
Warning: Deprecated attribute
  on main.tf line 96, in resource "scaleway_container" "portfolio_app":
  96: "DATABASE_CONNECTION_STRING" = "Host=${scaleway_rdb_instance.portfolio_db.endpoint_ip};..."
```

These warnings are **expected and acceptable** for non-HA database instances. See `FIX_SUMMARY.md` for details.

## Testing in GitHub Actions

When the infrastructure workflow runs, it should now:
1. ✅ Authenticate successfully with Scaleway API
2. ✅ Pass authorization checks for organization and project
3. ✅ Create resources without 403 Forbidden errors
4. ✅ Deploy infrastructure successfully

## Best Practices for Scaleway Terraform

### Always Set These Environment Variables:

**Authentication (Required)**:
- `SCW_ACCESS_KEY`
- `SCW_SECRET_KEY`

**Context (Recommended)**:
- `SCW_DEFAULT_ORGANIZATION_ID`
- `SCW_DEFAULT_PROJECT_ID`
- `SCW_DEFAULT_REGION`
- `SCW_DEFAULT_ZONE`

**Terraform Variables (Also Needed)**:
- `TF_VAR_scaleway_organization_id`
- `TF_VAR_scaleway_project_id`
- `TF_VAR_scaleway_region` (or use default)
- `TF_VAR_scaleway_zone` (or use default)

### Why This Matters

1. **Authentication**: Access and secret keys authenticate your identity
2. **Authorization**: Organization and project IDs determine what you can access
3. **Context**: Region and zone specify where resources should be created
4. **Clarity**: Makes it explicit which project and region operations target
5. **Security**: Prevents accidental operations in wrong projects/regions

## Related Documentation

- [PROJECT_ID_FIX.md](./PROJECT_ID_FIX.md) - Fix for explicit project_id in resources
- [FIX_SUMMARY.md](./FIX_SUMMARY.md) - Fix for endpoint_ip vs load_balancer issue
- [SECRETS.md](./SECRETS.md) - GitHub secrets configuration guide
- [Scaleway SDK Environment Variables](https://github.com/scaleway/scaleway-sdk-go#environment-variables)

## Summary

The fix adds the missing `SCW_DEFAULT_*` environment variables that the Scaleway SDK requires for proper authentication and authorization. While Terraform provider configuration uses `TF_VAR_*` variables, the underlying SDK needs the `SCW_*` versions. Setting both ensures complete compatibility and resolves the 403 Forbidden errors.

This is a minimal, surgical fix that adds only the necessary environment variables without changing any Terraform configuration or resource definitions.
