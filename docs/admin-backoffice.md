# Admin Backoffice Documentation

## Overview

The Clercq.It website includes a secure admin backoffice accessible at `/admin` for content management. The backoffice uses Scaleway IAM integration for authentication and provides a clean interface for managing blogs, projects, and system settings.

## Accessing the Admin Panel

### URL
The admin panel is accessible at: `https://www.clercq.it/admin`

**Important:** There are no direct links to the admin panel on public pages. You must navigate to this URL directly.

### Authentication

The system uses Scaleway IAM credentials for authentication:

1. Navigate to `/admin`
2. Enter your Scaleway Access Key (starts with `SCW_`)
3. Enter your Scaleway Secret Key
4. Click "Sign In"

Upon successful authentication, a JWT token is generated and stored in the browser's localStorage, which is used for subsequent API requests.

## Features

### Dashboard Overview
- **Total Blogs**: Count of published blog posts
- **Total Projects**: Count of portfolio projects
- **Media Files**: Count of images in storage
- **System Status**: Current health status

### Blog Management
- Create new blog posts
- Edit existing blogs (placeholder)
- Manage blog images

### Project Management
- Add new projects (placeholder)
- Edit portfolio items (placeholder)

### Settings
- View authentication status
- Storage connection status
- API health status

## Technical Implementation

### Frontend (Next.js)
- **Login Page**: `/admin/page.tsx`
- **Dashboard**: `/admin/dashboard/page.tsx`
- **Layout**: `/admin/layout.tsx` - Separate layout without public header/footer
- **API Route**: `/app/api/auth/login/route.ts` - Proxy to backend authentication

### Backend (.NET API)
- **Token Service**: `Features/Auth/TokenService.cs` - Generates and validates JWT tokens
- **Auth Endpoints**: `Features/AuthEndpoints.cs` - Provides `/api/auth/token` endpoint
- **CORS**: Configured to allow frontend requests from localhost:3000 and www.clercq.it

### Authentication Flow
1. User enters Scaleway IAM credentials in the login form
2. Frontend calls `/api/auth/login` (Next.js API route)
3. Next.js API route forwards request to backend `/api/auth/token`
4. Backend validates credentials against configured Scaleway admin credentials
5. If valid, backend generates a JWT token with admin claims
6. Token is returned to frontend and stored in localStorage
7. All subsequent API requests include the token in the Authorization header

## Configuration

### Backend Configuration

Add the following to your `appsettings.json` or environment variables:

```json
{
  "Scaleway": {
    "AdminAccessKey": "SCW_XXX...",
    "AdminSecretKey": "your-secret-key"
  },
  "Authentication": {
    "JwtSecretKey": "your-jwt-secret-key",
    "Issuer": "Clercq.It",
    "Audience": "Clercq.It.Api",
    "ExpirationMinutes": 60
  }
}
```

**Environment Variables (Production):**
- `Scaleway__AdminAccessKey`: Scaleway IAM access key
- `Scaleway__AdminSecretKey`: Scaleway IAM secret key
- `Authentication__JwtSecretKey`: Secret key for signing JWT tokens

### Frontend Configuration

Create a `.env.local` file in the Next.js project:

```env
NEXT_PUBLIC_API_URL=http://localhost:5035  # For development
# Or for production:
NEXT_PUBLIC_API_URL=https://api.clercq.it
```

## Security Considerations

1. **No Public Links**: The admin panel has no links from public pages, making it harder to discover
2. **Scaleway IAM**: Uses Scaleway credentials for authentication (can be enhanced with actual Scaleway SDK)
3. **JWT Tokens**: Secure token-based authentication with configurable expiration
4. **CORS**: Restricted to specific origins
5. **HTTPS Only**: Should only be accessed over HTTPS in production

## Future Enhancements

1. **Full Scaleway IAM Integration**: Currently uses simple credential comparison. Can be enhanced to use Scaleway SDK for proper IAM validation
2. **Multi-Factor Authentication**: Add MFA support
3. **Role-Based Access Control**: Implement different permission levels
4. **Audit Logging**: Track admin actions
5. **Session Management**: Better session handling and refresh tokens
6. **Rate Limiting**: Prevent brute force attacks

## Screenshots

### Login Page
![Admin Login](https://github.com/user-attachments/assets/b1603b9c-ce92-4662-8492-85c151675483)

The login page features:
- Clean, centered card layout
- Lock icon for security indication
- Access Key and Secret Key input fields
- Responsive design with gradient background

### Dashboard Overview
![Dashboard Overview](https://github.com/user-attachments/assets/611f193a-5d6d-4d41-9d1c-dd3316dcd963)

The dashboard provides:
- Quick stats cards showing counts
- Tab navigation for different sections
- Welcome message with guidance
- Logout button in header

### Blog Management
![Blog Management](https://github.com/user-attachments/assets/586d04ab-3c16-44d2-85dd-89194cf85a89)

The blog management section includes:
- Empty state with call-to-action
- Create blog post button (placeholder)
- Future: List of existing blogs with edit/delete actions

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

### Testing Credentials (Development)

The development environment includes test credentials:
- **Access Key**: `SCW_DEMO_ACCESS_KEY`
- **Secret Key**: `demo_secret_key_12345`

**Note:** These are for local development only and should never be used in production.

## Support

For issues or questions related to the admin panel:
- Check this documentation
- Review the API logs in Scaleway Cockpit (production)
- Check browser console for frontend errors
- Verify JWT token configuration
