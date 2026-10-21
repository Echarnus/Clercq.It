---
paths:
  - "infra/**"
---

# Infrastructure rules

Full policy: docs/devops.md and docs/secrets.md.

- Run `terraform fmt -check` and `terraform validate` in `infra/terraform` (`terraform init -backend=false` first in a fresh checkout).
- Every resource goes through Terraform; no console changes. Secrets are `scaleway_secret` resources, and secret values never appear in `.tf` files or `terraform.tfvars.example`. Mark sensitive variables `sensitive = true`.
- The `Deploy Infra` workflow applies on merge to `main` and posts the plan on the PR, so a plan that destroys or replaces production resources needs a sentence in the PR body on why that is safe.
