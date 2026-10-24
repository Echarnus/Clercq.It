# Cloud IAM (Hosted Keycloak) - Production Identity Provider

This document describes how to configure [Cloud IAM](https://www.cloud-iam.com) as the production identity provider for Clercq.It. Cloud IAM is a **hosted Keycloak** service, meaning it exposes the standard Keycloak OIDC and Admin REST APIs.

## Key Insight

Because Cloud IAM is standard Keycloak, local development (the Aspire Keycloak container) and production use the same code; the only difference is the base URL.

The admin UI signs in with the **authorization code flow and PKCE** (issue #257): `/admin` sends the browser to Keycloak's login page, Keycloak handles the password, MFA and brute-force protection, and redirects back to `/admin/callback` with a code that the browser exchanges for tokens. The API only validates the access tokens; it never sees a password. The frontend reads the issuer and client id from `GET /api/auth/config`.

## Current Deployment

| Setting | Value |
|---|---|
| Base URL (`keycloak-base-url` secret) | `https://lemur-16.cloud-iam.com/auth` |
| Realm | `clercqit` |
| Admin console | `https://lemur-16.cloud-iam.com/auth/admin/clercqit/console/` |
| Plan | Free (Freemium): 100 users, 1 realm, shared instance, no SLA |
| Location | Scaleway `nl-ams`, Keycloak 26.8 |
| Created | 22 October 2026 (issue #343). The previous deployment on `lemur-6` no longer exists. |

On the free plan the deployment name is also the realm name, and Cloud IAM creates it with one realm admin user (`admin`). Its first-login password arrives in the "deployment has been completed" email. Once a personal account with the `realm-management` → `realm-admin` role has signed in, disable `admin` under **Users**.

To get the free plan in **Create deployment**, move the user slider to **100**: the support level, instance type and price switch to Freemium, Mutualized and 0 €.

## Cloud IAM Setup Guide

### 1. Create Account and Deploy Keycloak

1. Sign up at https://www.cloud-iam.com
2. Deploy a new Keycloak instance - choose provider and region
3. Note your deployment URL: `https://{deployment}.cloud-iam.com`
4. Access the Keycloak admin console at `https://{deployment}.cloud-iam.com/admin`

### 2. Create the `clercqit` Realm

Create a new realm named `clercqit` with the following settings (matching the local realm configuration):

- **Display Name**: Clercq.It
- **Registration Allowed**: false (admin users are created by hand, see step 7)
- **Login with Email Allowed**: true
- **Remember Me**: true
- **Brute Force Protection**: enabled

Alternatively, import the local realm JSON (`src/Clercq.It.AppHost/KeycloakRealms/clercqit-realm.json`) directly via the admin console under **Realm Settings > Action > Partial import**, then adjust client redirect URIs for production. This is how the current deployment was built: a copy of that file without users and client secrets, with the production and verify redirect URIs, imported as 5 clients and 4 realm roles. Partial import does not cover client scopes, so step 5 is done by hand: a new realm already has a built-in `roles` scope, and its **realm roles** mapper was changed to token claim name `roles` with ID token, access token and userinfo on.

### 3. Create Clients

#### `clercqit-api` (Bearer-Only)
- **Client ID**: `clercqit-api`
- **Client Authentication**: On
- **Bearer Only**: Yes
- **Direct Access Grants**: Enabled
- **Service Accounts**: Enabled

#### `clercqit-web` (Public, Standard Flow)
- **Client ID**: `clercqit-web`
- **Client Authentication**: Off (public client)
- **Standard Flow**: Enabled
- **Direct Access Grants**: **Disabled** (no password grant; issue #257)
- **Advanced > Proof Key for Code Exchange Code Challenge Method**: `S256`
- **Valid Redirect URIs**: `https://www.clercq.it/admin/callback`
- **Valid Post Logout Redirect URIs**: `https://www.clercq.it/admin`
- **Web Origins**: `https://www.clercq.it` (lets the browser call the token endpoint)
- **Default Client Scopes**: `web-origins`, `acr`, `roles`, `profile`, `email`

#### MCP clients

The content MCP server (`/api/mcp`, see [MCP](./mcp.md)) accepts any realm token with a contributor role. Two clients give Claude such a token. They are not in the deployment yet; the owner creates them in the Cloud IAM console.

`clercqit-mcp` (public, standard flow) is for the Claude custom connector: you sign in once in the browser and Claude acts as you.
- **Client ID**: `clercqit-mcp`
- **Client Authentication**: Off (public client)
- **Standard Flow**: Enabled; **Direct Access Grants**: Disabled
- **Advanced > Proof Key for Code Exchange Code Challenge Method**: `S256`
- **Valid Redirect URIs**: `https://claude.ai/api/mcp/auth_callback`, `https://claude.com/api/mcp/auth_callback`
- **Default Client Scopes**: `web-origins`, `acr`, `roles`, `profile`, `email`

`clercqit-mcp-agent` (confidential, service account) is optional, for agents that run without a browser (client credentials grant). Only create it if the owner decides agents may write content unattended.
- **Client ID**: `clercqit-mcp-agent`
- **Client Authentication**: On; **Service Accounts**: Enabled; **Standard Flow** and **Direct Access Grants**: Disabled
- **Service account roles**: `Projects.Contributor`, `Certifications.Contributor`, `Blogs.Contributor`
- **Default Client Scopes**: `roles` (so the token carries the `roles` claim)
- Its client secret goes in Scaleway Secret Manager, never in an issue or chat.

### 4. Create Realm Roles

| Role | Description |
|------|-------------|
| `Admin.View` | Can view admin dashboard and settings |
| `Blogs.Contributor` | Can create, edit, and delete blog posts |
| `Projects.Contributor` | Can create, edit, and delete projects |
| `Certifications.Contributor` | Can create, edit, and delete certifications |
| `MyZen.Owner` | Can use the owner's MyZen account through `/api/myzen` ([MyZen](./myzen.md)). Give it only to the owner. |

The API enforces these per endpoint (issue #244): creating, updating or deleting blogs and uploading images needs `Blogs.Contributor`, projects need `Projects.Contributor`, certifications need `Certifications.Contributor`, and every `/api/myzen` endpoint needs `MyZen.Owner` (reads too). A signed-in user without the role gets 403. Reads are public. The roles must arrive in the access token's `roles` claim (step 5); the API reads claim names as Keycloak sends them (`MapInboundClaims = false`).

### 5. Create the `roles` Client Scope

1. Go to **Client Scopes > Create**
2. **Name**: `roles`
3. **Protocol**: OpenID Connect
4. **Include in Token Scope**: true
5. Add protocol mapper: **Realm Roles**
   - **Mapper Type**: User Realm Role
   - **Token Claim Name**: `roles`
   - **Add to ID token**: true
   - **Add to access token**: true
   - **Add to userinfo**: true
   - **Multivalued**: true

### 6. Configure Scope Mappings

Under **Client Scopes > roles > Scope**, assign all five realm roles so they appear in tokens.

### 7. Create Admin Users

Registration is closed, so every account is created by the owner:

1. Open the Keycloak admin console (`https://{deployment}.cloud-iam.com/admin`) and pick the `clercqit` realm.
2. **Users > Add user**: set username and email, tick **Email verified**, and save.
3. **Credentials > Set password**: set a strong password with **Temporary** off.
4. **Role mapping > Assign role**: assign `Admin.View`, `Blogs.Contributor`, `Projects.Contributor` and `Certifications.Contributor`, and for the owner also `MyZen.Owner`.
5. Recommended: **Required user actions > Configure OTP**, so the account uses MFA from its first login.
6. Sign in at `https://www.clercq.it/admin` to check the account.

## Production Configuration

Set the following environment variables for the API container:

```bash
Keycloak__BaseUrl=https://{deployment}.cloud-iam.com
Keycloak__Realm=clercqit
Keycloak__ClientId=clercqit-web
```

When `Keycloak:BaseUrl` is set (and no Aspire service reference exists), the API validates tokens against `{BaseUrl}/realms/{Realm}` and returns that issuer and `Keycloak:ClientId` from `GET /api/auth/config`. `clercqit-web` is a public client, so the site needs no client secret. The site's old `Keycloak:ClientSecret`, `Keycloak:AdminClientId` and `Keycloak:AdminClientSecret` settings, the local `clercqit-admin` client and the verify secrets were removed after the password grant went away (#257, #346). If the Cloud IAM realm still has a `clercqit-admin-verify` client, delete it under **Clients**; nothing uses it. The Cloud IAM `clercqit-admin` client stays for now, because the separate `administration-api` container in `infra/terraform/main.tf` is still configured with it.

### Scaleway Container Deployment

```hcl
resource "scaleway_container" "app" {
  name = "clercq-it-app"

  environment_variables = {
    "Keycloak__BaseUrl" = "https://{deployment}.cloud-iam.com"
    "Keycloak__Realm"   = "clercqit"
    "Keycloak__ClientId" = "clercqit-web"
  }
}
```

## Terraform Automation (Optional)

Cloud IAM supports the official [Keycloak Terraform Provider](https://registry.terraform.io/providers/mrparkers/keycloak/latest) for infrastructure-as-code:

```hcl
provider "keycloak" {
  client_id = "admin-cli"
  username  = "admin"
  password  = var.keycloak_admin_password
  url       = "https://{deployment}.cloud-iam.com"
}

resource "keycloak_realm" "clercqit" {
  realm   = "clercqit"
  enabled = true
}

resource "keycloak_role" "admin_view" {
  realm_id = keycloak_realm.clercqit.id
  name     = "Admin.View"
}

# ... additional roles, clients, scopes
```

**Note**: Add the Terraform runner's IP address to the Cloud IAM admin allow list in your deployment settings.

## Configuration Check

The API needs `Keycloak:BaseUrl` (and optionally `Keycloak:Realm`, default `clercqit`). If neither that nor an Aspire Keycloak reference is set, the API refuses to start with `Authentication is not configured`. There is no other auth provider to fall back to.

## Security Notes

- Production clients are public PKCE clients without secrets; never import the local realm's `clercqit-api` secret or `clercqit-tests` client into production
- HTTPS is enforced for external Keycloak (`RequireHttpsMetadata = true` when `isLocalKeycloak = false`)
- Cloud IAM provides ISO 27001 and SOC 2 Type 2 compliance
- Configure admin allow lists to restrict Keycloak admin console access by IP
- Enable MFA for admin accounts

## Related Documentation

- [Local Development Guide](./development.md) - Local Keycloak setup with test users
- [DevOps Guide](./devops.md) - Deployment and CI/CD configuration
