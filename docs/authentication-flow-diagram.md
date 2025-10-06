# Authentication Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         AUTHENTICATION FLOW                                  │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────┐
│   Browser    │
│  /admin      │
└──────┬───────┘
       │
       │ 1. User enters Scaleway credentials
       │    Access Key: SCWXXXXXXXXXXXXXXXXX
       │    Secret Key: **********************
       │
       ▼
┌──────────────────────┐
│  Next.js Frontend    │
│  /api/auth/login     │
└──────┬───────────────┘
       │
       │ 2. POST to backend API
       │    { accessKey, secretKey }
       │
       ▼
┌──────────────────────────────────────┐
│  .NET API                            │
│  /api/auth/token                     │
│                                      │
│  ┌────────────────────────────────┐ │
│  │ TokenService                   │ │
│  │ .ValidateScalewayCredentials() │ │
│  └────────┬───────────────────────┘ │
└───────────┼──────────────────────────┘
            │
            │ 3. Validate against Scaleway
            │    GET https://api.scaleway.com/iam/v1alpha1/api-keys
            │    Header: X-Auth-Token: {secretKey}
            │
            ▼
     ┌──────────────────┐
     │  Scaleway IAM    │
     │  API             │
     └──────┬───────────┘
            │
            │ 4a. If VALID (200)
            │     Returns user info
            │
            ▼
┌──────────────────────────────────────┐
│  .NET API                            │
│  TokenService.GenerateToken()        │
│                                      │
│  Creates JWT with claims:            │
│  - sub: {accessKey}                  │
│  - email: {accessKey}@scaleway       │
│  - role: admin                       │
│  - exp: {60 min from now}            │
│                                      │
│  Signs with JwtSecretKey             │
└──────┬───────────────────────────────┘
       │
       │ 5. Return JWT token
       │    { token: "eyJhbGci...", expiresAt: "..." }
       │
       ▼
┌──────────────────────┐
│  Next.js Frontend    │
│                      │
│  localStorage.setItem│
│  ("admin_token", jwt)│
└──────┬───────────────┘
       │
       │ 6. Redirect to dashboard
       │    /admin/dashboard
       │
       ▼
┌──────────────────────┐
│  Dashboard Page      │
│                      │
│  useEffect(() => {   │
│    const token =     │
│      localStorage    │
│        .getItem()    │
│    if (!token)       │
│      redirect()      │
│  })                  │
└──────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                     PROTECTED API REQUEST FLOW                               │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────┐
│  Dashboard   │
│  Create Blog │
└──────┬───────┘
       │
       │ 1. User clicks "Create Blog"
       │
       ▼
┌──────────────────────┐
│  Frontend API Call   │
│                      │
│  fetch("/api/blogs", │
│    headers: {        │
│      Authorization:  │
│       "Bearer " +    │
│       token          │
│    }                 │
│  )                   │
└──────┬───────────────┘
       │
       │ 2. Request with JWT
       │    Authorization: Bearer eyJhbGci...
       │
       ▼
┌──────────────────────────────────────┐
│  .NET API                            │
│  POST /api/blogs                     │
│  [RequireAuthorization]              │
│                                      │
│  ┌────────────────────────────────┐ │
│  │ JWT Authentication Middleware  │ │
│  │                                │ │
│  │ 1. Extract token from header   │ │
│  │ 2. Verify signature with       │ │
│  │    JwtSecretKey                │ │
│  │ 3. Check expiration            │ │
│  │ 4. Validate issuer/audience    │ │
│  │ 5. Extract claims              │ │
│  └────────┬───────────────────────┘ │
└───────────┼──────────────────────────┘
            │
            │ 3a. If VALID
            │     Set User.Claims
            │     Allow request
            │
            ▼
     ┌──────────────────┐
     │  Blog Handler    │
     │  Create blog...  │
     └──────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                     CONFIGURATION REQUIREMENTS                               │
└─────────────────────────────────────────────────────────────────────────────┘

Backend (.NET API):
━━━━━━━━━━━━━━━━━━
  Environment Variables:
    ✓ Authentication__JwtSecretKey (REQUIRED)
    ✓ Authentication__Issuer (optional, default: "Clercq.It")
    ✓ Authentication__Audience (optional, default: "Clercq.It.Api")
    ✓ Authentication__ExpirationMinutes (optional, default: 60)

  NO Scaleway credentials needed!
  ────────────────────────────────

Frontend (Next.js):
━━━━━━━━━━━━━━━━━━
  .env.local:
    ✓ NEXT_PUBLIC_API_URL (API endpoint)

User Requirements:
━━━━━━━━━━━━━━━━━
  Scaleway IAM credentials from:
  https://console.scaleway.com/organization/credentials
    ✓ Access Key (SCW...)
    ✓ Secret Key


┌─────────────────────────────────────────────────────────────────────────────┐
│                     SECURITY FEATURES                                        │
└─────────────────────────────────────────────────────────────────────────────┘

✓ No stored credentials    - User creds validated per-request, never saved
✓ Real-time validation     - Every login checks Scaleway's live API
✓ JWT expiration           - Tokens expire (default 60 min)
✓ Signature verification   - JWT signed with secret key
✓ HTTPS in production      - All communications encrypted
✓ CORS restrictions        - Only allowed origins can call API
✓ Role-based claims        - JWT includes role: "admin"
✓ Audit trail              - All validation attempts logged
```
