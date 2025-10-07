# Customer Identity and Access Management (CIAM)

This document describes the CIAM implementation using Auth0 Identity as a Service for the Clercq.It application.

## Overview

Clercq.It uses **Auth0** as an external Identity as a Service (IDaaS) provider to handle all authentication and user management concerns. This approach offloads the complexity of secure authentication, MFA, OAuth integration, and user lifecycle management to a dedicated service.

## Auth0 Architecture

### Authentication Flow

#### Username/Password Authentication

1. **User Submission**: User enters username and password in the frontend login form (`/admin`)
2. **API Request**: Frontend sends credentials to backend endpoint `POST /api/auth/login`
3. **Auth0 Validation**: Backend forwards credentials to Auth0 API for validation
4. **MFA Detection**: 
   - If user has MFA enabled, Auth0 returns HTTP 428 (Precondition Required)
   - Backend returns this status to frontend
   - Frontend dynamically displays TOTP input field
5. **MFA Validation** (if required):
   - User enters 6-digit TOTP code from authenticator app
   - Frontend resubmits with username, password, and TOTP
   - Backend validates complete credentials with Auth0
6. **Role Retrieval**: Upon successful authentication, backend retrieves user roles from Auth0
7. **JWT Generation**: Backend generates a JWT token containing:
   - User ID and username from Auth0
   - Assigned roles (e.g., `Admin.View`, `Blogs.Contributor`, `Projects.Contributor`, `Certifications.Contributor`)
   - Standard claims (issuer, audience, expiration)
8. **Token Response**: JWT returned to frontend and stored in browser localStorage
9. **Subsequent Requests**: Frontend includes JWT in `Authorization: Bearer <token>` header for protected endpoints

#### OAuth Authentication (GitHub/LinkedIn)

1. **OAuth Initiation**: User clicks "Sign in with GitHub" or "Sign in with LinkedIn"
2. **Backend Redirect**: Frontend navigates to backend endpoint `GET /api/auth/{provider}`
3. **Auth0 OAuth Flow**: 
   - Backend redirects to Auth0 OAuth endpoint
   - Auth0 handles provider-specific OAuth flow
4. **User Authorization**: User authorizes the application via GitHub/LinkedIn
5. **Callback Handling**: 
   - Provider redirects to Auth0
   - Auth0 validates OAuth response
   - Auth0 redirects to backend callback `GET /api/auth/{provider}/callback`
6. **User Creation** (first-time OAuth users):
   - Auth0 automatically creates user account
   - No roles assigned by default
7. **Role Retrieval**: Backend fetches user roles from Auth0 API
8. **JWT Generation**: Backend creates JWT with user data and roles
9. **Frontend Redirect**: Backend redirects to frontend dashboard with JWT in URL or cookie
10. **Dashboard Access**: Frontend extracts JWT and stores it for subsequent requests

#### User Registration Flow

1. **Registration Form**: User fills form at `/admin/register` (username, email, password)
2. **Account Creation**: Frontend sends registration data to `POST /api/auth/register`
3. **Auth0 User Creation**: Backend creates user in Auth0 with no roles assigned
4. **Email Verification**: Auth0 automatically sends verification email to user
5. **Email Confirmation**: User clicks verification link in email
6. **Account Activation**: Auth0 marks account as verified
7. **Role Assignment**: Administrator manually assigns roles via Auth0 dashboard
8. **Login Access**: User can now log in and access features based on assigned roles

### Role-Based Authorization

Once authenticated, the JWT token contains role claims that control access:

- **Frontend**: Shows/hides dashboard tabs based on roles in JWT
- **Backend**: Validates JWT and checks role claims before processing requests
- **API Endpoints**: Protected with `[Authorize(Roles = "RoleName")]` attributes

Users without any roles can authenticate but will see a "No Access" message in the admin panel.

## Container Configuration

### Environment Variables

The application container must be configured with the following Auth0-related environment variables:

#### Required Configuration

```bash
# Auth0 API Configuration
Auth0__Domain=<your-tenant>.auth0.com
Auth0__ClientId=<your-client-id>
Auth0__ClientSecret=<your-client-secret>
Auth0__Audience=<your-api-identifier>
Auth0__ClientRedirectUrl=<frontend-url>
```

#### Configuration Details

**Auth0__Domain**
- Your Auth0 tenant domain
- Format: `your-tenant.auth0.com` or `your-tenant.us.auth0.com`
- Used for all authentication and user management operations

**Auth0__ClientId**
- Application client ID from Auth0 dashboard
- Obtained from Auth0 Application settings
- Required for all Auth0 API calls

**Auth0__ClientSecret**
- Application client secret from Auth0 dashboard
- Obtained from Auth0 Application settings
- **Critical**: Keep this secret and never commit to source control

**Auth0__Audience**
- API identifier for your Auth0 API
- Used to validate JWT tokens
- Obtained from Auth0 API settings

**Auth0__ClientRedirectUrl**
- Frontend URL for OAuth redirects
- Development: `http://localhost:3000`
- Production: `https://www.clercq.it`
- Must match OAuth redirect URLs configured in Auth0

### Dockerfile Configuration

The containerized application automatically reads environment variables. No code changes needed for deployment.

Example Docker run command:

```bash
docker run -d \
  -p 80:80 \
  -e Auth0__Domain=<your-tenant>.auth0.com \
  -e Auth0__ClientId=<your-client-id> \
  -e Auth0__ClientSecret=<your-client-secret> \
  -e Auth0__Audience=<your-api-identifier> \
  -e Auth0__ClientRedirectUrl=https://www.clercq.it \
  echarnus/clercq-it:latest
```

### Scaleway Container Configuration

For Scaleway Serverless Containers, environment variables are configured via Terraform or the Scaleway console:

#### Via Terraform

```hcl
resource "scaleway_container" "app" {
  name = "clercq-it-app"
  # ... other configuration ...
  
  environment_variables = {
    "Auth0__Domain" = var.auth0_domain
    "Auth0__ClientId" = var.auth0_client_id
    "Auth0__Audience" = var.auth0_audience
    "Auth0__ClientRedirectUrl" = var.client_redirect_url
  }
  
  secret_environment_variables = {
    "Auth0__ClientSecret" = var.auth0_client_secret
  }
}
```

#### Via Scaleway Console

1. Navigate to Serverless Containers
2. Select the container
3. Go to "Environment Variables" tab
4. Add variables as shown above
5. Use "Secret Variables" for sensitive values (API keys, JWT secret)

### Next.js Frontend Configuration

The Next.js frontend requires the API URL to communicate with the backend:

```env
# .env.local (development)
NEXT_PUBLIC_API_URL=http://localhost:5035

# Production
NEXT_PUBLIC_API_URL=https://api.clercq.it
```

## Auth0 Setup

### Initial Configuration

1. **Create Account**: Sign up at https://auth0.com
2. **Create Application**: Create a new Regular Web Application in Auth0 dashboard
3. **Get Credentials**: Copy the Domain, Client ID, and Client Secret from application settings
4. **Create API**: Create a new API in Auth0 dashboard for your backend
5. **Configure OAuth**:
   - Enable GitHub and LinkedIn social connections in Auth0 dashboard
   - Configure redirect URLs in Application Settings:
     - Development: `http://localhost:5035/api/auth/callback`
     - Production: `https://api.clercq.it/api/auth/callback`
   - Add allowed callback URLs and logout URLs

### Role Configuration

Create the following roles in Auth0 dashboard (Authorization > Roles):

1. **Admin.View**
   - Description: Access admin area and view content
   - Assigned to: Site administrators

2. **Blogs.Contributor**
   - Description: Create, edit, and delete blog posts
   - Assigned to: Content creators and blog authors

3. **Projects.Contributor**
   - Description: Create, edit, and delete projects
   - Assigned to: Portfolio managers

4. **Certifications.Contributor**
   - Description: Create, edit, and delete certifications
   - Assigned to: Portfolio managers

### Enable Role-Based Access Control

1. Enable RBAC in Auth0 API settings
2. Add roles to access tokens
3. Create an Action or Rule to add roles to the JWT token
4. Assign roles to users via Auth0 dashboard (User Management > Roles)

## Security Considerations

### Token Validation

The backend validates JWT tokens by:
1. Verifying the token signature using the configured secret key
2. Checking token expiration
3. Validating issuer and audience claims
4. Extracting and verifying role claims

### API Key Protection

- Never commit API keys to source control
- Use environment variables or secret management systems
- Rotate API keys periodically
- Use separate API keys for development and production

### OAuth Security

- Redirect URLs must exactly match those configured in OAuth providers
- State parameters are used to prevent CSRF attacks
- OAuth tokens are validated through Auth0

### MFA Best Practices

- Encourage users to enable MFA for enhanced security
- Support TOTP-based authenticators (Google Authenticator, Authy, etc.)
- Automatically detect MFA status without manual configuration

## Troubleshooting

### Common Issues

**Issue**: "Unauthorized" errors when accessing admin endpoints
- **Cause**: Invalid or expired JWT token
- **Solution**: Log in again to obtain a fresh token

**Issue**: "No Access" message after successful login
- **Cause**: User has no roles assigned in Auth0
- **Solution**: Administrator must assign roles in Auth0 dashboard

**Issue**: OAuth callback errors
- **Cause**: Redirect URLs don't match configuration
- **Solution**: Verify redirect URLs in OAuth provider settings and Auth0 match exactly

**Issue**: MFA not detected
- **Cause**: User has MFA enabled but system doesn't show TOTP field
- **Solution**: Ensure Auth0 API properly returns 428 status for MFA-enabled accounts

## Related Documentation

- [Admin Backoffice Documentation](./admin-backoffice.md) - Detailed admin panel usage
- [Features Documentation](./features.md) - Overview of all application features
- [DevOps Guide](./devops.md) - Deployment and CI/CD configuration
