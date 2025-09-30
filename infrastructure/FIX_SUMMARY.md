# Terraform Deployment Fix - Corrected Load Balancer Issue

## Problem Fixed

The Terraform deployment was failing with these errors:
1. **Invalid index**: `load_balancer[0]` was empty because `is_ha_cluster = false`
2. **Incorrect attribute usage**: `load_balancer` is only available for HA cluster RDB instances

## Root Cause

The previous fix incorrectly assumed that `load_balancer[0].ip` and `load_balancer[0].port` should be used for all RDB instances. However:
- **HA Cluster instances** (`is_ha_cluster = true`): Use `load_balancer[0].ip` and `load_balancer[0].port`
- **Non-HA instances** (`is_ha_cluster = false`): Use `endpoint_ip` and `endpoint_port`

Since our configuration has `is_ha_cluster = false`, the `load_balancer` list is empty, causing the "Invalid index" error.

## Changes Made

### 1. Reverted to Correct Endpoint Attributes
- Changed `load_balancer[0].ip` back to `endpoint_ip`
- Changed `load_balancer[0].port` back to `endpoint_port`
- Updated in: outputs.tf and main.tf (database connection string)

### 2. Why This is Correct
- For non-HA RDB instances, `endpoint_ip` and `endpoint_port` are the **correct** attributes to use
- These attributes are not deprecated for non-HA instances
- The deprecation warnings in some Scaleway provider versions only apply when you should be using HA clusters with load balancers

## Verification

To verify this fix works locally (if you have Terraform and Scaleway credentials):

```bash
cd infrastructure/terraform
terraform init
terraform validate
terraform plan
```

## Additional Notes

If you want to use `load_balancer` attributes in the future, you would need to:
1. Change `is_ha_cluster = false` to `is_ha_cluster = true` in the RDB instance configuration
2. Note that HA clusters have higher costs due to multiple database nodes
