# Secrets

All application secrets live in **Scaleway Secret Manager**, in the same Scaleway project as the site, under the path `/clercq-it`. Terraform (`infra/terraform/secrets.tf`) creates them and reads them back; nothing secret is set as a plain container environment variable. Cost is about €0.04 per stored version per month (decided 21 October 2026).

The owner keeps personal logins (Scaleway, Cloud IAM, OVHcloud, Docker Hub) in a password manager; those are not in Secret Manager.

## Secrets

| Name (path `/clercq-it`) | Used by | First value comes from |
| --- | --- | --- |
| `db-app-password` | RDB admin and `clercqit_user` passwords, web/API container connection string | `TF_VAR_database_password` (GitHub `DATABASE_PASSWORD`), else generated |
| `keycloak-base-url` | `Keycloak__BaseUrl` on both containers (current value: `https://lemur-16.cloud-iam.com/auth`, see [ciam.md](./ciam.md#current-deployment)) | `TF_VAR_keycloak_base_url`, else a placeholder |
| `myzen-session-key` | `MyZen__SessionKey` on the web/API container; encrypts the stored MyZen session ([MyZen](./myzen.md)) | generated |

The site signs admins in with the public PKCE client `clercqit-web`, so no container gets a Keycloak secret. The old `keycloak-web-client-secret` (#346), `keycloak-admin-client-secret` and `db-administration-password` (#387) were removed with the clients and the unused `administration-api` container that read them. The GitHub secrets `KEYCLOAK_WEB_CLIENT_SECRET`, `KEYCLOAK_ADMIN_CLIENT_SECRET` and `ADMINISTRATION_DATABASE_PASSWORD` are no longer read and can be deleted.

One secret is created by the owner, not Terraform, and only read by it:

| Name | Used by | Value |
| --- | --- | --- |
| `myzen-login` (type Username/password) | `MyZen__Email` and `MyZen__Password` on the web/API container ([MyZen](./myzen.md)) | The owner's MyZen login, set in the console. Terraform reads the latest enabled version; to change it, add a new version and run **Deploy Infra**. |

### Verify environment (path `/clercq-it/verify`)

The verify stage (see [DevOps](./devops.md#verify-stage)) has its own secrets, created by `infra/terraform/verify.tf`. It never reads a production secret.

| Name (path `/clercq-it/verify`) | Used by | First value comes from |
| --- | --- | --- |
| `db-password` | `clercqit_verify_user` password, verify connection string, verify migrations in `deploy.yml` | generated |

`db-password` is fully Terraform-owned: `random_password.db_verify` writes it as a secret version without `ignore_changes`, so do not edit it in the console (the next apply overwrites it). Scaleway RDB requires at least one digit, uppercase, lowercase and special character; the special characters are limited to `!#%*-_+.` so the value stays safe unquoted in an Npgsql connection string and in `PGPASSWORD`. Verify has no Keycloak secret: its client `clercqit-web-verify` is public, like `clercqit-web`.

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
| Claude cloud sessions | **List only** (names, paths, versions; no values), this project only | IAM application `clercq-it-claude-agent` with `SecretManagerReadOnly` |
| Running containers | Values injected at deploy time | None of their own |

### Setting up Claude's read-only key (owner, once)

The IAM application and its policy are created by Terraform. The key itself is created by hand so its secret never lands in Terraform state:

1. Scaleway console → IAM → Applications → `clercq-it-claude-agent` → API keys → Generate API key. Set the preferred project to the Clercq.It project.
2. In Claude, open the project's settings → Cloud environment → add (or edit) the environment, and add network secrets `SCW_ACCESS_KEY`, `SCW_SECRET_KEY` and `SCW_DEFAULT_PROJECT_ID` with the values from step 1. Never paste them in a chat, issue or PR.
3. Allow network access to `api.scaleway.com` in the same environment.

A session can then see which secrets and versions exist with the Scaleway CLI that the session-start hook installs:

```bash
scw secret secret list path=/clercq-it
scw secret version list <secret-id>
```

It cannot read a value (`scw secret version access` is refused). That is on purpose (issue #415): no agent task needs a secret value, and the values include the owner's MyZen login. If a task ever does, the owner reads the value and puts it where it is needed.

## State

Seeded and generated values are stored in the Terraform state in the `clercq-it-terraform-state` bucket. That bucket must stay private, and should have versioning enabled.

### Who can reach which bucket

Scaleway IAM grants object storage per project, so on its own every storage key could read every bucket, including the state. Bucket policies in `infra/terraform/storage_policies.tf` close that (issue #415). With the `2023-04-17` policy version a bucket allows only the principals it lists:

| Bucket | Allowed |
| --- | --- |
| `clercq-it-terraform-state` | Terraform only |
| `clercq-it-blog-images` | Terraform and the deploy pipeline (all actions), the site container's key (`clercq-it-app-storage`: list, read, write, delete objects and set their ACL), everyone (read objects) |
| `clercq-it-verify-images` | Terraform (all actions), the verify key (`clercq-it-verify-storage`: same object actions), everyone (read objects) |

The container keys' IAM policies are also reduced from `ObjectStorageFullAccess` to the objects-only sets (`ObjectStorageObjectsRead`, `ObjectStorageObjectsWrite`, `ObjectStorageObjectsDelete`), so they cannot change bucket settings or delete buckets.

"Terraform" is whoever owns the API key in the `SCALEWAY_ACCESS_KEY` repository secret. The infra workflow passes that access key (not the secret key) as `TF_VAR_terraform_access_key`, and Terraform looks up its user or application. If you rotate that key to one owned by another user or application, run **Deploy Infra** right away so the policies follow. If a policy ever locks Terraform out, the Organization owner can delete it in the console (Object Storage → bucket → Bucket policy) and rerun the pipeline.
