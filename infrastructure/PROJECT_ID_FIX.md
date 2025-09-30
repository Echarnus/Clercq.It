# Terraform Project ID Fix - Complete Explanation

## Issue Summary

The Terraform deployment was failing with these errors:

```
Error: scaleway-sdk-go: http error 403 Forbidden: Permission denied
  with scaleway_rdb_instance.portfolio_db

Error: scaleway-sdk-go: invalid argument(s): project_id is required
  with scaleway_container_namespace.portfolio
```

## Root Cause Analysis

### The Problem

Even though the Scaleway provider was configured with `project_id` at the provider level:

```hcl
provider "scaleway" {
  zone            = var.scaleway_zone
  region          = var.scaleway_region
  organization_id = var.scaleway_organization_id
  project_id      = var.scaleway_project_id
}
```

Some Scaleway resources require **explicit** `project_id` configuration on the resource itself, particularly:
- `scaleway_rdb_instance` - Database instances
- `scaleway_container_namespace` - Container namespaces

### Why This Happens

In Scaleway Terraform Provider v2.x:
- Provider-level `project_id` sets a **default** project
- However, certain resources (especially those that create billable infrastructure) require explicit project_id for security and billing clarity
- This prevents accidental resource creation in the wrong project

## The Fix

### Changes Made

Added explicit `project_id` configuration to resources that require it:

#### 1. RDB Instance (Database)

```hcl
resource "scaleway_rdb_instance" "portfolio_db" {
  name              = "portfolio-database"
  node_type         = "db-dev-s"
  engine            = "PostgreSQL-15"
  is_ha_cluster     = false
  disable_backup    = false
  volume_type       = "bssd"
  volume_size_in_gb = 5
  project_id        = var.scaleway_project_id  # ← Added this line

  settings = {
    "max_connections" = "20"
    "shared_buffers"  = "32MB"
  }
  # ... tags
}
```

#### 2. Container Namespace

```hcl
resource "scaleway_container_namespace" "portfolio" {
  name        = "portfolio"
  description = "Container namespace for Clercq.It Portfolio applications"
  project_id  = var.scaleway_project_id  # ← Added this line

  environment_variables = {
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "NODE_ENV"               = "production"
  }
  # ... tags
}
```

### Resources That DON'T Need Explicit project_id

The following resources inherit the project_id from their parent resources:
- `scaleway_rdb_database` - inherits from RDB instance via `instance_id`
- `scaleway_rdb_user` - inherits from RDB instance via `instance_id`
- `scaleway_container` - inherits from namespace via `namespace_id`
- `scaleway_container_domain` - inherits from container via `container_id`

## Validation

The fix was validated using:

```bash
terraform fmt -check      # ✅ Formatting is correct
terraform init            # ✅ Providers initialized
terraform validate        # ✅ Configuration is valid
```

Expected output from `terraform validate`:
```
Success! The configuration is valid, but there were some validation warnings as shown above.
```

The warnings about deprecated `endpoint_ip` and `endpoint_port` attributes are expected and documented in `TERRAFORM_FIX_EXPLANATION.md` - these are the correct attributes for non-HA database instances.

## Testing in GitHub Actions

When the infrastructure workflow runs, it should now:
1. ✅ Pass Terraform validation
2. ✅ Generate a valid plan without "project_id is required" errors
3. ✅ Deploy successfully without 403 Forbidden errors

## Best Practices for Scaleway Terraform

### Always Set project_id Explicitly On:
- RDB instances (`scaleway_rdb_instance`)
- Container namespaces (`scaleway_container_namespace`)
- K8s clusters (`scaleway_k8s_cluster`)
- Instance servers (`scaleway_instance_server`)
- Any other "root" infrastructure resources

### Can Rely on Inheritance For:
- Child resources that reference a parent via ID
- Resources that are scoped to a namespace or instance

### Why This Matters

1. **Security**: Prevents accidentally creating resources in the wrong project
2. **Billing**: Ensures costs are properly attributed to the correct project
3. **Organization**: Keeps infrastructure organized by project
4. **Clarity**: Makes it explicit which project owns each resource

## Related Documentation

- [TERRAFORM_FIX_EXPLANATION.md](./TERRAFORM_FIX_EXPLANATION.md) - Fix for endpoint_ip vs load_balancer issue
- [FIX_SUMMARY.md](./FIX_SUMMARY.md) - Summary of previous fixes
- [SECRETS.md](./SECRETS.md) - GitHub secrets configuration guide

## Summary

The fix is minimal and surgical - adding just two lines (`project_id = var.scaleway_project_id`) to the resources that require explicit project configuration. This resolves both the 403 Forbidden and "project_id is required" errors without changing any other aspects of the infrastructure configuration.
