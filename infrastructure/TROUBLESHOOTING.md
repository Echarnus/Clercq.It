# Infrastructure Deployment - Quick Troubleshooting Guide

## Common Issues and Solutions

### ❌ Error: "Namespace already exists" (409 Conflict)

**Symptom:**
```
Error: scaleway-sdk-go: http error 409 Conflict: Namespace already exist
```

**Root Cause:** Attempting to create a namespace that already exists in Scaleway.

**Solution:** ✅ **FIXED** by using data source instead of resource (see `NAMESPACE_DATA_SOURCE_FIX.md`)

**What Changed:**
- Changed from `resource "scaleway_container_namespace"` to `data "scaleway_container_namespace"`
- Data sources reference existing infrastructure without trying to create it
- Removed import logic from workflow (no longer needed)

**Previous Solutions (if you're on an older version):**
1. S3 backend for state persistence (see `BACKEND_STATE_FIX.md`)
2. Import logic in workflow
3. Check if S3 bucket exists: https://console.scaleway.com/object-storage/buckets

---

### ⚠️ Warning: "endpoint_ip is deprecated"

**Symptom:**
```
Warning: Deprecated attribute
  on main.tf line 121: "endpoint_ip"
The attribute "endpoint_ip" is deprecated.
```

**Root Cause:** Informational warning from Terraform provider.

**Solution:** ✅ **ACCEPTABLE** - These warnings don't block deployment

**Details:**
- For non-HA databases (our config), `endpoint_ip` is **correct**
- Warnings are informational only
- To eliminate warnings, upgrade to HA cluster (5-10x cost increase)
- See `DEPLOYMENT_FIX.md` section 4 for details

**Action Required:** None - accept the warnings

---

### ❌ Error: "Backend bucket does not exist"

**Symptom:**
```
Error: Failed to get existing workspaces: S3 bucket does not exist.
```

**Root Cause:** S3 bucket for Terraform state not created.

**Solution:**
```bash
export SCW_ACCESS_KEY="your-key"
export SCW_SECRET_KEY="your-secret"
export SCW_DEFAULT_PROJECT_ID="your-project-id"

cd infrastructure
bash scripts/setup-backend.sh
```

**Or manually:**
1. Go to https://console.scaleway.com/object-storage/buckets
2. Click "Create bucket"
3. Name: `clercq-it-terraform-state`
4. Region: `fr-par`
5. Click "Create"

---

### ❌ Error: "403 Forbidden" on state access

**Symptom:**
```
Error: Failed to save state: AccessDenied: 403 Forbidden
```

**Root Cause:** Credentials don't have Object Storage permissions.

**Solution:**
1. Verify credentials are set in GitHub Secrets:
   - `SCALEWAY_ACCESS_KEY`
   - `SCALEWAY_SECRET_KEY`
   - `SCALEWAY_PROJECT_ID`

2. Check IAM permissions in Scaleway:
   - Object Storage: Read/Write access
   - Container Registry: Read/Write access

3. Verify project ID matches the one used for the bucket

---

### ❌ Error: "max_connections must be superior to 50"

**Symptom:**
```
Error: max_connections does not respect constraint, max_connections must be superior to 50
```

**Root Cause:** PostgreSQL RDB instances require min 50 connections.

**Solution:** ✅ **FIXED** - `main.tf` now has `max_connections = "50"`

**If still happening:** Update `main.tf`:
```hcl
settings = {
  "max_connections" = "50"  # Must be >= 50
}
```

---

### ❌ Workflow fails on "Import Existing Resources"

**Symptom:**
```
Error: Import failed for scaleway_container_namespace.portfolio
```

**Root Cause:** Resource might not exist yet or API call failed.

**Solution:**
1. Check if namespace exists in Scaleway console
2. If exists, get the namespace ID:
   ```bash
   curl -H "X-Auth-Token: $SCW_SECRET_KEY" \
     "https://api.scaleway.com/containers/v1beta1/regions/fr-par/namespaces"
   ```
3. Manually import if needed:
   ```bash
   terraform import scaleway_container_namespace.portfolio <namespace-id>
   ```

**Note:** This step has `continue-on-error: true`, so it shouldn't block deployment.

---

### 🔍 How to Check Current State

**View all resources in state:**
```bash
cd infrastructure/terraform
terraform state list
```

**View specific resource:**
```bash
terraform state show scaleway_container_namespace.portfolio
```

**Check state file in S3:**
```bash
aws s3 ls s3://clercq-it-terraform-state/portfolio/ \
  --endpoint-url https://s3.fr-par.scw.cloud
```

---

### 🔧 How to Fix Corrupted State

**If state is corrupted or inconsistent:**

1. **Backup current state:**
   ```bash
   terraform state pull > backup-state.json
   ```

2. **Option A: Remove specific resource from state**
   ```bash
   terraform state rm scaleway_container_namespace.portfolio
   ```

3. **Option B: Start fresh (DANGEROUS - will lose all state)**
   ```bash
   # Backup first!
   aws s3 cp s3://clercq-it-terraform-state/portfolio/terraform.tfstate ./backup.tfstate
   
   # Delete state (use with extreme caution)
   aws s3 rm s3://clercq-it-terraform-state/portfolio/terraform.tfstate
   
   # Re-import all resources
   terraform import scaleway_container_namespace.portfolio <id>
   terraform import scaleway_rdb_instance.portfolio_db <id>
   # etc.
   ```

---

### 📊 Debugging Workflow

**Enable Terraform debug output:**

Add to workflow env:
```yaml
env:
  TF_LOG: DEBUG
```

**Check workflow logs:**
1. Go to Actions tab in GitHub
2. Click on failed workflow run
3. Expand "Terraform Apply" step
4. Look for error messages

**Common signs of issues:**
- ❌ "Error: Backend initialization required"
- ❌ "Error: 403 Forbidden"
- ❌ "Error: 409 Conflict"
- ⚠️ "Warning: deprecated attribute" (safe to ignore)

---

### ✅ Verification Checklist

After deployment, verify:

- [ ] S3 bucket `clercq-it-terraform-state` exists
- [ ] State file `portfolio/terraform.tfstate` exists in bucket
- [ ] Namespace "portfolio" exists in Scaleway console
- [ ] Container "clercq-it-app" exists and is running
- [ ] Database "portfolio-database" is accessible
- [ ] No 409 Conflict errors in workflow logs
- [ ] Deprecation warnings present but not blocking

---

### 📚 Related Documentation

- [NAMESPACE_DATA_SOURCE_FIX.md](./NAMESPACE_DATA_SOURCE_FIX.md) - Latest fix using data sources
- [BACKEND_STATE_FIX.md](./BACKEND_STATE_FIX.md) - Complete backend fix explanation
- [STATE_MANAGEMENT_FLOW.md](./STATE_MANAGEMENT_FLOW.md) - Visual flow diagrams
- [FIX_SUMMARY_BACKEND.md](./FIX_SUMMARY_BACKEND.md) - Quick summary
- [DEPLOYMENT_FIX.md](./DEPLOYMENT_FIX.md) - Previous deployment fixes
- [README.md](./README.md) - Infrastructure overview

---

### 🆘 Still Having Issues?

1. Check all GitHub Secrets are set correctly
2. Verify Scaleway credentials have proper permissions
3. Run the setup script manually: `bash scripts/setup-backend.sh`
4. Check Scaleway console for existing resources
5. Review workflow logs for specific error messages
6. Consult the detailed documentation files listed above

---

**Quick Command Reference:**

```bash
# Setup backend
bash infrastructure/scripts/setup-backend.sh

# Initialize Terraform
terraform init -backend-config="access_key=$SCW_ACCESS_KEY" \
               -backend-config="secret_key=$SCW_SECRET_KEY"

# Check state
terraform state list

# Plan changes
terraform plan

# Apply changes
terraform apply

# View outputs
terraform output
```
