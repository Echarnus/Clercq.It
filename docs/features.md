# Features

This document provides an overview of the main features available in the Clercq.It application.

## Blog Management

The Blog Management feature allows administrators to create, manage, and publish blog posts with rich markdown content and images.

### Key Capabilities
- **Rich Content Creation**: Full markdown support for blog content with headers, lists, code blocks, links, and images
- **Image Upload**: Direct image upload to Scaleway Object Storage with automatic URL generation
- **Tagging System**: Support for up to 10 tags per blog post for better organization and searchability
- **Public API**: Publicly accessible endpoint for retrieving blog posts
- **Protected Creation**: Blog creation requires authentication via JWT tokens

### API Endpoints
- `GET /api/blogs` - Retrieve all published blogs (public)
- `POST /api/blogs` - Create a new blog post with image (authenticated)

### Validation Rules
- Short description: Required, max 500 characters
- Long description: Required, max 50,000 characters, supports full markdown
- Image: Required, supports JPG, JPEG, PNG, GIF, WebP formats
- Tags: At least 1 tag required, maximum 10 tags

### Storage
- Blog images are stored in Scaleway Object Storage with public-read permissions
- Images use unique GUID paths to prevent naming collisions
- Direct S3-compatible object storage URLs for optimal performance

## Project Portfolio Management

The Project Portfolio feature enables administrators to showcase their work through a structured project catalog with rich descriptions, images, and skill tags.

### Key Capabilities
- **Two-Step Creation**: Separate image upload and project creation for better control
- **Project Timeline**: Start and end date tracking for each project
- **Featured Projects**: Mark important projects as featured for homepage display
- **Skills Tracking**: Associate projects with relevant technologies and skills
- **Rich Descriptions**: Full markdown support for detailed project documentation

### API Endpoints
- `GET /api/projects` - Retrieve all projects (public)
- `GET /api/projects/featured` - Retrieve only featured projects (public)
- `POST /api/projects/images` - Upload a project image (authenticated)
- `POST /api/projects` - Create a new project (authenticated)

### Validation Rules
- Title: Required, max 200 characters
- Short description: Required, max 500 characters
- Long description: Required, max 50,000 characters, supports full markdown
- Image URL: Required, must be a valid HTTP/HTTPS URL from the image upload endpoint
- Start/End dates: Required, valid ISO 8601 format, end date must be >= start date
- Skills: At least 1 skill required, maximum 20 skills
- Featured flag: Optional, defaults to false

### Workflow
1. Upload project image via `POST /api/projects/images` to get the image URL
2. Create project with the returned image URL via `POST /api/projects`

### Storage
- Project images stored separately in Scaleway Object Storage
- Images have unique GUID paths for collision prevention
- Public-read permissions for direct access

## Admin Backoffice

The Admin Backoffice is a secure, role-based content management system accessible at `/admin` for managing blogs, projects, and system settings.

### Authentication

The system uses **Cloud IAM (KeyCloak-based) Identity as a Service** for comprehensive authentication:

#### Supported Authentication Methods
1. **Username/Password Login**: Traditional credentials-based authentication with automatic MFA detection
2. **GitHub OAuth**: Single sign-on via GitHub
3. **LinkedIn OAuth**: Single sign-on via LinkedIn

#### Multi-Factor Authentication (MFA)
- Automatic detection of MFA-enabled accounts
- TOTP (Time-based One-Time Password) support via authenticator apps
- Dynamic UI that shows MFA input only when required

### User Management

#### Registration Flow
1. Users register at `/admin/register` with username, email, and password
2. Email verification link sent to confirm account
3. Account activated but without any roles assigned
4. Administrator assigns roles via Cloud IAM dashboard

#### Role-Based Access Control (RBAC)

The system implements fine-grained permissions through three distinct roles:

- **`Admin.View`**: Access admin area and view content
- **`Blogs.Contributor`**: Create, edit, and delete blog posts
- **`Projects.Contributor`**: Create, edit, and delete projects

Users can have individual roles or be added to an "Admin" group that includes all three roles.

### Dashboard Features

The admin dashboard provides role-based tab visibility:
- **Overview**: Visible to users with `Admin.View` role
- **Blogs**: Visible to users with `Blogs.Contributor` role
- **Projects**: Visible to users with `Projects.Contributor` role
- **Settings**: Visible to users with `Admin.View` role

### Security Features
- JWT token storage in browser localStorage
- Tokens issued by Cloud IAM with embedded role claims
- Backend validates tokens using Cloud IAM's public keys
- No access for users without assigned roles

### OAuth Configuration

OAuth providers must be configured in the Cloud IAM dashboard:
1. Create OAuth apps in GitHub and LinkedIn developer portals
2. Add OAuth client IDs and secrets to Cloud IAM
3. Configure redirect URLs to point to the backend API

### Protected Routes
All blog and project creation/modification endpoints require valid JWT tokens with appropriate roles.
