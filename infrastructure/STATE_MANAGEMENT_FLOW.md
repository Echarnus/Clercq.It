# Terraform State Management Flow

## Before Fix (Broken) 🔴

```
┌─────────────────────────────────────────────────────────┐
│  GitHub Actions Workflow Run #1                         │
├─────────────────────────────────────────────────────────┤
│  1. Checkout code                                        │
│  2. Terraform Init (local state in /tmp)                │
│  3. Empty state → tries to create namespace             │
│  4. Creates namespace "portfolio" ✅                     │
│  5. State stored locally in runner                      │
│  6. Runner terminates → STATE LOST ❌                   │
└─────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────┐
│  GitHub Actions Workflow Run #2 (e.g., new push)        │
├─────────────────────────────────────────────────────────┤
│  1. Checkout code                                        │
│  2. Terraform Init (fresh runner, empty state)          │
│  3. Empty state → thinks namespace doesn't exist        │
│  4. Tries to create namespace "portfolio"               │
│  5. ERROR: 409 Conflict - Namespace already exists! 🔴  │
│  6. Deployment fails ❌                                  │
└─────────────────────────────────────────────────────────┘
```

**Problem**: State doesn't persist between workflow runs!

---

## After Fix (Working) ✅

```
┌─────────────────────────────────────────────────────────┐
│  GitHub Actions Workflow Run #1                         │
├─────────────────────────────────────────────────────────┤
│  1. Checkout code                                        │
│  2. Setup Backend Bucket (creates S3 bucket)            │
│  3. Terraform Init with backend config                  │
│     └─> Connects to S3: clercq-it-terraform-state      │
│  4. Import existing namespace if found                  │
│  5. Terraform Apply                                      │
│     └─> Creates/updates resources                       │
│  6. State saved to S3 bucket ✅                         │
│  7. Runner terminates (state is safe in S3) ✅          │
└─────────────────────────────────────────────────────────┘
                           ↓
                ┌──────────────────┐
                │   S3 Bucket      │
                │ terraform.tfstate│
                │   [PERSISTED]    │
                └──────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────┐
│  GitHub Actions Workflow Run #2 (e.g., new push)        │
├─────────────────────────────────────────────────────────┤
│  1. Checkout code                                        │
│  2. Setup Backend Bucket (bucket exists, skip)          │
│  3. Terraform Init with backend config                  │
│     └─> Loads state from S3 ✅                          │
│  4. State contains namespace → knows it exists!         │
│  5. Terraform Apply                                      │
│     └─> Updates only what changed (no recreate)         │
│  6. State updated in S3 bucket ✅                       │
│  7. No conflicts! Deployment succeeds ✅                │
└─────────────────────────────────────────────────────────┘
```

**Solution**: State persists in S3 between runs!

---

## State Persistence Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    GitHub Actions                        │
│  ┌───────────────────────────────────────────┐          │
│  │  Workflow (infra.yml)                     │          │
│  │  ┌─────────────────────────────────────┐  │          │
│  │  │ 1. Setup Backend Bucket             │  │          │
│  │  │    - Check if bucket exists         │  │          │
│  │  │    - Create if needed               │  │          │
│  │  └─────────────────────────────────────┘  │          │
│  │                  ↓                         │          │
│  │  ┌─────────────────────────────────────┐  │          │
│  │  │ 2. Terraform Init                   │  │          │
│  │  │    - Connect to S3 backend          │◄─┼─────┐    │
│  │  │    - Load existing state            │  │     │    │
│  │  └─────────────────────────────────────┘  │     │    │
│  │                  ↓                         │     │    │
│  │  ┌─────────────────────────────────────┐  │     │    │
│  │  │ 3. Import Resources (if needed)     │  │     │    │
│  │  │    - Check Scaleway for existing    │  │     │    │
│  │  │    - Import into state if found     │  │     │    │
│  │  └─────────────────────────────────────┘  │     │    │
│  │                  ↓                         │     │    │
│  │  ┌─────────────────────────────────────┐  │     │    │
│  │  │ 4. Terraform Apply                  │  │     │    │
│  │  │    - Create/update resources        │  │     │    │
│  │  │    - Save state back to S3          │◄─┼─────┘    │
│  │  └─────────────────────────────────────┘  │          │
│  └───────────────────────────────────────────┘          │
└─────────────────────────────────────────────────────────┘
                           │
                           │ State stored/retrieved via S3 API
                           ↓
┌─────────────────────────────────────────────────────────┐
│              Scaleway Object Storage (S3)                │
│  ┌───────────────────────────────────────────┐          │
│  │  Bucket: clercq-it-terraform-state        │          │
│  │  ┌─────────────────────────────────────┐  │          │
│  │  │ Key: portfolio/terraform.tfstate    │  │          │
│  │  │                                     │  │          │
│  │  │ {                                   │  │          │
│  │  │   "version": 4,                     │  │          │
│  │  │   "resources": [                    │  │          │
│  │  │     {                               │  │          │
│  │  │       "type": "scaleway_container   │  │          │
│  │  │               _namespace",          │  │          │
│  │  │       "name": "portfolio",          │  │          │
│  │  │       "instances": [...]            │  │          │
│  │  │     }                               │  │          │
│  │  │   ]                                 │  │          │
│  │  │ }                                   │  │          │
│  │  └─────────────────────────────────────┘  │          │
│  └───────────────────────────────────────────┘          │
└─────────────────────────────────────────────────────────┘
```

---

## Key Benefits

| Aspect | Before (Broken) | After (Fixed) |
|--------|----------------|---------------|
| **State Storage** | Local (runner /tmp) | Remote (S3 bucket) |
| **State Persistence** | ❌ Lost after run | ✅ Persists forever |
| **Resource Tracking** | ❌ Forgets resources | ✅ Tracks all resources |
| **409 Conflicts** | 🔴 Every deployment | ✅ Never happens |
| **Team Collaboration** | ❌ Conflicts | ✅ Shared state |
| **Cost** | Free | ~€0.01/month |
| **Best Practice** | ❌ Anti-pattern | ✅ Recommended |

---

## Quick Reference

### State Backend Configuration
```hcl
# infrastructure/terraform/main.tf
backend "s3" {
  bucket   = "clercq-it-terraform-state"
  key      = "portfolio/terraform.tfstate"
  region   = "fr-par"
  endpoint = "https://s3.fr-par.scw.cloud"
}
```

### Workflow Changes
```yaml
# .github/workflows/infra.yml
- name: Setup Backend Bucket
  run: bash scripts/setup-backend.sh

- name: Terraform Init
  run: |
    terraform init \
      -backend-config="access_key=$SCW_ACCESS_KEY" \
      -backend-config="secret_key=$SCW_SECRET_KEY"
```

### State Location
- **Bucket**: `clercq-it-terraform-state`
- **Key**: `portfolio/terraform.tfstate`
- **Region**: `fr-par` (Paris)
- **Endpoint**: `https://s3.fr-par.scw.cloud`

---

**Result**: Infrastructure deployments now work reliably! ✅
