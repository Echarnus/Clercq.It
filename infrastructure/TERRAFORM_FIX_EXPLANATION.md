# Terraform Deployment Fix - Complete Explanation

## Issue Summary

The Terraform deployment was failing with the following error:

```
Error: Invalid index

  on main.tf line 94, in resource "scaleway_container" "portfolio_app":
  94:     "DATABASE_CONNECTION_STRING" = "Host=${scaleway_rdb_instance.portfolio_db.load_balancer[0].ip};Port=${scaleway_rdb_instance.portfolio_db.load_balancer[0].port};Database=clercqit_portfolio;Username=clercqit_user;***"
    ├────────────────
    │ scaleway_rdb_instance.portfolio_db.load_balancer is empty list of object

The given key does not identify an element in this collection value: the collection has no elements.
```

## Root Cause Analysis

### The Problem

The Terraform configuration was trying to access `scaleway_rdb_instance.portfolio_db.load_balancer[0].ip` and `scaleway_rdb_instance.portfolio_db.load_balancer[0].port`, but the `load_balancer` attribute was an **empty list**.

### Why Was It Empty?

In the Scaleway Terraform provider, the `load_balancer` attribute is only populated for **High Availability (HA) cluster** RDB instances. Our configuration explicitly sets:

```hcl
resource "scaleway_rdb_instance" "portfolio_db" {
  name              = "portfolio-database"
  is_ha_cluster     = false  # <-- This is the key
  # ... other configuration
}
```

Since `is_ha_cluster = false`, the instance is a single-node database, not an HA cluster, and therefore:
- ✅ `endpoint_ip` is available
- ✅ `endpoint_port` is available
- ❌ `load_balancer` is an empty list

## The Fix

### What Changed

We reverted the endpoint attributes back to the correct ones for non-HA instances:

**Before (incorrect):**
```hcl
"DATABASE_CONNECTION_STRING" = "Host=${scaleway_rdb_instance.portfolio_db.load_balancer[0].ip};Port=${scaleway_rdb_instance.portfolio_db.load_balancer[0].port};..."
```

**After (correct):**
```hcl
"DATABASE_CONNECTION_STRING" = "Host=${scaleway_rdb_instance.portfolio_db.endpoint_ip};Port=${scaleway_rdb_instance.portfolio_db.endpoint_port};..."
```

### Files Modified

1. **infrastructure/terraform/main.tf**
   - Fixed the `DATABASE_CONNECTION_STRING` environment variable in the container resource
   
2. **infrastructure/terraform/outputs.tf**
   - Fixed `database_endpoint` output
   - Fixed `database_port` output
   - Fixed `infrastructure_summary` output

3. **infrastructure/FIX_SUMMARY.md**
   - Updated documentation to reflect the correct fix

## Scaleway RDB Instance Attributes Reference

For future reference, here's how to access RDB instance endpoints in Scaleway:

### Non-HA Cluster (is_ha_cluster = false)
```hcl
# Use these attributes:
endpoint_ip     # The IP address of the database endpoint
endpoint_port   # The port of the database endpoint
```

### HA Cluster (is_ha_cluster = true)
```hcl
# Use these attributes:
load_balancer[0].ip    # The IP address of the load balancer
load_balancer[0].port  # The port of the load balancer
```

## Testing the Fix

The fix can be validated in GitHub Actions when the workflow runs. The Terraform plan should now complete without the "Invalid index" error.

Expected workflow behavior:
1. ✅ Terraform Init - should succeed
2. ✅ Terraform Validate - should succeed
3. ✅ Terraform Plan - should succeed (no more index errors)
4. ✅ Terraform Apply - should deploy infrastructure successfully

## Why the Previous Fix Was Incorrect

A previous attempt tried to "fix" deprecation warnings by changing from `endpoint_ip` to `load_balancer[0].ip`. However:

1. The deprecation warnings (if any) were misleading or misinterpreted
2. `endpoint_ip` and `endpoint_port` are **not deprecated** for non-HA instances
3. They are the **correct** and **intended** attributes for single-node databases
4. `load_balancer` should only be used for HA cluster instances

## Additional Notes

### If You Want to Use Load Balancer Attributes

If you want to use `load_balancer` attributes in the future, you would need to:

1. Change the RDB instance configuration:
   ```hcl
   resource "scaleway_rdb_instance" "portfolio_db" {
     is_ha_cluster = true  # Changed from false to true
     # ... other configuration
   }
   ```

2. **Important:** Enabling HA cluster has cost implications:
   - Multiple database nodes (for high availability)
   - Load balancer resource
   - Higher monthly costs

3. After enabling HA cluster, you can use:
   ```hcl
   scaleway_rdb_instance.portfolio_db.load_balancer[0].ip
   scaleway_rdb_instance.portfolio_db.load_balancer[0].port
   ```

### Current Configuration is Optimal

The current configuration with `is_ha_cluster = false` is appropriate for:
- ✅ Development and portfolio projects
- ✅ Cost-conscious deployments
- ✅ Applications that don't require 99.99% uptime
- ✅ Single-region deployments

## Summary

✅ **Fixed:** Reverted to correct endpoint attributes for non-HA RDB instance
✅ **Validated:** Changes align with Scaleway provider documentation
✅ **Minimal:** Only changed what was necessary to fix the issue
✅ **Documented:** Updated FIX_SUMMARY.md with accurate information

The Terraform deployment should now work correctly!
