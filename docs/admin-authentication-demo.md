# Admin Authentication - Configuration & Validation Guide

This guide demonstrates how the Scaleway IAM authentication works and what you need to configure.

## Configuration Requirements

### Required Environment Variable

Only **ONE** environment variable is required:

```bash
Authentication__JwtSecretKey=your-secret-key-here
```

This is used to sign the JWT tokens after successful Scaleway IAM validation.

**That's it!** No Scaleway credentials need to be stored in your application.

## How Authentication Works

### Step-by-Step Flow

1. **User enters credentials** at `/admin`:
   - Scaleway Access Key (e.g., `SCWXXXXXXXXXXXXXXXXX`)
   - Scaleway Secret Key (the secret token from Scaleway Console)

2. **Frontend sends credentials** to Next.js API route:
   ```
   POST /api/auth/login
   {
     "accessKey": "SCW...",
     "secretKey": "secret-key-here"
   }
   ```

3. **Next.js proxies to backend**:
   ```
   POST https://your-api/api/auth/token
   {
     "accessKey": "SCW...",
     "secretKey": "secret-key-here"
   }
   ```

4. **Backend validates against Scaleway**:
   - Makes HTTP request to `https://api.scaleway.com/iam/v1alpha1/api-keys`
   - Includes user's secret key in `X-Auth-Token` header
   - Scaleway responds with 200 (valid) or 403 (invalid)

5. **If valid, backend generates JWT**:
   ```json
   {
     "token": "eyJhbGci...",
     "expiresAt": "2024-01-01T12:00:00Z"
   }
   ```

6. **Frontend stores JWT in localStorage**:
   - Token is saved as `admin_token`
   - Used in Authorization header for subsequent API calls

7. **Protected API calls**:
   ```
   Authorization: Bearer eyJhbGci...
   ```

## Validating a User is Authenticated

### Backend Validation

All protected endpoints use the `[RequireAuthorization]` attribute:

```csharp
group.MapPost("/", async (HttpRequest request, IMediator mediator) =>
{
    // Handler code
})
.RequireAuthorization()  // ← This validates the JWT token
.WithName("CreateBlog");
```

The JWT is automatically validated by ASP.NET Core's authentication middleware:
- Checks signature using `Authentication:JwtSecretKey`
- Verifies issuer and audience
- Checks expiration time
- Extracts claims (user ID, email, role)

### Frontend Validation

The dashboard checks authentication on page load:

```typescript
useEffect(() => {
  // Check if user is authenticated
  const token = localStorage.getItem("admin_token");
  if (!token) {
    router.push("/admin"); // Redirect to login
  } else {
    setIsAuthenticated(true);
  }
}, [router]);
```

## Testing the Authentication

### Manual Test with cURL

1. **Get a valid token**:
```bash
curl -X POST http://localhost:5035/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "accessKey": "YOUR_SCALEWAY_ACCESS_KEY",
    "secretKey": "YOUR_SCALEWAY_SECRET_KEY"
  }'
```

Response if valid:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-01T13:00:00Z"
}
```

Response if invalid (401 Unauthorized):
```json
{
  "status": 401,
  "title": "Unauthorized"
}
```

2. **Use the token to call a protected endpoint**:
```bash
curl -X POST http://localhost:5035/api/blogs \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: multipart/form-data" \
  -F "shortDescription=Test Blog" \
  -F "longDescription=# Test Content" \
  -F "tags=test" \
  -F "image=@image.jpg"
```

### Automated Tests

The test suite includes authentication validation:

```csharp
[Fact]
public async Task AuthToken_WithInvalidCredentials_ReturnsUnauthorized()
{
    // Arrange
    var content = new StringContent(
        "{\"accessKey\":\"INVALID_KEY\",\"secretKey\":\"invalid_secret\"}",
        System.Text.Encoding.UTF8,
        "application/json");

    // Act
    var response = await _client.PostAsync("/api/auth/token", content);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

## Verifying Scaleway Credentials Are Used

### How to Confirm It Works

1. **Create IAM API Key in Scaleway Console**:
   - Go to https://console.scaleway.com/
   - Navigate to **Organization** → **IAM** → **API Keys**
   - Click "Generate API key"
   - Copy both Access Key and Secret Key

2. **Try to login with those credentials**:
   - Navigate to `http://localhost:3000/admin`
   - Enter your Scaleway Access Key
   - Enter your Scaleway Secret Key
   - Click "Sign In"

3. **Check the logs** to see the validation:
   ```
   info: TokenService[0]
         Successfully validated Scaleway IAM credentials
   ```

4. **Try with invalid credentials**:
   - Use wrong secret key
   - You'll see:
   ```
   warn: TokenService[0]
         Failed to validate Scaleway credentials. Status: 403
   ```

## Security Features

### What Makes This Secure?

1. **No stored credentials**: User credentials are validated per-request, never stored
2. **Real-time validation**: Every login validates against Scaleway's live API
3. **JWT expiration**: Tokens expire after 60 minutes (configurable)
4. **HTTPS only**: In production, all communications are encrypted
5. **CORS restrictions**: Only specific origins can call the API
6. **No credential leakage**: Scaleway credentials are never logged or persisted

### JWT Token Claims

The generated JWT includes:
```json
{
  "sub": "SCWXXXXXXXXXXXXXXXXX",  // User's access key
  "email": "SCWXXXXXXXXXXXXXXXXX@scaleway",
  "jti": "unique-token-id",
  "role": "admin",
  "iss": "Clercq.It",
  "aud": "Clercq.It.Api",
  "exp": 1704110400  // Expiration timestamp
}
```

## Troubleshooting

### "JWT secret key is not configured"

Set the environment variable:
```bash
export Authentication__JwtSecretKey="your-secret-key-minimum-32-chars"
```

Or in `appsettings.json`:
```json
{
  "Authentication": {
    "JwtSecretKey": "your-secret-key-minimum-32-chars"
  }
}
```

### "Failed to validate Scaleway credentials"

Possible causes:
- Invalid secret key
- Scaleway API is down
- Network connectivity issues
- API key doesn't have required permissions

Check logs for the specific HTTP status code from Scaleway.

### User gets redirected to login after successful authentication

Check:
1. JWT is being stored in localStorage
2. Token hasn't expired (60 minutes default)
3. Browser allows localStorage for the domain

## Production Configuration

### Recommended Settings

```json
{
  "Authentication": {
    "JwtSecretKey": "use-a-strong-random-key-at-least-32-characters-long",
    "Issuer": "Clercq.It",
    "Audience": "Clercq.It.Api",
    "ExpirationMinutes": 60
  }
}
```

Set via environment variables:
```bash
Authentication__JwtSecretKey="your-production-secret"
Authentication__ExpirationMinutes=60
```

### Security Checklist

- [ ] Strong JWT secret key (min 32 chars, random)
- [ ] HTTPS enabled
- [ ] CORS restricted to production domain
- [ ] Token expiration set appropriately
- [ ] Scaleway IAM API accessible from production
- [ ] Proper error handling (don't leak sensitive info in logs)

## Summary

### What You Need to Configure

**Just one thing**: `Authentication__JwtSecretKey`

### What Users Need

Their Scaleway IAM credentials (Access Key + Secret Key) from:
https://console.scaleway.com/organization/credentials

### How to Validate Authentication

1. **Frontend**: Check for JWT token in localStorage
2. **Backend**: Use `.RequireAuthorization()` on endpoints
3. **Testing**: Use cURL to test token generation and usage
4. **Monitoring**: Check logs for validation success/failure

The system automatically handles all Scaleway IAM validation - no additional configuration needed!
