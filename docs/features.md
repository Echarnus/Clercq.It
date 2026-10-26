# Features

This document provides an overview of the main features available in the Clercq.It application.

## Visitor Statistics

The admin dashboard's **Statistics > Visitors** section shows page views, unique page views and
unique visitors for the last 7, 30 or 90 days, with a daily trend and the top
pages. Counting is first-party and cookieless; details in
[Visitor statistics](./statistics.md).

- `POST /api/stats/pageview` - Count a page view (public, sent by the site)
- `GET /api/stats?days=30` - Statistics for a period (`Admin.View`)

## Blog Management

The Blog Management feature allows administrators to create, manage, and publish blog posts with rich markdown content and images.

### Key Capabilities
- **Rich Content Creation**: Full markdown support for blog content with headers, lists, code blocks, links, and images
- **Image Upload**: Direct image upload to Scaleway Object Storage with automatic URL generation
- **Tagging System**: Support for up to 10 tags per blog post for better organization and searchability
- **Public API**: Publicly accessible endpoint for retrieving blog posts
- **Protected Changes**: Creating, updating and deleting needs the `Blogs.Contributor` role

### API Endpoints
- `GET /api/blogs`, `GET /api/blogs/{id}` - Retrieve blog posts (public)
- `POST /api/blogs` - Create a blog post with an `image` file (multipart form)
- `PUT /api/blogs/{id}` - Update a blog post
- `DELETE /api/blogs/{id}` - Delete a blog post
- `POST /api/images` - Upload an image for use inside Markdown, returns its URL

### Validation Rules
- Title: Required, max 200 characters
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
- **Project Timeline**: Start and end date tracking for each project
- **Featured Projects**: Mark important projects as featured for homepage display
- **Skills Tracking**: Associate projects with relevant technologies and skills
- **Rich Descriptions**: Full markdown support for detailed project documentation

### API Endpoints
- `GET /api/projects`, `GET /api/projects/{id}` - Retrieve projects (public)
- `GET /api/projects/featured` - Retrieve only featured projects (public)
- `POST /api/projects` - Create a project (multipart form, optional `image` file; `Projects.Contributor`)
- `PUT /api/projects/{id}` - Update a project (`Projects.Contributor`). Send a new `image` file to replace the image, or `removeImage=true` to clear it; send neither to keep it
- `DELETE /api/projects/{id}` - Delete a project (`Projects.Contributor`)

### Validation Rules
- Title: Required, max 200 characters
- Short description: Required, max 500 characters
- Long description: Required, max 50,000 characters, supports full markdown
- Image: Optional. A project without an image shows no picture on the site
- Start/End dates: Required, valid ISO 8601 format, end date must be >= start date
- Skills: At least 1 skill required, maximum 20 skills
- Featured flag: Optional, defaults to false

### Admin image field
In the admin project and certification dialogs, the red X on the current image removes it; saving then stores the item without an image (issues #405 and #479). The API takes `removeImage=true` on `PUT /api/certifications/{id}` for this, and `POST /api/certifications` accepts a certification without an `image` file. Blog posts need an image, so their dialog shows **Replace image** instead of an X. The `/placeholder.svg` import default counts as no image.

### Storage
- Project images stored separately in Scaleway Object Storage
- Images have unique GUID paths for collision prevention
- Public-read permissions for direct access

## Admin Backoffice

The Admin Backoffice is a secure, role-based content management system accessible at `/admin` for managing blogs, projects, and system settings.

### Authentication

The system uses **Cloud IAM (hosted Keycloak)** for authentication. **Sign In** on `/admin` sends the browser to Keycloak's login page (authorization code flow with PKCE); Keycloak handles the password, MFA (TOTP) and brute-force protection, then returns to `/admin/callback`. The site never sees a password. Details: [Cloud IAM](./ciam.md).

### User Management

#### Content from the repository
Projects, blog posts and certifications can also be added as Markdown files in `content/` through a pull request. See [content.md](./content.md).

#### Accounts
Registration is closed. The owner creates admin users and assigns roles in the Cloud IAM console, see [ciam.md](./ciam.md#7-create-admin-users).

#### Role-Based Access Control (RBAC)

The system implements fine-grained permissions through five realm roles:

- **`Admin.View`**: Access admin area, overview, statistics and settings
- **`Blogs.Contributor`**: Create, edit, and delete blog posts
- **`Projects.Contributor`**: Create, edit, and delete projects
- **`Certifications.Contributor`**: Create, edit, and delete certifications
- **`MyZen.Owner`**: The owner's MyZen section (owner only)

### Dashboard Features

The admin dashboard has a side menu with groups (issue #449). Each section has its own URL, so a link or a reload opens it directly (after sign-in if needed). The menu shows only the sections your roles open:

| Group | Section | URL | Role |
| --- | --- | --- | --- |
| | **Overview** | `/admin/dashboard` | `Admin.View` |
| Portfolio | **Blogs** | `/admin/dashboard/blogs` | `Blogs.Contributor` |
| Portfolio | **Projects** | `/admin/dashboard/projects` | `Projects.Contributor` |
| Portfolio | **Certifications** | `/admin/dashboard/certifications` | `Certifications.Contributor` |
| Statistics | **Visitors** | `/admin/dashboard/visitors` | `Admin.View` |
| Administration | **MyZen** | `/admin/dashboard/myzen` | `MyZen.Owner`, while the `MyZen` feature flag is on |
| Administration | **Settings** | `/admin/dashboard/settings` | `Admin.View` |

Without `Admin.View`, `/admin/dashboard` opens the first section you can use. Layout and how to add a section: [UX: admin navigation](./ux.md#admin-navigation).

### Security Features
- Sign-in with the authorization code flow and PKCE: Keycloak (Cloud IAM) shows the login page, including MFA; the site never sees a password (issue #257)
- Tokens are kept in memory only, never in localStorage; after a reload the Keycloak session signs the admin in again without a prompt
- Tokens issued by Cloud IAM with embedded role claims
- Backend validates tokens using Cloud IAM's public keys
- No access for users without assigned roles

### Protected Routes
Every create, update and delete endpoint, and every MyZen and statistics read, needs a valid token with the matching role. The full list is in [Architecture](./architecture.md#api-endpoints).

## MyZen time registration

The owner can connect his own MyZen (TrustTeam / Billit) account from the admin dashboard, see his MyZen projects and time entries on clercq.it, and add, edit and delete entries one at a time. He types MyZen's CAPTCHA and MFA code himself; the site keeps the session and shows **Reconnect MyZen** when MyZen ends it. Owner only (`MyZen.Owner`), behind the `MyZen` feature flag. Details: [MyZen](./myzen.md).
