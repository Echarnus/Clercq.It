# Blog Creation API - Usage Examples

This document provides examples of how to use the new blog creation API endpoint.

## Prerequisites

1. **Authentication Token**: You need a valid JWT token to create blogs
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

### GET /api/blogs

Retrieve all blogs from the system.

**Request:**
```bash
curl -X GET https://localhost:7000/api/blogs
```

**Response:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "createdDate": "2024-01-15T10:30:00Z",
    "publishDate": "2024-01-15T10:30:00Z",
    "shortDescription": "Introduction to Clean Architecture",
    "longDescription": "# Clean Architecture\n\nA comprehensive guide...",
    "image": "https://s3.fr-par.scw.cloud/clercq-it-blog-images/blog-images/...",
    "tags": ["Architecture", "Development", "Best Practices"]
  }
]
```

### POST /api/blogs

Create a new blog post with an image (requires authentication).

**Request:**
```bash
curl -X POST https://localhost:7000/api/blogs \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "shortDescription=Introduction to Clean Architecture" \
  -F "longDescription=# Clean Architecture\n\nA comprehensive guide to building maintainable applications..." \
  -F "tags=Architecture,Development,Best Practices" \
  -F "image=@/path/to/your/image.jpg"
```

**Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "createdDate": "2024-01-15T10:30:00Z",
  "publishDate": "2024-01-15T10:30:00Z",
  "shortDescription": "Introduction to Clean Architecture",
  "longDescription": "# Clean Architecture\n\nA comprehensive guide to building maintainable applications...",
  "image": "https://s3.fr-par.scw.cloud/clercq-it-blog-images/blog-images/3fa85f64-.../image.jpg",
  "tags": ["Architecture", "Development", "Best Practices"]
}
```

## Using Postman

### 1. Create a new POST request

- **URL**: `https://localhost:7000/api/blogs`
- **Method**: POST
- **Headers**:
  - `Authorization`: `Bearer YOUR_JWT_TOKEN`

### 2. Configure Body

- Select **form-data**
- Add the following fields:

| Key | Type | Value |
|-----|------|-------|
| shortDescription | Text | Your short description (max 500 chars) |
| longDescription | Text | Your markdown content |
| tags | Text | Comma-separated tags (e.g., "C#,Testing,Development") |
| image | File | Select your image file |

### 3. Send Request

Click "Send" to create the blog post.

## Using HTTPie

```bash
# Install HTTPie
pip install httpie

# Create a blog
http -f POST https://localhost:7000/api/blogs \
  "Authorization: Bearer YOUR_JWT_TOKEN" \
  shortDescription="Introduction to Clean Architecture" \
  longDescription="# Clean Architecture\n\nA comprehensive guide..." \
  tags="Architecture,Development,Best Practices" \
  image@/path/to/your/image.jpg
```

## Validation Rules

The API validates the following:

### Short Description
- **Required**: Yes
- **Maximum Length**: 500 characters

### Long Description (Markdown)
- **Required**: Yes
- **Maximum Length**: 50,000 characters
- **Format**: Markdown (supports headers, lists, code blocks, links, etc.)

### Image
- **Required**: Yes
- **Allowed Extensions**: .jpg, .jpeg, .png, .gif, .webp
- **Content Type**: Must be `image/*`

### Tags
- **Required**: Yes (at least one tag)
- **Maximum**: 10 tags
- **Format**: Comma-separated string

## Error Responses

### 400 Bad Request - Validation Error

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "ShortDescription": [
      "Short description is required"
    ],
    "ImageFileName": [
      "Invalid image file extension. Only jpg, jpeg, png, gif, and webp are allowed"
    ]
  }
}
```

### 401 Unauthorized - Missing or Invalid Token

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

### 500 Internal Server Error - Object Storage Not Configured

```json
{
  "error": "Object storage is not configured. Please configure ObjectStorage settings in appsettings.json"
}
```

## Markdown Examples

The `longDescription` field supports full Markdown syntax:

```markdown
# Main Heading

This is a blog post about **Clean Architecture**.

## What is Clean Architecture?

Clean Architecture is a software design philosophy that separates concerns:

1. Domain Layer
2. Application Layer
3. Infrastructure Layer
4. Presentation Layer

### Code Example

\`\`\`csharp
public class Blog : IAggregateRoot
{
    public Guid Id { get; set; }
    public string ShortDescription { get; set; }
    public string LongDescription { get; set; }
}
\`\`\`

### Links

For more information, visit [Microsoft Docs](https://docs.microsoft.com).

### Images

You can reference images in your markdown:

![Architecture Diagram](https://example.com/diagram.png)
```

## Testing Without Authentication

For local testing, you can temporarily disable authentication by commenting out the `.RequireAuthorization()` line in `BlogsEndpoints.cs`. **Do not deploy without authentication enabled!**

## Notes

- Blog images are uploaded to Scaleway Object Storage with public-read permissions
- Images are stored with a unique GUID path to prevent naming collisions
- The returned image URL points directly to the object storage location
- Markdown content is stored as-is and should be rendered by the frontend
