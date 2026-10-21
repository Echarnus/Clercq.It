# Secrets

All application secrets live in **Scaleway Secret Manager**, in the same Scaleway project as the site, under the path `/clercq-it`. Terraform (`infra/terraform/secrets.tf`) creates them and reads them back; nothing secret is set as a plain container environment variable. Cost is about €0.04 per stored version per month (decided 21 October 2026).

The owner keeps personal logins (Scaleway, Cloud IAM, OVHcloud, Docker Hub) in a password manager; those are not in Secret Manager.

## Secrets

| Name (path `/clercq-it`) | Used by | First value comes from |
| --- | --- | --- |
| `db-app-password` | RDB admin and `clercqit_user` passwords, web/API container connection string | `TF_VAR_database_password` (GitHub `DATABASE_PASSWORD`), else generated |
| `db-administration-password` | `admin_app_user` password, administration API connection string | `TF_VAR_administration_database_password`, else generated |
| `keycloak-base-url` | `Keycloak__BaseUrl` on both containers | `TF_VAR_keycloak_base_url`, else a placeholder |
| `keycloak-web-client-secret` | `Keycloak__ClientSecret` on the web/API container | `TF_VAR_keycloak_web_client_secret`, else a placeholder |
| `keycloak-admin-client-secret` | `Keycloak__AdminClientSecret` on both containers | `TF_VAR_keycloak_admin_client_secret`, else a placeholder |

### Verify environment (path `/clercq-it/verify`)

The verify stage (see [DevOps](./devops.md#verify-stage)) has its own secrets, created by `infra/terraform/verify.tf`. It never reads a production secret.

| Name (path `/clercq-it/verify`) | Used by | First value comes from |
| --- | --- | --- |
| `db-password` | `clercqit_verify_user` password, verify connection string, verify migrations in `deploy.yml` | generated |
| `keycloak-web-client-secret` | `Keycloak__ClientSecret` on `clercq-it-verify` (client `clercqit-web-verify`) | placeholder, owner replaces it |
| `keycloak-admin-client-secret` | `Keycloak__AdminClientSecret` on `clercq-it-verify` (client `clercqit-admin-verify`) | placeholder, owner replaces it |

`db-password` is fully Terraform-owned: `random_password.db_verify` writes it as a secret version without `ignore_changes`, so do not edit it in the console (the next apply overwrites it). Scaleway RDB requires at least one digit, uppercase, lowercase and special character; the special characters are limited to `!#%*-_+.` so the value stays safe unquoted in an Npgsql connection string and in `PGPASSWORD`. The Keycloak secrets are only seeded once and then ignored by Terraform.

The verify container also has its own object storage key (IAM application `clercq-it-verify-storage`) and Cockpit token (`verify-container-logs`).

Other credentials Terraform manages itself, without a Secret Manager entry:

- **Object storage key** for the container: IAM application `clercq-it-app-storage` with `ObjectStorageFullAccess` on this project, and an API key created by Terraform (`iam.tf`). This replaces the account-wide key that used to be passed in.
- **Cockpit token** for logs and metrics (`scaleway_cockpit_token`).

All of these reach the containers as `secret_environment_variables`, which the Scaleway console does not display.

## How a value is owned

1. On the first apply, Terraform creates the secret and one version seeded from the `TF_VAR_*` value (or a generated password or placeholder).
2. From then on Terraform never overwrites the value (`ignore_changes`). It reads the **latest enabled version** and passes it on.
3. Seed variables can be removed from GitHub once the secret exists; Terraform no longer needs them.

## Rotating a value

1. Scaleway console → Secret Manager → `/clercq-it/<name>` → add a new version.
2. For database passwords, the infra pipeline updates the database user and the containers in the same apply.
3. Run **Deploy Infra** (Actions → Deploy Infra → Run workflow), or wait for the next deploy.
4. Disable or delete the old version, so stored versions (and cost) stay at one per secret.

A placeholder value (`replace-me`) means the secret was created without a seed; set the real value the same way.

## Access

| Who | Access | Credential |
| --- | --- | --- |
| Owner | Full, in the Scaleway console | Personal Scaleway login |
| GitHub Actions (`infra.yml`, `deploy.yml`) | Manages secrets and infra | `SCALEWAY_ACCESS_KEY` / `SCALEWAY_SECRET_KEY` repository secrets (needs IAM manager rights) |
| Claude cloud sessions | **Read-only**, this project only | IAM application `clercq-it-claude-agent` with `SecretManagerReadOnly` + `SecretManagerSecretAccess` |
| Running containers | Values injected at deploy time | None of their own |

### Setting up Claude's read-only key (owner, once)

The IAM application and its policy are created by Terraform. The key itself is created by hand so its secret never lands in Terraform state:

1. Scaleway console → IAM → Applications → `clercq-it-claude-agent` → API keys → Generate API key. Set the preferred project to the Clercq.It project.
2. In Claude, open the project's settings → Cloud environment → add (or edit) the environment, and add network secrets `SCW_ACCESS_KEY`, `SCW_SECRET_KEY` and `SCW_DEFAULT_PROJECT_ID` with the values from step 1. Never paste them in a chat, issue or PR.
3. Allow network access to `api.scaleway.com` in the same environment.

A session can then read a secret with the Scaleway CLI that the session-start hook installs:

```bash
scw secret secret list path=/clercq-it
scw secret version access <secret-id> revision=latest_enabled
```

## State

Seeded and generated values are stored in the Terraform state in the `clercq-it-terraform-state` bucket. That bucket must stay private, and should have versioning enabled.
