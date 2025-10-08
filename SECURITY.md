# Security Policy

## Supported Versions

We release patches for security vulnerabilities for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

We take the security of Clercq.It seriously. If you believe you have found a security vulnerability, please report it to us as described below.

### Where to Report

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report them via:
- **Email**: Send details to the repository owner
- **GitHub Security Advisories**: Use the [Security tab](https://github.com/Echarnus/Clercq.It/security/advisories) to privately report vulnerabilities

### What to Include

Please include the following information in your report:
- Type of vulnerability
- Full paths of source file(s) related to the vulnerability
- Location of the affected source code (tag/branch/commit or direct URL)
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit it

### Response Timeline

- **Initial Response**: Within 48 hours
- **Status Update**: Within 7 days
- **Fix Timeline**: Varies by severity
  - Critical: Within 7 days
  - High: Within 30 days
  - Medium: Within 90 days
  - Low: Best effort

## Security Measures

### Automated Security Scanning

The project uses multiple automated security scanning tools:

#### Dependency Scanning
- **Dependabot**: Automated dependency updates for .NET, npm, Docker, Terraform, and GitHub Actions
- **npm audit**: Security audits for Node.js dependencies
- **dotnet list package --vulnerable**: Checks for known .NET package vulnerabilities

#### Code Analysis
- **CodeQL**: Advanced semantic code analysis for C# and JavaScript/TypeScript
- **Security linting**: ESLint security rules for frontend code

#### Container Security
- **Trivy**: Comprehensive container vulnerability scanning
  - Scans Docker images for OS and application vulnerabilities
  - Scans filesystem for IaC misconfigurations
  - Integrated into build pipeline

#### Secret Detection
- **TruffleHog**: Scans for accidentally committed secrets and credentials
- **GitHub Secret Scanning**: Native GitHub secret detection

#### Security Scoring & Compliance
- **OpenSSF Scorecard**: Evaluates project security best practices
  - Provides security score from 0-10
  - Badge shows current security posture: [![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Echarnus/Clercq.It/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Echarnus/Clercq.It)
  - Target score: 8.0+ (High Security)
  - Click badge to view detailed analysis
- **Security Compliance**: Bank-grade security aligned with NIST, PCI DSS, ISO 27001
  - See [Security Compliance Guide](./docs/security-compliance.md) for detailed framework alignment

### CI/CD Security

- **Build Attestation**: All container images have signed build provenance
- **Multi-platform Builds**: Support for AMD64 and ARM64 architectures
- **Least Privilege**: GitHub Actions use minimal required permissions
- **Secret Management**: Sensitive data stored in GitHub Secrets
- **Automated Scanning**: Security scans run on every push and pull request

### Container Security

- **Non-root Execution**: Containers run with non-privileged users
- **Minimal Base Images**: Alpine and Debian slim-based images with minimal packages
- **Health Checks**: Built-in container health monitoring
- **Image Signing**: Container images are signed with attestations

### Application Security

- **Parameterized Queries**: Entity Framework Core prevents SQL injection
- **Input Validation**: FluentValidation on all API requests
- **HTTPS Enforcement**: TLS/SSL enforced in production
- **CORS Configuration**: Configurable cross-origin resource sharing
- **JWT Authentication**: Secure token-based authentication
- **File Upload Validation**: Strict validation of file types and sizes
- **Content Security Policy**: Modern web security headers

### Infrastructure Security

- **Infrastructure as Code**: Terraform for reproducible deployments
- **Scaleway Managed Services**: Database and container hosting with built-in security
- **Backup Strategy**: Automated database backups
- **Network Isolation**: Proper network segmentation
- **Secrets Management**: Environment variables for sensitive configuration

## Security Best Practices for Contributors

### Code Security
1. Never commit secrets, API keys, or passwords
2. Use parameterized queries, not string concatenation
3. Validate all user input
4. Follow principle of least privilege
5. Keep dependencies up to date

### Dependency Management
1. Review Dependabot PRs promptly
2. Test dependency updates before merging
3. Avoid dependencies with known vulnerabilities
4. Minimize dependency count

### Review Process
1. All code changes require review
2. Security-sensitive changes require additional scrutiny
3. Run security scans locally before pushing
4. Address security findings before merging

### Local Security Testing

```bash
# Run .NET vulnerability check
cd src
dotnet list ClercqIt.Api/ClercqIt.Api.csproj package --vulnerable --include-transitive

# Run npm audit
cd src/ClercqIt.Web
pnpm audit

# Scan for secrets (requires trufflehog)
trufflehog filesystem . --only-verified

# Build and scan Docker image locally (requires trivy)
docker build -t clercq-it:local ./src
trivy image clercq-it:local
```

## Disclosure Policy

When we receive a security vulnerability report, we will:

1. Confirm receipt of the report
2. Assess the vulnerability and determine its impact
3. Work on a fix in a private repository
4. Prepare a security advisory
5. Release a patched version
6. Publish the security advisory
7. Credit the reporter (unless they wish to remain anonymous)

## Security Updates

Security updates are released as soon as possible after a vulnerability is confirmed. We use GitHub Security Advisories to communicate security issues.

### Staying Informed

- Watch this repository for security advisories
- Enable Dependabot alerts
- Check the [Security tab](https://github.com/Echarnus/Clercq.It/security) regularly
- Review the [CHANGELOG](./CHANGELOG.md) for security-related updates

## Compliance

This project implements security controls aligned with banking and financial services standards:

### Industry Standards
- **NIST Cybersecurity Framework (CSF)** - Complete alignment with all five functions: Identify, Protect, Detect, Respond, Recover
- **NIST 800-53** - Implementation of security controls suitable for financial institutions
- **PCI DSS** - Controls aligned with Payment Card Industry Data Security Standard
- **SOC 2 Type II** - Security, Availability, Processing Integrity, Confidentiality, Privacy principles
- **ISO 27001** - Information Security Management System (ISMS) controls
- **GDPR** - Data protection and privacy compliance measures

### Security Level
**Bank-Grade / Financial Services Ready**

This application implements comprehensive security controls suitable for use in banking and financial services environments, including:
- Multi-factor authentication (MFA)
- End-to-end encryption (TLS 1.2+, AES-256)
- Role-based access control (RBAC)
- Comprehensive audit trails
- Daily vulnerability scanning
- Automated security monitoring
- Incident response procedures

For detailed compliance mapping and security controls, see the [Security Compliance Guide](./docs/security-compliance.md).

### Security Score

Current security posture: [![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Echarnus/Clercq.It/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Echarnus/Clercq.It)

**Target**: 8.0+ (High Security)

### Best Practices Implemented
- OWASP Top 10 mitigation strategies
- CWE/SANS Top 25 vulnerability prevention
- OpenSSF Best Practices Badge criteria
- Secure Software Development Framework (SSDF)
- Supply-chain Levels for Software Artifacts (SLSA)

## Additional Resources

- [Security Compliance Guide](./docs/security-compliance.md) - Detailed NIST, PCI DSS, ISO 27001 alignment
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CWE Top 25](https://cwe.mitre.org/top25/)
- [OpenSSF Best Practices](https://bestpractices.coreinfrastructure.org/)
- [GitHub Security Best Practices](https://docs.github.com/en/code-security)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)

## Contact

For security concerns, please contact the repository maintainers through GitHub Security Advisories or via the repository owner's contact information.

---

**Last Updated**: 2024-01-08
