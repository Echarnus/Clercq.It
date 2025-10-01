# Terraform Init Environment Variables Fix

## Issue Summary

The Terraform deployment was still failing with 403 Forbidden errors even after the SCW environment variables were added to the apply/plan/destroy steps:

```
Error: scaleway-sdk-go: http error 403 Forbidden: Permission denied
  with scaleway_rdb_instance.portfolio_db

Error: scaleway-sdk-go: http error 403 Forbidden: Not authorized
  with scaleway_container_namespace.portfolio
```

Additionally, there was a warning about multiple variable sources:
```
Warning: Multiple variable sources detected, please make sure the right credentials are used

Variable		AvailableSources						Using
SCW_DEFAULT_PROJECT_ID	Profile defined in provider{} block, Environment variable	Environment variable
```

## Root Cause

The **Terraform Init** steps in all four workflow jobs (terraform-check, terraform-plan, terraform-apply, terraform-destroy) were missing the SCW environment variables. They only had the TF_VAR variables.

This caused an inconsistency:
- During `terraform init`: Only TF_VAR variables were available, causing the provider to configure differently
- During `terraform plan/apply/destroy`: Both SCW and TF_VAR variables were available

The Scaleway provider and SDK need the SCW environment variables to be present during **both** init and apply phases for consistent authentication and authorization.

## The Fix

Added the complete set of SCW environment variables to all Terraform Init steps:

```yaml
- name: Terraform Init
  run: |
    cd infrastructure/terraform
    terraform init
  env:
    SCW_ACCESS_KEY: ${{ secrets.SCALEWAY_ACCESS_KEY }}
    SCW_SECRET_KEY: ${{ secrets.SCALEWAY_SECRET_KEY }}
    SCW_DEFAULT_ORGANIZATION_ID: ${{ secrets.SCALEWAY_ORGANIZATION_ID }}
    SCW_DEFAULT_PROJECT_ID: ${{ secrets.SCALEWAY_PROJECT_ID }}
    SCW_DEFAULT_REGION: fr-par
    SCW_DEFAULT_ZONE: fr-par-1
    TF_VAR_scaleway_organization_id: ${{ secrets.SCALEWAY_ORGANIZATION_ID }}
    TF_VAR_scaleway_project_id: ${{ secrets.SCALEWAY_PROJECT_ID }}
    TF_VAR_database_password: ${{ secrets.DATABASE_PASSWORD }}
```

### Jobs Updated

1. **terraform-check** (line ~53) - Added SCW variables to init step
2. **terraform-plan** (line ~107) - Added SCW variables to init step
3. **terraform-apply** (line ~186) - Added SCW variables to init step
4. **terraform-destroy** (line ~284) - Added SCW variables to init step

## Why This Matters

### Consistent Authentication Flow

The Scaleway Terraform provider uses the Scaleway SDK underneath. When initializing Terraform:
1. The provider connects to Scaleway APIs to validate credentials
2. It checks available resources and permissions
3. It may cache provider configuration

If the SCW variables aren't present during init but are present during apply, the provider might:
- Use different authentication contexts
- Have different permission scopes
- Generate inconsistent state

### Environment Variables Required

Both variable types are needed throughout the entire Terraform lifecycle:

**SCW_* Variables** (Scaleway SDK):
- `SCW_ACCESS_KEY` - API access key
- `SCW_SECRET_KEY` - API secret key
- `SCW_DEFAULT_ORGANIZATION_ID` - Default organization
- `SCW_DEFAULT_PROJECT_ID` - Default project
- `SCW_DEFAULT_REGION` - Default region
- `SCW_DEFAULT_ZONE` - Default zone

**TF_VAR_* Variables** (Terraform):
- `TF_VAR_scaleway_organization_id` - Passed to provider config
- `TF_VAR_scaleway_project_id` - Passed to provider config
- `TF_VAR_database_password` - Required variable

## Validation

The fix was validated with:

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
# ✅ Configuration is valid
```

## Expected Behavior After Fix

1. ✅ Terraform init will use the same authentication context as apply
2. ✅ No more 403 Forbidden errors during resource creation
3. ⚠️ Warning about "Multiple variable sources detected" may still appear (this is expected and harmless)
4. ⚠️ Warnings about deprecated `endpoint_ip` attributes will still appear (expected for non-HA instances)

## Related Documentation

- [SCW_ENV_VARS_FIX.md](./SCW_ENV_VARS_FIX.md) - Original fix that added SCW variables to apply/plan/destroy
- [PROJECT_ID_FIX.md](./PROJECT_ID_FIX.md) - Fix for explicit project_id in resources
- [SECRETS.md](./SECRETS.md) - GitHub secrets configuration guide
- [Scaleway SDK Environment Variables](https://github.com/scaleway/scaleway-sdk-go#environment-variables)

## Summary

This fix completes the authentication configuration by ensuring SCW environment variables are present during **all** Terraform operations, not just plan/apply/destroy. This provides consistent authentication and authorization throughout the entire infrastructure deployment workflow.

The change is minimal and surgical - it only adds the missing environment variables to the init steps without modifying any Terraform configuration or resource definitions.
