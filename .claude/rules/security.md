# Security rules

Full policy: docs/application-security.md and docs/secrets.md.

- No secrets in code, config, tests, issues or PRs. App and infra secrets live in Scaleway Secret Manager (Terraform, `infra/terraform/secrets.tf`); CI secrets in GitHub secrets. Never ask anyone to paste a secret.
- Semgrep (ERROR findings), gitleaks, the CodeQL gate and the NuGet vulnerability check block a PR. Run them locally as shown in docs/application-security.md#run-locally.
- Do not disable a security rule to get green. A suppression (`// nosemgrep: <rule>`, `.semgrepignore`, `.gitleaksignore`, `.trivyignore`) needs a reason on the same line or right above it, and a real false positive.
- Validate input at the boundary (FluentValidation in Application, zod in the frontend). Admin endpoints need an authorization policy and tests for 401 and 403 (docs/testing.md).
- Anything that needs a console login, an account, credentials or money is for the owner: label the issue `status:needs-human` and say exactly what to do.
