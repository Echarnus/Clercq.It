# Project Creation API - Usage Examples

This document provides examples of how to use the new project creation API endpoint.

## Prerequisites

1. **Authentication Token**: You need a valid JWT token to create projects
2. **Object Storage**: Scaleway Object Storage must be configured
3. **Valid Image File**: Supported formats are JPG, JPEG, PNG, GIF, WebP

## Configuration

### Environment Variables

Make sure the following configuration is set in your `appsettings.json` or environment variables:

```json
{
  "ObjectStorage": {
    "Endpoint": "https://s3.fr-par.scw.cloud",
    "BucketName": "clercq-it-blog-images",
    "Region": "fr-par",
    "AccessKey": "your-access-key",
    "SecretKey": "your-secret-key"
  },
  "Authentication": {
    "JwtSecretKey": "your-secret-key-min-32-characters",
    "Issuer": "Clercq.It",
    "Audience": "Clercq.It.Api",
    "ExpirationMinutes": 60
  }
}
```

## API Endpoints

### GET /api/projects

Retrieve all projects from the system.

**Request:**
```bash
curl -X GET https://localhost:7000/api/projects
```

**Response:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "startDate": "2023-06-01T00:00:00Z",
    "endDate": "2023-12-31T00:00:00Z",
    "shortDescription": "Modern web application",
    "longDescription": "# Project Overview\n\nA comprehensive web application...",
    "image": "https://s3.fr-par.scw.cloud/clercq-it-blog-images/project-images/...",
    "featured": true,
    "title": "E-commerce Platform",
    "skills": ["C#", ".NET", "React", "TypeScript", "Docker"]
  }
]
```

### GET /api/projects/featured

Retrieve only featured projects from the system.

**Request:**
```bash
curl -X GET https://localhost:7000/api/projects/featured
```

### POST /api/projects/images

Upload a project image (requires authentication).

**Request:**
```bash
curl -X POST https://localhost:7000/api/projects/images \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "image=@/path/to/your/image.jpg"
```

**Response:**
```json
{
  "url": "https://s3.fr-par.scw.cloud/clercq-it-blog-images/project-images/3fa85f64-.../image.jpg"
}
```

### POST /api/projects

Create a new project with an image URL (requires authentication).

**Request:**
```bash
curl -X POST https://localhost:7000/api/projects \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "title=E-commerce Platform" \
  -F "shortDescription=Modern web application with microservices architecture" \
  -F "longDescription=# Project Overview\n\nA comprehensive web application built with Clean Architecture..." \
  -F "imageUrl=https://s3.fr-par.scw.cloud/clercq-it-blog-images/project-images/3fa85f64-.../image.jpg" \
  -F "startDate=2023-06-01T00:00:00Z" \
  -F "endDate=2023-12-31T00:00:00Z" \
  -F "featured=true" \
  -F "skills=C#,.NET,React,TypeScript,Docker"
```

**Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "startDate": "2023-06-01T00:00:00Z",
  "endDate": "2023-12-31T00:00:00Z",
  "shortDescription": "Modern web application with microservices architecture",
  "longDescription": "# Project Overview\n\nA comprehensive web application built with Clean Architecture...",
  "image": "https://s3.fr-par.scw.cloud/clercq-it-blog-images/project-images/3fa85f64-.../image.jpg",
  "featured": true,
  "title": "E-commerce Platform",
  "skills": ["C#", ".NET", "React", "TypeScript", "Docker"]
}
```

## Workflow

Creating a project is a two-step process:

1. **Upload the project image** using `POST /api/projects/images`
2. **Create the project** using `POST /api/projects` with the returned image URL

This separation allows for better control over image uploads and enables reuse of images if needed.

## Using Postman

### 1. Create a new POST request

- **URL**: `https://localhost:7000/api/projects`
- **Method**: POST
- **Headers**:
  - `Authorization`: `Bearer YOUR_JWT_TOKEN`

### 2. Configure Body

- Select **form-data**
- Add the following fields:

| Key | Type | Value |
|-----|------|-------|
| title | Text | Your project title (max 200 chars) |
| shortDescription | Text | Your short description (max 500 chars) |
| longDescription | Text | Your markdown content |
| imageUrl | Text | URL from /api/projects/images upload |
| startDate | Text | ISO 8601 date format (e.g., "2023-06-01T00:00:00Z") |
| endDate | Text | ISO 8601 date format (e.g., "2023-12-31T00:00:00Z") |
| featured | Text | true or false (defaults to false if not provided) |
| skills | Text | Comma-separated skills (e.g., "C#,.NET,React") |

### 3. Send Request

Click "Send" to create the project.

## Using HTTPie

```bash
# Install HTTPie
pip install httpie

# Step 1: Upload the image
http -f POST https://localhost:7000/api/projects/images \
  "Authorization: Bearer YOUR_JWT_TOKEN" \
  image@/path/to/your/image.jpg

# Step 2: Create the project with the returned image URL
http -f POST https://localhost:7000/api/projects \
  "Authorization: Bearer YOUR_JWT_TOKEN" \
  title="E-commerce Platform" \
  shortDescription="Modern web application with microservices architecture" \
  longDescription="# Project Overview\n\nA comprehensive web application..." \
  imageUrl="https://s3.fr-par.scw.cloud/clercq-it-blog-images/project-images/..." \
  startDate="2023-06-01T00:00:00Z" \
  endDate="2023-12-31T00:00:00Z" \
  featured="true" \
  skills="C#,.NET,React,TypeScript,Docker"
```

## Using PowerShell

```powershell
# Step 1: Upload the image
$imageHeaders = @{
    Authorization = "Bearer YOUR_JWT_TOKEN"
}

$imageForm = @{
    image = Get-Item -Path "C:\path\to\your\image.jpg"
}

$imageResponse = Invoke-RestMethod -Uri "https://localhost:7000/api/projects/images" `
    -Method Post `
    -Form $imageForm `
    -Headers $imageHeaders

# Step 2: Create the project with the returned image URL
$form = @{
    title = "E-commerce Platform"
    shortDescription = "Modern web application with microservices architecture"
    longDescription = "# Project Overview`n`nA comprehensive web application..."
    imageUrl = $imageResponse.url
    startDate = "2023-06-01T00:00:00Z"
    endDate = "2023-12-31T00:00:00Z"
    featured = "true"
    skills = "C#,.NET,React,TypeScript,Docker"
}

# Set headers
$headers = @{
    Authorization = "Bearer YOUR_JWT_TOKEN"
}

# Send request
Invoke-RestMethod -Uri "https://localhost:7000/api/projects" `
    -Method Post `
    -Form $form `
    -Headers $headers
```

## Field Validation

### Required Fields
- `title`: Must not be empty, max 200 characters
- `shortDescription`: Must not be empty, max 500 characters
- `longDescription`: Must not be empty, max 50,000 characters (markdown content)
- `imageUrl`: Must be a valid HTTP/HTTPS URL (obtained from `/api/projects/images`)
- `startDate`: Valid date in ISO 8601 format
- `endDate`: Valid date in ISO 8601 format, must be >= startDate
- `skills`: At least one skill, max 20 skills

### Optional Fields
- `featured`: Defaults to false if not provided

## Notes

- Project images must be uploaded separately via `/api/projects/images` before creating the project
- Images are uploaded to Scaleway Object Storage with public-read permissions
- Images are stored with a unique GUID path to prevent naming collisions
- The returned image URL from the upload endpoint should be used in the `imageUrl` field when creating the project
- Markdown content in `longDescription` is stored as-is and should be rendered by the frontend
- Skills are provided as a comma-separated string and stored as an array
- Dates should be in ISO 8601 format (e.g., "2023-06-01T00:00:00Z")
