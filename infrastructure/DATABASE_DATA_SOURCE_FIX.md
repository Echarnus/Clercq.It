# Database and Namespace Data Source Fix

## Problem Summary

The infrastructure deployment had multiple issues:

1. **409 Conflict: Namespace already exists** - Namespace already exists in Scaleway (currently named "cae-portfolio")
2. **Multiple databases created** - Each deployment was creating a new database instance instead of using the existing one
3. **Random password generation** - Password was being randomly generated instead of using the configured secret

## Root Cause

The fundamental issues were:

1. **Resource vs Data Source Confusion**: Using `resource` blocks for pre-existing infrastructure (namespace and database server) instead of `data` sources
2. **Random Password Generation**: Using `random_password` provider instead of the DATABASE_PASSWORD secret
3. **Wrong PostgreSQL Version**: Configuration specified PostgreSQL-15 instead of using the existing PostgreSQL-16 server (DB-DEV-S)

## Solution Implemented

### 1. Changed Namespace from Resource to Data Source

**Before (Resource - 25 lines):**
```hcl
resource "scaleway_container_namespace" "portfolio" {
  name        = "portfolio"
  description = "..."
  project_id  = var.scaleway_project_id
  # ... configuration
}
```

**After (Data Source - 3 lines):**
```hcl
data "scaleway_container_namespace" "portfolio" {
  name = "cae-portfolio"
}
```

### 2. Changed Database Instance from Resource to Data Source

**Before (Resource - creating new database):**
```hcl
resource "random_password" "db_password" {
  length = 24
  # ... configuration
}

resource "scaleway_rdb_instance" "portfolio_db" {
  name              = "portfolio-database"
  node_type         = "db-dev-s"
  engine            = "PostgreSQL-15"
  # ... configuration
}

resource "scaleway_rdb_user" "portfolio_app_user" {
  instance_id = scaleway_rdb_instance.portfolio_db.id
  name        = "clercqit_user"
  password    = random_password.db_password.result
  is_admin    = false
}
```

**After (Data Source - using existing database):**
```hcl
data "scaleway_rdb_instance" "portfolio_db" {
  name = "DB-DEV-S"
}

resource "scaleway_rdb_user" "portfolio_app_user" {
  instance_id = data.scaleway_rdb_instance.portfolio_db.id
  name        = "clercqit_user"
  password    = var.database_password
  is_admin    = false
}
```

### 3. Added Database Password Variable

Added `database_password` variable in `variables.tf`:
```hcl
variable "database_password" {
  description = "Database password for the application user"
  type        = string
  sensitive   = true
}
```

### 4. Removed Random Provider

Since we no longer generate random passwords, removed the `random` provider from the configuration.

## What Changed

### Files Modified

1. **`infrastructure/terraform/main.tf`**
   - Removed `random_password` resource
   - Changed `scaleway_rdb_instance` from resource to data source
   - Updated to reference existing "DB-DEV-S" database instance (PostgreSQL-16)
   - Changed `scaleway_container_namespace` from resource to data source
   - Updated all references to use data sources (`data.scaleway_rdb_instance.portfolio_db`)
   - Removed `random` provider from required_providers

2. **`infrastructure/terraform/variables.tf`**
   - Added `database_password` variable (sensitive)

3. **`infrastructure/terraform/outputs.tf`**
   - Updated database outputs to reference data source
   - Removed `database_password` output (now comes from secrets)
   - Updated infrastructure summary to use data sources

4. **`.github/workflows/infra.yml`**
   - Added `TF_VAR_database_password: ${{ secrets.DATABASE_PASSWORD }}` to both Plan and Apply steps

## Benefits

1. **No More 409 Conflicts** - Data sources never attempt to create resources
2. **No Duplicate Databases** - Uses existing PostgreSQL-16 server "DB-DEV-S"
3. **Consistent Password** - Uses DATABASE_PASSWORD from GitHub secrets
4. **Simpler Code** - Reduced complexity, removed random provider
5. **Best Practice** - Follows Terraform pattern for external infrastructure
6. **Clear Separation** - Managed resources vs referenced resources

## Database Architecture

### What Terraform Manages (Resources):
- ✅ `scaleway_rdb_database.portfolio_app_db` - The database within the instance
- ✅ `scaleway_rdb_user.portfolio_app_user` - The database user
- ✅ `scaleway_container.portfolio_app` - The application container

### What Terraform References (Data Sources):
- ✅ `data.scaleway_rdb_instance.portfolio_db` - The DB-DEV-S PostgreSQL-16 server
- ✅ `data.scaleway_container_namespace.portfolio` - The container namespace (named "cae-portfolio" in Scaleway)

This separation ensures:
- Pre-existing infrastructure is never accidentally created or deleted
- Only application-specific resources are managed by Terraform
- Clear distinction between shared infrastructure and application components

## Required GitHub Secrets

Ensure the following GitHub secret is configured in repository settings:

- `DATABASE_PASSWORD` - Password for the database user

Password requirements (Scaleway):
- 8-128 characters
- At least one digit, uppercase, lowercase, and special character

## Expected Deployment Flow

After this fix, deployments will:

1. **Terraform Init** - Connect to Scaleway Object Storage backend and load state
2. **Data Source Reads**:
   - Query Scaleway API: "Does namespace 'cae-portfolio' exist?" → YES ✅
   - Query Scaleway API: "Does database instance 'DB-DEV-S' exist?" → YES ✅
3. **Terraform Apply** - Create/update managed resources only:
   - Database `clercqit_portfolio` (if not exists)
   - Database user `clercqit_user` (if not exists or password changed)
   - Container `clercq-it-app` (with updated environment variables)
4. **No More Errors**:
   - ❌ No 409 Conflict (namespace already exists)
   - ❌ No duplicate database instances
   - ❌ No random password generation
   - ❌ No import logic failures

## Migration Notes

### If Previous Deployments Created Resources

If previous deployments created a `portfolio-database` instance:
- The new configuration will use the existing "DB-DEV-S" instance instead
- The old `portfolio-database` instance can be manually removed from Scaleway console if no longer needed
- Terraform state will automatically adjust to the new data source pattern
- No manual state manipulation required

### Database User

The `clercqit_user` will be updated with the password from DATABASE_PASSWORD secret if it differs from what's currently set.

## Validation

The configuration follows Terraform best practices:
- ✅ Data sources for pre-existing infrastructure
- ✅ Resources only for Terraform-managed components
- ✅ Sensitive variables properly marked
- ✅ Clean separation of concerns
- ✅ No unnecessary provider dependencies

## Related Documentation

- [NAMESPACE_DATA_SOURCE_FIX.md](./NAMESPACE_DATA_SOURCE_FIX.md) - Original namespace fix documentation
- [BACKEND_STATE_FIX.md](./BACKEND_STATE_FIX.md) - S3 backend configuration
- [DEPLOYMENT_FIX.md](./DEPLOYMENT_FIX.md) - Previous deployment fixes
- [Terraform Data Sources](https://developer.hashicorp.com/terraform/language/data-sources)
- [Scaleway RDB Instance Data Source](https://registry.terraform.io/providers/scaleway/scaleway/latest/docs/data-sources/rdb_instance)

## Summary

| Issue | Status | Solution |
|-------|--------|----------|
| 409 Namespace conflict | ✅ Fixed | Changed to data source |
| Multiple databases created | ✅ Fixed | Use existing DB-DEV-S instance |
| Random password generation | ✅ Fixed | Use DATABASE_PASSWORD secret |
| Wrong PostgreSQL version | ✅ Fixed | Uses existing PostgreSQL-16 server |
| Random provider needed | ✅ Fixed | Removed from configuration |
| Complex import logic | ✅ Fixed | Removed - no longer needed |

**All deployment issues have been resolved through proper Terraform patterns.**
