# Cloud IAM (Hosted Keycloak) - Production Identity Provider

This document describes how to configure [Cloud IAM](https://www.cloud-iam.com) as the production identity provider for Clercq.It. Cloud IAM is a **hosted Keycloak** service, meaning it exposes the standard Keycloak OIDC and Admin REST APIs.

## Key Insight

Because Cloud IAM is standard Keycloak, the same `LocalKeycloakAuthService` used for local development works in production. No separate auth service implementation is needed - the only difference is the base URL.

## Cloud IAM Setup Guide

### 1. Create Account and Deploy Keycloak

1. Sign up at https://www.cloud-iam.com
2. Deploy a new Keycloak instance - choose provider and region
3. Note your deployment URL: `https://{deployment}.cloud-iam.com`
4. Access the Keycloak admin console at `https://{deployment}.cloud-iam.com/admin`

### 2. Create the `clercqit` Realm

Create a new realm named `clercqit` with the following settings (matching the local realm configuration):

- **Display Name**: Clercq.It
- **Registration Allowed**: true
- **Login with Email Allowed**: true
- **Remember Me**: true
- **Brute Force Protection**: enabled

Alternatively, import the local realm JSON (`src/Clercq.It.AppHost/KeycloakRealms/clercqit-realm.json`) directly via the admin console under **Realm Settings > Partial Import**, then adjust client redirect URIs for production.

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
- **Direct Access Grants**: Enabled
- **Valid Redirect URIs**: `https://www.clercq.it/*`
- **Web Origins**: `https://www.clercq.it`
- **Default Client Scopes**: `web-origins`, `acr`, `roles`, `profile`, `email`

#### `clercqit-admin` (Service Account)
- **Client ID**: `clercqit-admin`
- **Client Authentication**: On
- **Direct Access Grants**: Enabled
- **Service Accounts**: Enabled

### 4. Create Realm Roles

| Role | Description |
|------|-------------|
| `Admin.View` | Can view admin dashboard and settings |
| `Blogs.Contributor` | Can create, edit, and delete blog posts |
| `Projects.Contributor` | Can create, edit, and delete projects |
| `Certifications.Contributor` | Can create, edit, and delete certifications |

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

Under **Client Scopes > roles > Scope**, assign all four realm roles so they appear in tokens.

## Production Configuration

Set the following environment variables for the API container:

```bash
Keycloak__BaseUrl=https://{deployment}.cloud-iam.com
Keycloak__Realm=clercqit
Keycloak__ClientId=clercqit-web
Keycloak__ClientSecret={production-secret}
Keycloak__AdminClientId=clercqit-admin
Keycloak__AdminClientSecret={production-admin-secret}
```

When `Keycloak:BaseUrl` is set (and no Aspire service reference exists), the API uses `LocalKeycloakAuthService` which talks to the standard Keycloak OIDC/Admin endpoints - works identically for both local and Cloud IAM.

### Scaleway Container Deployment

```hcl
resource "scaleway_container" "app" {
  name = "clercq-it-app"

  environment_variables = {
    "Keycloak__BaseUrl" = "https://{deployment}.cloud-iam.com"
    "Keycloak__Realm"   = "clercqit"
    "Keycloak__ClientId" = "clercqit-web"
    "Keycloak__AdminClientId" = "clercqit-admin"
  }

  secret_environment_variables = {
    "Keycloak__ClientSecret"      = var.keycloak_client_secret
    "Keycloak__AdminClientSecret" = var.keycloak_admin_client_secret
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

## CloudIAMAuthService (Legacy)

The codebase contains a `CloudIAMAuthService` that was built for a custom (non-Keycloak) identity API. This service is **not used** when connecting to Cloud IAM (cloud-iam.com), because Cloud IAM is standard Keycloak.

- **Cloud IAM (cloud-iam.com)**: Uses `LocalKeycloakAuthService` (standard Keycloak API)
- **Custom identity provider**: Would use `CloudIAMAuthService` (custom REST endpoints like `/v1/auth/login`)

If you are only using Keycloak (local or Cloud IAM), `CloudIAMAuthService` can be considered legacy code.

## Security Notes

- Use separate client secrets for production - never reuse local development secrets
- HTTPS is enforced for external Keycloak (`RequireHttpsMetadata = true` when `isLocalKeycloak = false`)
- Rotate client secrets periodically
- Cloud IAM provides ISO 27001 and SOC 2 Type 2 compliance
- Configure admin allow lists to restrict Keycloak admin console access by IP
- Enable MFA for admin accounts

## Related Documentation

- [Local Development Guide](./development.md) - Local Keycloak setup with test users
- [Admin Backoffice Documentation](./admin-backoffice.md) - Admin panel usage
- [DevOps Guide](./devops.md) - Deployment and CI/CD configuration
