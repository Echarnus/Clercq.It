# Admin Backoffice Documentation

## Overview

The Clercq.It website includes a secure admin backoffice accessible at `/admin` for content management. The backoffice uses **Quasr.io Identity as a Service** for authentication with support for **username/password login, OAuth providers (GitHub, LinkedIn), and automatic MFA detection**. It provides a clean interface with **fine-grained role-based access control** for managing blogs, projects, and system settings.

## Accessing the Admin Panel

### URL
The admin panel is accessible at: `https://www.clercq.it/admin`

**Important:** There are no direct links to the admin panel on public pages. You must navigate to this URL directly.

### Authentication Methods

The system supports multiple authentication methods via Quasr.io:

#### 1. Username/Password Login
1. Navigate to `/admin`
2. Enter your username
3. Enter your password
4. Click "Sign In"
5. **If MFA is enabled on your account**, the system will automatically detect it and show a TOTP input field
6. Enter your 6-digit TOTP code from your authenticator app
7. Click "Sign In" again to complete authentication

#### 2. GitHub OAuth
1. Navigate to `/admin`
2. Click "Sign in with GitHub"
3. You'll be redirected to GitHub to authorize
4. After authorization, you'll be logged into the admin panel

#### 3. LinkedIn OAuth
1. Navigate to `/admin`
2. Click "Sign in with LinkedIn"
3. You'll be redirected to LinkedIn to authorize
4. After authorization, you'll be logged into the admin panel

### User Registration

New users can register via `/admin/register`:
1. Enter username, email, and password
2. Submit the registration form
3. Check your email for a verification link
4. Click the verification link to confirm your email
5. **Note:** New users have no roles assigned and cannot access admin features until an administrator assigns roles in Quasr.io

Upon successful authentication, a JWT token from Quasr.io is stored in the browser's localStorage, which is used for subsequent API requests.

## Role-Based Access Control

The admin backoffice uses fine-grained roles to control access:

### Roles
- **`Admin.View`** - Required to access the admin area and view content
- **`Blogs.Contributor`** - Required to create, edit, and delete blog posts  
- **`Projects.Contributor`** - Required to create, edit, and delete projects

### Admin Group
Users can be added to the **Admin group** in Quasr.io, which grants all three roles automatically.

### Tab Visibility
Dashboard tabs are conditionally shown based on user roles:
- **Overview tab**: Visible if user has `Admin.View` role
- **Blogs tab**: Visible if user has `Blogs.Contributor` role
- **Projects tab**: Visible if user has `Projects.Contributor` role
- **Settings tab**: Visible if user has `Admin.View` role

Users without any roles will see a "No Access" message with instructions to request access from an administrator.

## Features

### Dashboard Overview
- **Total Blogs**: Count of published blog posts
- **Total Projects**: Count of portfolio projects
- **Media Files**: Count of images in storage
- **System Status**: Current health status
- **User Welcome**: Displays logged-in username
- **Role Display**: Shows assigned roles in Settings tab

### Blog Management
- Create new blog posts (requires `Blogs.Contributor` role)
- Edit existing blogs (placeholder)
- Manage blog images

### Project Management
- Add new projects (requires `Projects.Contributor` role)
- Edit portfolio items (placeholder)

### Settings
- View authentication status
- Display user roles
- Storage connection status
- API health status

## Technical Implementation

### Frontend (Next.js)
- **Login Page**: `/admin/page.tsx`
- **Dashboard**: `/admin/dashboard/page.tsx`
- **Registration**: `/admin/register/page.tsx`
- **OAuth Callback**: `/admin/auth/callback/page.tsx`
- **Layout**: `/admin/layout.tsx` - Separate layout without public header/footer
- **API Routes**: 
  - `/app/api/auth/login/route.ts` - Proxy to backend authentication
  - `/app/api/auth/register/route.ts` - Proxy to backend registration

### Backend (.NET API)
- **Quasr Auth Service**: `Features/Auth/QuasrAuthService.cs` - Handles authentication with Quasr.io
- **Auth Endpoints**: `Features/AuthEndpoints.cs` - Provides authentication and OAuth endpoints
- **CORS**: Configured to allow frontend requests from localhost:3000 and www.clercq.it

### Authentication Flow

#### Username/Password Login
1. User enters username and password in the login form
2. Frontend calls `/api/auth/login` (Next.js API route)
3. Next.js API route forwards request to backend `/api/auth/login`
4. Backend calls Quasr.io API to authenticate user
5. **If MFA is required**, backend detects this from Quasr.io's response and returns status 428 with `requiresMfa: true`
6. Frontend automatically shows the TOTP input field when MFA is detected
7. User enters 6-digit TOTP code from their authenticator app
8. Frontend resends the request with the TOTP code included
9. Backend validates credentials + TOTP code with Quasr.io
10. If valid, Quasr.io returns a JWT token with user info and roles
11. Token is returned to frontend and stored in localStorage
12. All subsequent API requests include the token in the Authorization header

#### OAuth Flow (GitHub/LinkedIn)
1. User clicks "Sign in with GitHub" or "Sign in with LinkedIn"
2. Frontend redirects to backend OAuth initiation endpoint
3. Backend redirects to Quasr.io OAuth endpoint
4. Quasr.io handles OAuth dance with provider (GitHub/LinkedIn)
5. After authorization, Quasr.io redirects back to backend callback
6. Backend validates the callback with Quasr.io
7. Quasr.io returns JWT token with user info and roles
8. Backend redirects to frontend callback page with token
9. Frontend stores token in localStorage and navigates to dashboard

#### User Registration
1. User fills out registration form at `/admin/register`
2. Frontend calls `/api/auth/register` (Next.js API route)
3. Backend calls Quasr.io API to create user
4. Quasr.io creates user with no roles and sends verification email
5. User clicks verification link in email
6. User's email is verified, but no roles assigned yet
7. Admin assigns roles manually in Quasr.io dashboard

**Important:** The system:
- Uses JWT tokens provided by Quasr.io (not self-generated)
- Automatically detects if MFA/TOTP is required
- Validates all credentials via Quasr.io Identity as a Service
- No user credentials are stored in the application

## Configuration

### Backend Configuration

The application requires Quasr.io API configuration:

```json
{
  "Quasr": {
    "ApiUrl": "https://api.quasr.io",
    "ApiKey": "your-quasr-api-key",
    "ClientRedirectUrl": "http://localhost:3000"
  }
}
```

**Environment Variables (Production):**
- `Quasr__ApiUrl`: Quasr.io API endpoint (e.g., `https://api.quasr.io`)
- `Quasr__ApiKey`: API key for authenticating with Quasr.io (required)
- `Quasr__ClientRedirectUrl`: Frontend URL for OAuth redirects (e.g., `https://www.clercq.it`)

**Note:** JWT tokens are issued by Quasr.io, not by the application. The backend validates tokens using Quasr.io's public keys.

### Frontend Configuration

Create a `.env.local` file in the Next.js project:

```env
NEXT_PUBLIC_API_URL=http://localhost:5035  # For development
# Or for production:
NEXT_PUBLIC_API_URL=https://api.clercq.it
```

### Quasr.io Setup

1. Create a Quasr.io account at https://quasr.io
2. Create a new application/project in Quasr.io dashboard
3. Copy the API key from Quasr.io
4. Configure OAuth providers (GitHub, LinkedIn) in Quasr.io dashboard:
   - Create OAuth apps in GitHub and LinkedIn developer portals
   - Add OAuth client IDs and secrets to Quasr.io
   - Configure redirect URLs to point to your backend
5. Create roles in Quasr.io:
   - `Admin.View`
   - `Blogs.Contributor`
   - `Projects.Contributor`
6. Optionally create an "Admin" group with all three roles

## Security Considerations

1. **No Public Links**: The admin panel has no links from public pages, making it harder to discover
2. **Quasr.io Identity Service**: Uses Quasr.io for secure identity management and authentication
3. **OAuth Providers**: Supports GitHub and LinkedIn OAuth for passwordless login
4. **Email Verification**: Requires email verification for new user registrations
5. **Automatic MFA Detection**: Detects and handles MFA/TOTP requirements automatically
6. **JWT Tokens from Quasr.io**: Uses tokens issued by Quasr.io (not self-generated)
7. **Fine-Grained Roles**: Separate permissions for viewing, managing blogs, and managing projects
8. **CORS**: Restricted to specific origins
9. **HTTPS Only**: Should only be accessed over HTTPS in production

## Future Enhancements

1. **Additional OAuth Providers**: Support for Google, Microsoft, etc.
2. **Enhanced Role Management**: UI for managing user roles
3. **Additional MFA Methods**: Support for hardware keys, SMS, etc.
4. **Audit Logging**: Track admin actions
5. **Session Management**: Better session handling and refresh tokens
6. **Rate Limiting**: Prevent brute force attacks
7. **User Profile Management**: Allow users to link/unlink OAuth providers

## Screenshots

### Login Page
![Admin Login](https://github.com/user-attachments/assets/b1603b9c-ce92-4662-8492-85c151675483)

The login page features:
- Clean, centered card layout
- Lock icon for security indication
- Username and password input fields
- OAuth provider buttons (GitHub, LinkedIn)
- Link to registration page
- Responsive design with gradient background

### Dashboard Overview
![Dashboard Overview](https://github.com/user-attachments/assets/611f193a-5d6d-4d41-9d1c-dd3316dcd963)

The dashboard provides:
- Quick stats cards showing counts
- Tab navigation for different sections (based on user roles)
- Welcome message with username
- System status indicators
- Logout button in header

### Blog Management
![Blog Management](https://github.com/user-attachments/assets/586d04ab-3c16-44d2-85dd-89194cf85a89)

The blog management section includes:
- Empty state with call-to-action
- Create blog post button (placeholder)
- Future: List of existing blogs with edit/delete actions
- Only visible if user has `Blogs.Contributor` role

## Development

### Running Locally

1. Start the API:
```bash
cd src/ClercqIt.Api
dotnet run
```

2. Start the Next.js frontend:
```bash
cd src/ClercqIt.Web
pnpm dev
```

3. Navigate to `http://localhost:3000/admin`

### Testing Authentication

#### Username/Password Login
1. Register a new account at `/admin/register`
2. Check email for verification link
3. Click verification link
4. Ask admin to assign roles in Quasr.io dashboard
5. Log in at `/admin` with username and password

#### OAuth Login
1. Click "Sign in with GitHub" or "Sign in with LinkedIn"
2. Authorize the application
3. You'll be redirected back and logged in
4. If first time, account created in Quasr.io (no roles assigned)
5. Ask admin to assign roles in Quasr.io dashboard

**Note:** Users without roles will see a "No Access" message after logging in.

## Support

For issues or questions related to the admin panel:
- Check this documentation
- Review Quasr.io documentation at https://docs.quasr.io
- Check browser console for frontend errors
- Review API logs for backend errors
- Verify Quasr.io configuration and API key
