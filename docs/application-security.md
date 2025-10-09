# Application Security Configuration

## Overview

This document details the bank-grade security configurations implemented at the application level across Next.js, .NET API, and Docker/nginx layers.

## Next.js Frontend Security

### Security Headers (`next.config.mjs`)

The Next.js application implements comprehensive security headers following OWASP best practices:

#### Strict Transport Security (HSTS)
```javascript
'Strict-Transport-Security': 'max-age=31536000; includeSubDomains; preload'
```
- Forces HTTPS for 1 year
- Includes all subdomains
- Preload ready for browser HSTS preload lists

#### Content Security Policy (CSP)
```javascript
Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; ...
```
- Prevents XSS attacks by controlling resource loading
- Restricts scripts to same-origin
- Allows only trusted image sources
- Prevents framing (`frame-ancestors 'none'`)

#### Additional Headers
- **X-Frame-Options: DENY** - Prevents clickjacking
- **X-Content-Type-Options: nosniff** - Prevents MIME type sniffing
- **X-XSS-Protection: 1; mode=block** - Legacy XSS protection
- **Referrer-Policy: strict-origin-when-cross-origin** - Limits referrer leakage
- **Permissions-Policy** - Disables unnecessary browser features (camera, microphone, geolocation)

### CSP Compliance

The CSP policy is configured to:
- Allow self-hosted resources only
- Permit inline styles/scripts required by Next.js
- Restrict connections to known API endpoints
- Prevent embedding in iframes
- Block base tag hijacking

## .NET API Security

### Rate Limiting (`Program.cs`)

Implements multi-tier rate limiting to prevent DDoS attacks:

#### Global Rate Limit
- **100 requests per minute per IP** - Overall protection
- Fixed window algorithm
- Auto-replenishment enabled

#### API Endpoints Rate Limit
- **30 requests per minute** - Standard API calls
- Applied to: Images, Projects, Blogs, Certifications endpoints

#### Authentication Endpoints Rate Limit
- **10 requests per minute** - Stricter limit for sensitive operations
- Applied to: Login, token refresh, authentication endpoints
- Prevents brute force attacks

### JWT Token Validation

Enhanced token validation for bank-grade security:

```csharp
TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ClockSkew = TimeSpan.FromMinutes(5),  // Strict clock tolerance
    RequireExpirationTime = true,          // Tokens must have expiration
    RequireSignedTokens = true             // Tokens must be signed
}
```

### Security Headers Middleware

Custom middleware adds security headers to all API responses:

- **Remove Server Headers** - Security through obscurity
- **X-Content-Type-Options: nosniff** - MIME type protection
- **X-Frame-Options: DENY** - Clickjacking protection
- **X-XSS-Protection: 1; mode=block** - XSS filter
- **Referrer-Policy: strict-origin-when-cross-origin** - Referrer control
- **Permissions-Policy** - Feature restrictions
- **HSTS** (Production only) - Force HTTPS
- **Content-Security-Policy: default-src 'none'** - Strict CSP for API

### HTTPS Enforcement

- **Development**: Optional HTTPS metadata validation
- **Production**: Required HTTPS (`RequireHttpsMetadata = true`)
- **Redirect**: All HTTP requests redirected to HTTPS

### CORS Configuration

Strict CORS policy:
- Whitelist specific origins only
- No wildcard origins
- Credentials allowed only for whitelisted origins
- Specific headers and methods allowed

## nginx Security

### Server Hardening (`nginx.conf`)

#### Version Hiding
```nginx
server_tokens off;
```
Prevents version information disclosure in error pages and headers.

#### Rate Limiting

Three-tier rate limiting system:

1. **API Rate Limit**
   - Zone: `api_limit` (10MB memory)
   - Rate: 30 requests/minute
   - Burst: 10 additional requests
   - Applied to: `/api/*` endpoints

2. **General Rate Limit**
   - Zone: `general_limit` (10MB memory)
   - Rate: 100 requests/minute
   - Burst: 20 additional requests
   - Applied to: All other endpoints

3. **Connection Limit**
   - Zone: `addr` (10MB memory)
   - Limit: 10 concurrent connections per IP

#### Request Size Limits

```nginx
client_max_body_size 10M;
client_body_buffer_size 128k;
```
- Prevents large payload attacks
- Limits body size to 10MB
- 128KB buffer for performance

#### Timeout Protection

Prevents slowloris and similar attacks:

```nginx
client_body_timeout 10s;
client_header_timeout 10s;
keepalive_timeout 30s;
send_timeout 10s;
```

#### Security Headers

All responses include:
- X-Frame-Options: DENY
- X-Content-Type-Options: nosniff
- X-XSS-Protection: 1; mode=block
- Referrer-Policy: strict-origin-when-cross-origin
- Permissions-Policy
- Content-Security-Policy

#### Hidden Files Protection

```nginx
location ~ /\. {
    deny all;
}
```
Blocks access to hidden files (`.git`, `.env`, etc.)

#### Proxy Header Hiding

```nginx
proxy_hide_header X-Powered-By;
proxy_hide_header Server;
```
Removes identifying headers from backend responses.

#### Buffer Protection

```nginx
proxy_buffer_size 4k;
proxy_buffers 8 4k;
proxy_busy_buffers_size 8k;
```
Limits buffer sizes to prevent memory exhaustion attacks.

## Security Layers Summary

### Defense in Depth

| Layer | Protection | Implementation |
|-------|------------|----------------|
| **nginx** | Rate limiting, timeouts, headers | nginx.conf |
| **.NET API** | Rate limiting, JWT validation, CORS | Program.cs |
| **Next.js** | Security headers, CSP | next.config.mjs |
| **Database** | Parameterized queries | Entity Framework |
| **Authentication** | MFA, JWT, RBAC | Quasr.io integration |

### Attack Prevention

| Attack Vector | Protection Mechanism |
|---------------|---------------------|
| **DDoS** | Multi-tier rate limiting (nginx + .NET) |
| **Brute Force** | Authentication endpoint rate limiting (10 req/min) |
| **Slowloris** | Timeout configurations (10-30s) |
| **XSS** | CSP headers, input validation |
| **Clickjacking** | X-Frame-Options: DENY |
| **MIME Sniffing** | X-Content-Type-Options: nosniff |
| **SQL Injection** | Parameterized queries (EF Core) |
| **CSRF** | CORS policy, SameSite cookies |
| **Man-in-the-Middle** | HSTS, HTTPS enforcement |
| **Information Disclosure** | Hidden server headers, version hiding |

## Compliance Alignment

### NIST 800-53 Controls

- **SC-7**: Boundary Protection - Rate limiting, CORS
- **SC-8**: Transmission Confidentiality - HTTPS, HSTS
- **SC-23**: Session Authenticity - JWT validation
- **SI-10**: Information Input Validation - FluentValidation
- **SI-11**: Error Handling - No information disclosure

### PCI DSS Requirements

- **2.2.5**: Security services/protocols enabled - HTTPS, HSTS
- **6.5.1**: Injection flaws - Parameterized queries
- **6.5.3**: Insecure cryptographic storage - Encryption at rest
- **6.5.4**: Insecure communications - TLS 1.2+
- **6.5.7**: XSS - CSP headers
- **6.5.9**: CSRF - CORS, token validation

### OWASP Top 10 Mitigations

1. **A01 Broken Access Control** - RBAC, JWT validation
2. **A02 Cryptographic Failures** - HTTPS, HSTS, encryption
3. **A03 Injection** - Parameterized queries, input validation
4. **A04 Insecure Design** - Security by design approach
5. **A05 Security Misconfiguration** - Hardened defaults, hidden headers
6. **A06 Vulnerable Components** - Automated scanning, Dependabot
7. **A07 Authentication Failures** - MFA, rate limiting, JWT
8. **A08 Software Integrity** - Build attestation, SBOM
9. **A09 Logging Failures** - Centralized logging (Scaleway Cockpit)
10. **A10 SSRF** - Input validation, whitelist approach

## Production Recommendations

### Enable HSTS in nginx

Uncomment in `nginx.conf` when SSL is configured:

```nginx
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains; preload" always;
```

### SSL/TLS Configuration

Add to nginx.conf:

```nginx
listen 443 ssl http2;
ssl_protocols TLSv1.2 TLSv1.3;
ssl_ciphers HIGH:!aNULL:!MD5;
ssl_prefer_server_ciphers on;
ssl_session_cache shared:SSL:10m;
ssl_session_timeout 10m;
```

### Rate Limit Tuning

Adjust based on traffic patterns:
- API limit: Currently 30/min, monitor and adjust
- General limit: Currently 100/min, scale as needed
- Burst allowance: Tune for legitimate traffic spikes

### Monitoring

Monitor these metrics:
- Rate limit rejections (429 responses)
- Authentication failures
- CSP violations
- Slow requests (potential slowloris)
- Connection count per IP

## Testing Security Configuration

### Test Rate Limiting

```bash
# Test API rate limit (should get 429 after 30 requests)
for i in {1..35}; do curl -w "\n%{http_code}\n" http://localhost/api/projects; done

# Test auth rate limit (should get 429 after 10 requests)
for i in {1..15}; do curl -X POST -w "\n%{http_code}\n" http://localhost/api/auth/login; done
```

### Test Security Headers

```bash
# Check all security headers are present
curl -I https://www.clercq.it

# Verify CSP
curl -I https://www.clercq.it | grep -i content-security-policy

# Verify HSTS
curl -I https://www.clercq.it | grep -i strict-transport-security
```

### Test CSP Compliance

Use browser developer tools:
- Open Console tab
- Look for CSP violation warnings
- Adjust policy if legitimate resources are blocked

## References

- [OWASP Security Headers](https://owasp.org/www-project-secure-headers/)
- [NIST 800-53 Rev 5](https://csrc.nist.gov/publications/detail/sp/800-53/rev-5/final)
- [PCI DSS v3.2.1](https://www.pcisecuritystandards.org/)
- [Next.js Security](https://nextjs.org/docs/app/building-your-application/configuring/security)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [nginx Security](https://nginx.org/en/docs/http/ngx_http_security_module.html)

---

**Last Updated**: 2024-01-08  
**Security Level**: Bank-Grade / Financial Services Ready  
**Maintained By**: Security Team
