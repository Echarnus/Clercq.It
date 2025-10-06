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

### POST /api/projects

Create a new project with an image (requires authentication).

**Request:**
```bash
curl -X POST https://localhost:7000/api/projects \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "title=E-commerce Platform" \
  -F "shortDescription=Modern web application with microservices architecture" \
  -F "longDescription=# Project Overview\n\nA comprehensive web application built with Clean Architecture..." \
  -F "startDate=2023-06-01T00:00:00Z" \
  -F "endDate=2023-12-31T00:00:00Z" \
  -F "featured=true" \
  -F "skills=C#,.NET,React,TypeScript,Docker" \
  -F "image=@/path/to/your/image.jpg"
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
| startDate | Text | ISO 8601 date format (e.g., "2023-06-01T00:00:00Z") |
| endDate | Text | ISO 8601 date format (e.g., "2023-12-31T00:00:00Z") |
| featured | Text | true or false (defaults to false if not provided) |
| skills | Text | Comma-separated skills (e.g., "C#,.NET,React") |
| image | File | Select your image file |

### 3. Send Request

Click "Send" to create the project.

## Using HTTPie

```bash
# Install HTTPie
pip install httpie

# Create a project
http -f POST https://localhost:7000/api/projects \
  "Authorization: Bearer YOUR_JWT_TOKEN" \
  title="E-commerce Platform" \
  shortDescription="Modern web application with microservices architecture" \
  longDescription="# Project Overview\n\nA comprehensive web application..." \
  startDate="2023-06-01T00:00:00Z" \
  endDate="2023-12-31T00:00:00Z" \
  featured="true" \
  skills="C#,.NET,React,TypeScript,Docker" \
  image@/path/to/your/image.jpg
```

## Using PowerShell

```powershell
# Create multipart form data
$form = @{
    title = "E-commerce Platform"
    shortDescription = "Modern web application with microservices architecture"
    longDescription = "# Project Overview`n`nA comprehensive web application..."
    startDate = "2023-06-01T00:00:00Z"
    endDate = "2023-12-31T00:00:00Z"
    featured = "true"
    skills = "C#,.NET,React,TypeScript,Docker"
    image = Get-Item -Path "C:\path\to\your\image.jpg"
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
- `image`: Valid image file (jpg, jpeg, png, gif, webp)
- `startDate`: Valid date in ISO 8601 format
- `endDate`: Valid date in ISO 8601 format, must be >= startDate
- `skills`: At least one skill, max 20 skills

### Optional Fields
- `featured`: Defaults to false if not provided

## Notes

- Project images are uploaded to Scaleway Object Storage with public-read permissions
- Images are stored with a unique GUID path to prevent naming collisions
- The returned image URL points directly to the object storage location
- Markdown content in `longDescription` is stored as-is and should be rendered by the frontend
- Skills are provided as a comma-separated string and stored as an array
- Dates should be in ISO 8601 format (e.g., "2023-06-01T00:00:00Z")
