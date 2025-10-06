# Quasr.io Integration Implementation Plan

## Overview
Replace Scaleway IAM authentication with Quasr.io Identity as a Service, implementing:
- Username/password authentication with MFA
- GitHub and LinkedIn OAuth 
- User registration with email verification
- Fine-grained role-based access control

## Backend Changes (.NET API)

### 1. Remove Scaleway Dependencies
- [x] Remove `TokenService.ValidateScalewayCredentials` method
- [x] Remove `ScalewayValidationResult` class
- [x] Remove Scaleway-specific authentication logic

### 2. Add Quasr.io Integration  
- [ ] Install Quasr.io SDK/client library (or create HTTP client if no SDK)
- [ ] Create `QuasrAuthService` for user validation via Quasr.io API
- [ ] Implement role/group checking with Quasr.io
- [ ] Update `TokenService` to generate JWT with Quasr.io user data and roles

### 3. Update Authentication Endpoints
- [ ] `POST /api/auth/login` - Username/password with MFA detection
- [ ] `POST /api/auth/register` - User registration with email verification
- [ ] `GET /api/auth/github` - Initiate GitHub OAuth via Quasr.io
- [ ] `GET /api/auth/github/callback` - Handle GitHub OAuth callback
- [ ] `GET /api/auth/linkedin` - Initiate LinkedIn OAuth via Quasr.io
- [ ] `GET /api/auth/linkedin/callback` - Handle LinkedIn OAuth callback

### 4. Implement Role-Based Authorization
- [ ] Update JWT claims to include Quasr.io roles
- [ ] Apply `[Authorize(Roles = "Admin.View")]` to dashboard endpoints
- [ ] Apply `[Authorize(Roles = "Blogs.Contributor")]` to blog endpoints
- [ ] Apply `[Authorize(Roles = "Projects.Contributor")]` to project endpoints

### 5. Configuration
```bash
Quasr__ApiUrl=https://api.quasr.io
Quasr__ApiKey=your-quasr-api-key
Authentication__JwtSecretKey=your-jwt-secret
```

## Frontend Changes (Next.js)

### 1. Update Login Page (`/admin/page.tsx`)
- [ ] Replace Scaleway fields with username/password
- [ ] Keep MFA/TOTP input (dynamic visibility)
- [ ] Add "Sign in with GitHub" button
- [ ] Add "Sign in with LinkedIn" button
- [ ] Add "Create Account" link

### 2. Create Registration Page (`/admin/register/page.tsx`)
- [ ] User registration form (username, email, password)
- [ ] Form validation
- [ ] Email verification flow
- [ ] Success message with verification instructions

### 3. Update Dashboard (`/admin/dashboard/page.tsx`)
- [ ] Fetch user roles from JWT
- [ ] Conditionally show/hide tabs based on roles:
  - Overview: `Admin.View`
  - Blogs: `Blogs.Contributor`
  - Projects: `Projects.Contributor`
  - Settings: `Admin.View`

### 4. Create OAuth Callback Handlers
- [ ] `/api/auth/github/callback/route.ts` - Handle GitHub OAuth
- [ ] `/api/auth/linkedin/callback/route.ts` - Handle LinkedIn OAuth

## Roles & Permissions

### Role Definitions
1. **`Admin.View`** - Access admin area and view content
2. **`Blogs.Contributor`** - Create/edit/delete blog posts
3. **`Projects.Contributor`** - Create/edit/delete projects

### Group Structure
- **Admin group** - All three roles combined
- Users can have individual roles without Admin group membership

## Authentication Flows

### Username/Password Flow
1. User enters username/password
2. Backend validates with Quasr.io API
3. If MFA required, return 428 status
4. User enters TOTP code
5. Backend validates credentials + TOTP
6. Generate JWT with roles from Quasr.io
7. Return token to frontend

### OAuth Flow (GitHub/LinkedIn)
1. User clicks OAuth button
2. Frontend redirects to `/api/auth/{provider}`
3. Backend redirects to Quasr.io OAuth endpoint
4. User authenticates with provider via Quasr.io
5. Quasr.io redirects to `/api/auth/{provider}/callback`
6. Backend validates OAuth response with Quasr.io
7. Generate JWT with roles
8. Redirect to dashboard

### Registration Flow
1. User fills registration form
2. Backend creates user in Quasr.io (no roles)
3. Quasr.io sends verification email
4. User clicks verification link
5. Account activated (still no admin access)
6. Admin assigns roles in Quasr.io dashboard

## Testing Checklist
- [ ] Username/password login
- [ ] MFA/TOTP detection and validation
- [ ] GitHub OAuth flow
- [ ] LinkedIn OAuth flow
- [ ] User registration
- [ ] Email verification
- [ ] Role-based dashboard tab visibility
- [ ] Role-based API authorization
- [ ] Logout functionality

## Documentation Updates
- [ ] Update `/docs/admin-backoffice.md`
- [ ] Update `/docs/admin-authentication-demo.md`
- [ ] Update `/docs/authentication-flow-diagram.md`
- [ ] Add Quasr.io-specific configuration guide
