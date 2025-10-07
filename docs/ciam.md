# Customer Identity and Access Management (CIAM)

This document describes the CIAM implementation using Quasr.io Identity as a Service for the Clercq.It application.

## Overview

Clercq.It uses **Quasr.io** as an external Identity as a Service (IDaaS) provider to handle all authentication and user management concerns. This approach offloads the complexity of secure authentication, MFA, OAuth integration, and user lifecycle management to a dedicated service.

## Quasr.io Architecture

### Authentication Flow

#### Username/Password Authentication

1. **User Submission**: User enters username and password in the frontend login form (`/admin`)
2. **API Request**: Frontend sends credentials to backend endpoint `POST /api/auth/login`
3. **Quasr.io Validation**: Backend forwards credentials to Quasr.io API for validation
4. **MFA Detection**: 
   - If user has MFA enabled, Quasr.io returns HTTP 428 (Precondition Required)
   - Backend returns this status to frontend
   - Frontend dynamically displays TOTP input field
5. **MFA Validation** (if required):
   - User enters 6-digit TOTP code from authenticator app
   - Frontend resubmits with username, password, and TOTP
   - Backend validates complete credentials with Quasr.io
6. **Role Retrieval**: Upon successful authentication, backend retrieves user roles from Quasr.io
7. **JWT Generation**: Backend generates a JWT token containing:
   - User ID and username from Quasr.io
   - Assigned roles (e.g., `Admin.View`, `Blogs.Contributor`, `Projects.Contributor`)
   - Standard claims (issuer, audience, expiration)
8. **Token Response**: JWT returned to frontend and stored in browser localStorage
9. **Subsequent Requests**: Frontend includes JWT in `Authorization: Bearer <token>` header for protected endpoints

#### OAuth Authentication (GitHub/LinkedIn)

1. **OAuth Initiation**: User clicks "Sign in with GitHub" or "Sign in with LinkedIn"
2. **Backend Redirect**: Frontend navigates to backend endpoint `GET /api/auth/{provider}`
3. **Quasr.io OAuth Flow**: 
   - Backend redirects to Quasr.io OAuth endpoint
   - Quasr.io handles provider-specific OAuth flow
4. **User Authorization**: User authorizes the application via GitHub/LinkedIn
5. **Callback Handling**: 
   - Provider redirects to Quasr.io
   - Quasr.io validates OAuth response
   - Quasr.io redirects to backend callback `GET /api/auth/{provider}/callback`
6. **User Creation** (first-time OAuth users):
   - Quasr.io automatically creates user account
   - No roles assigned by default
7. **Role Retrieval**: Backend fetches user roles from Quasr.io API
8. **JWT Generation**: Backend creates JWT with user data and roles
9. **Frontend Redirect**: Backend redirects to frontend dashboard with JWT in URL or cookie
10. **Dashboard Access**: Frontend extracts JWT and stores it for subsequent requests

#### User Registration Flow

1. **Registration Form**: User fills form at `/admin/register` (username, email, password)
2. **Account Creation**: Frontend sends registration data to `POST /api/auth/register`
3. **Quasr.io User Creation**: Backend creates user in Quasr.io with no roles assigned
4. **Email Verification**: Quasr.io automatically sends verification email to user
5. **Email Confirmation**: User clicks verification link in email
6. **Account Activation**: Quasr.io marks account as verified
7. **Role Assignment**: Administrator manually assigns roles via Quasr.io dashboard
8. **Login Access**: User can now log in and access features based on assigned roles

### Role-Based Authorization

Once authenticated, the JWT token contains role claims that control access:

- **Frontend**: Shows/hides dashboard tabs based on roles in JWT
- **Backend**: Validates JWT and checks role claims before processing requests
- **API Endpoints**: Protected with `[Authorize(Roles = "RoleName")]` attributes

Users without any roles can authenticate but will see a "No Access" message in the admin panel.

## Container Configuration

### Environment Variables

The application container must be configured with the following Quasr.io-related environment variables:

#### Required Configuration

```bash
# Quasr.io API Configuration
Quasr__ApiUrl=https://api.quasr.io
Quasr__ApiKey=<your-quasr-api-key>
Quasr__ClientRedirectUrl=<frontend-url>
```

#### Configuration Details

**Quasr__ApiUrl**
- The Quasr.io API endpoint
- Default: `https://api.quasr.io`
- Used for all authentication and user management operations

**Quasr__ApiKey**
- API key for authenticating with Quasr.io
- Obtained from Quasr.io dashboard
- Required for all Quasr.io API calls
- **Critical**: Keep this secret and never commit to source control

**Quasr__ClientRedirectUrl**
- Frontend URL for OAuth redirects
- Development: `http://localhost:3000`
- Production: `https://www.clercq.it`
- Must match OAuth redirect URLs configured in Quasr.io

### Dockerfile Configuration

The containerized application automatically reads environment variables. No code changes needed for deployment.

Example Docker run command:

```bash
docker run -d \
  -p 80:80 \
  -e Quasr__ApiUrl=https://api.quasr.io \
  -e Quasr__ApiKey=<your-api-key> \
  -e Quasr__ClientRedirectUrl=https://www.clercq.it \
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
    "Quasr__ApiUrl" = "https://api.quasr.io"
    "Quasr__ClientRedirectUrl" = var.client_redirect_url
  }
  
  secret_environment_variables = {
    "Quasr__ApiKey" = var.quasr_api_key
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

## Quasr.io Setup

### Initial Configuration

1. **Create Account**: Sign up at https://quasr.io
2. **Create Application**: Create a new application/project in Quasr.io dashboard
3. **Get API Key**: Copy the API key from the application settings
4. **Configure OAuth**:
   - Create OAuth apps in [GitHub Developer Settings](https://github.com/settings/developers)
   - Create OAuth apps in [LinkedIn Developer Portal](https://www.linkedin.com/developers/)
   - Add OAuth client IDs and secrets to Quasr.io
   - Configure redirect URLs:
     - Development: `http://localhost:5035/api/auth/github/callback`, `http://localhost:5035/api/auth/linkedin/callback`
     - Production: `https://api.clercq.it/api/auth/github/callback`, `https://api.clercq.it/api/auth/linkedin/callback`

### Role Configuration

Create the following roles in Quasr.io dashboard:

1. **Admin.View**
   - Description: Access admin area and view content
   - Assigned to: Site administrators

2. **Blogs.Contributor**
   - Description: Create, edit, and delete blog posts
   - Assigned to: Content creators and blog authors

3. **Projects.Contributor**
   - Description: Create, edit, and delete projects
   - Assigned to: Portfolio managers

### Optional Group Setup

Create an "Admin" group with all three roles for convenience:
- Assign all three roles to the group
- Add administrators to this group for full access

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
- OAuth tokens are validated through Quasr.io

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
- **Cause**: User has no roles assigned in Quasr.io
- **Solution**: Administrator must assign roles in Quasr.io dashboard

**Issue**: OAuth callback errors
- **Cause**: Redirect URLs don't match configuration
- **Solution**: Verify redirect URLs in OAuth provider settings and Quasr.io match exactly

**Issue**: MFA not detected
- **Cause**: User has MFA enabled but system doesn't show TOTP field
- **Solution**: Ensure Quasr.io API properly returns 428 status for MFA-enabled accounts

## Related Documentation

- [Admin Backoffice Documentation](./admin-backoffice.md) - Detailed admin panel usage
- [Features Documentation](./features.md) - Overview of all application features
- [DevOps Guide](./devops.md) - Deployment and CI/CD configuration
