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
- Applied to: Images, Projects, Blogs, Certifications, Auth (`/api/auth/config`), Features (`/api/features`), MyZen, MCP and the statistics summary (`GET /api/stats`). `POST /api/stats/pageview` only has the global limit.

Both limits are partitioned by the connection's remote address. In the container every request reaches the API from nginx or from Next.js on `localhost`, and the API does not read `X-Forwarded-For`, so in production all visitors currently share one bucket (#438).

#### Admin sign-in (authorization code + PKCE)
- The admin UI signs in with the OAuth 2.1 authorization code flow and PKCE (S256) against Keycloak, using `oidc-client-ts` (`src/ClercqIt.Web/lib/auth.ts`, issue #257). Keycloak's own login page handles passwords, MFA, brute-force protection and required actions.
- No API endpoint accepts a password: `POST /api/auth/login` and the password grant are gone, and `clercqit-web` has **Direct access grants** off. `GET /api/auth/config` only returns the issuer URL and the public client id.
- Tokens stay in memory (never `localStorage`); only the PKCE verifier and state sit in `sessionStorage` during the redirect. The return address after sign-in must be an `/admin` path.
- The CSP `connect-src` allows `https://*.cloud-iam.com`, so the browser can call Keycloak's token endpoint.

#### Closed Registration
- There is no self-registration. The API has no register or social-login endpoints, the Next.js app has no `/admin/register` page, and the `clercqit` realm has `registrationAllowed: false`.
- Admin users are created by the owner in the Cloud IAM console (see [ciam.md](./ciam.md#7-create-admin-users)).
- `AuthEndpointTests` checks that the removed endpoints stay gone.

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

## Static analysis (SAST)

With no human review, these checks are what stops insecure code reaching `main` (issue #237). Each one fails its check on a finding.

| Check | Workflow | Blocks on | Scope |
|-------|----------|-----------|-------|
| **Semgrep** | `sast.yml` | Any `ERROR` finding | Registry packs `p/csharp`, `p/typescript`, `p/react`, `p/nextjs`, `p/owasp-top-ten`, `p/secrets`, `p/dockerfile`, `p/terraform`, plus the repo rules in `.semgrep/` |
| **gitleaks** | `sast.yml` | Any secret | PR: the PR's commits. `main`: full history |
| **CodeQL gate** | `codeql.yml` | An open `high` or `critical` CodeQL alert on the PR's merge ref | C# and JS/TS, `security-extended` + `security-and-quality` |
| **Vulnerable NuGet packages** | `security-scan.yml` | High or Critical advisory | All projects in `Clercq.It.CI.slnf`, transitive included |
| **pnpm audit** | `security-scan.yml`, `test.yml` | High or Critical advisory | `src/ClercqIt.Web`, transitive included |
| **Trivy image** | `security-scan.yml` | Fixable High or Critical finding | The production Docker image (OS packages, Node, npm and .NET dependencies) |
| **Trivy filesystem** | `security-scan.yml` | Fixable High or Critical finding | Lockfiles, Dockerfile and Terraform in the repository |

Results from Semgrep, CodeQL and Trivy are also in the repository's **Security** tab.

### Repo rules (`.semgrep/clercq.yml`)

| Rule | Catches |
|------|---------|
| `no-tls-verification-bypass` | `NODE_TLS_REJECT_UNAUTHORIZED=0` anywhere |
| `efcore-raw-sql-interpolation` | `FromSqlRaw`/`ExecuteSqlRaw`/`SqlQueryRaw` with an interpolated, concatenated or `string.Format` string (use `FromSql`/`ExecuteSql`, which parameterise) |
| `no-dangerously-set-inner-html-without-sanitising` | `dangerouslySetInnerHTML` with anything but a literal or `DOMPurify.sanitize(...)` |

**Workflows**: Semgrep also checks `.github/` for shell injection. Never put `${{ github.* }}` or `${{ inputs.* }}` straight into a `run:` script; pass it through `env:` and quote the variable (`"$BRANCH"`).

### Run locally

```bash
pip install semgrep            # or: brew install semgrep
semgrep scan --config p/csharp --config p/typescript --config p/react --config p/nextjs \
  --config p/owasp-top-ten --config p/secrets --config p/dockerfile --config p/terraform \
  --config .semgrep --severity ERROR --error

gitleaks git . --log-opts="origin/main..HEAD"   # your branch's commits
dotnet list Clercq.It.CI.slnf package --vulnerable --include-transitive
```

### Suppressing a finding

Fix the code first. A suppression is only for a false positive or an accepted risk that has its own issue, and it always says why:

- **Semgrep**: a comment on the line above, with the reason on the line before it:
  ```ts
  // The CSS is built from the chart config in code, never from user input.
  // nosemgrep: semgrep.no-dangerously-set-inner-html-without-sanitising
  ```
  Whole paths go in `.semgrepignore`, each with a reason.
- **gitleaks**: add the finding's fingerprint (printed by gitleaks) to `.gitleaksignore` under a comment with the reason.
- **CodeQL**: dismiss the alert in the Security tab as "False positive" or "Won't fix", with a comment.
- **pnpm audit**: add the GHSA id to `pnpm.auditConfig.ignoreGhsas` in `src/ClercqIt.Web/package.json` and a row to [Accepted findings](#accepted-findings) (JSON has no comments).
- **Trivy**: add the CVE id to `.trivyignore` with a comment above it, and a row to [Accepted findings](#accepted-findings).

Never weaken a rule or a severity threshold to get a PR green.

### Current suppressions

| Where | What | Why |
|-------|------|-----|
| `.gitleaksignore` | 5 findings in old docs commits, 1 in the #237 probe commit | Placeholder `YOUR_JWT_TOKEN`, a truncated example JWT, and the random fake key that proved the gate fails |

### Accepted findings

Dependency advisories that block but have no fix, so they are ignored with a reason. Revisit each when a patched version appears (Dependabot proposes it).

| Where | Advisory | Package and path | Why it is accepted |
|-------|----------|------------------|--------------------|
| `pnpm.auditConfig.ignoreGhsas` | GHSA-vfj7-8cjw-p6xm (high) | `braces` via `eslint-config-next > @next/eslint-plugin-next > fast-glob > micromatch` | No patched version yet. Dev-only: it runs in ESLint on the repository's own glob patterns, never on user input and never in the production image. |

Moderate advisories are listed but do not block; `sprintf-js` (GHSA-hp3w-g68c-fv3c, via `gray-matter`, no fix yet) is one.

### What does not block yet

- **Moderate and low** dependency advisories (pnpm audit, Trivy) are reported, not blocking.
- **IaC scanning**: Checkov and Trivy have no Scaleway checks, so `infra/terraform` is covered only by Semgrep's `p/terraform` and by review of the Terraform plan on the PR.

### Repository settings (owner)

These are GitHub settings, not code. The owner turns them on once:

1. **Settings → Code security → Secret Protection**: enable it, and enable **Push protection**.
2. Apply the rulesets in `.github/rulesets/` as described in [contributing.md](./contributing.md#changing-a-rule). The `main` ruleset requires `Semgrep`, `gitleaks`, `CodeQL gate`, `.NET Security Scan`, `npm Security Audit`, `Trivy Container Scan` and `Trivy Filesystem Scan`.

### Workflow output

This repository is public, so workflow logs and job summaries are public too. Never print a secret or a sensitive Terraform output there. `deploy.yml` masks the sensitive outputs it reads (`::add-mask::`), and the `Terraform Output` step in `infra.yml` writes only the outputs without `sensitive = true` to the summary. Until #435 it wrote all of them with `terraform output -json`, which shows sensitive values in plain text; the exposed verify credentials were rotated and later deleted with the verify environment (#440; see [Secrets](./secrets.md#rotating-a-terraform-generated-credential)).

## Security Layers Summary

### Defense in Depth

| Layer | Protection | Implementation |
|-------|------------|----------------|
| **nginx** | Rate limiting, timeouts, headers | nginx.conf |
| **.NET API** | Rate limiting, JWT validation, CORS | Program.cs |
| **Next.js** | Security headers, CSP | next.config.mjs |
| **Database** | Parameterized queries | Entity Framework |
| **Authentication** | MFA, JWT, RBAC, closed registration | Cloud IAM (hosted Keycloak) |

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
