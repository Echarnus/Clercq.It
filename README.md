# Clercq.It

[![Test](https://github.com/Echarnus/Clercq.It/actions/workflows/test.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/test.yml)
[![Build](https://github.com/Echarnus/Clercq.It/actions/workflows/build.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/build.yml)
[![Deploy](https://github.com/Echarnus/Clercq.It/actions/workflows/deploy.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/deploy.yml)
[![Infra](https://github.com/Echarnus/Clercq.It/actions/workflows/infra.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/infra.yml)
[![Security Scanning](https://github.com/Echarnus/Clercq.It/actions/workflows/security-scan.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/security-scan.yml)
[![CodeQL](https://github.com/Echarnus/Clercq.It/actions/workflows/codeql.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/codeql.yml)
[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Echarnus/Clercq.It/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Echarnus/Clercq.It)
[![Docker Hub](https://img.shields.io/docker/pulls/echarnus/clercq-it)](https://hub.docker.com/r/echarnus/clercq-it)

A modern full-stack web application showcasing enterprise-grade development practices with Clean Architecture, Domain-Driven Design, and automated CI/CD pipelines. This project demonstrates proficiency in .NET, Next.js, containerization, and cloud deployment.

## 📊 Pipeline Status

The badges above show the live state of each pipeline. Pipelines, triggers and required secrets are described in the [DevOps guide](./docs/devops.md); how work flows from issue to production is in [Contributing](./docs/contributing.md).

## 🚀 Tech Stack

### Backend
- **.NET 10** - Modern C# web API with minimal APIs
- **ASP.NET Core** - High-performance web framework
- **Entity Framework Core** - ORM with PostgreSQL
- **MediatR** - CQRS pattern implementation
- **FluentValidation** - Request validation
- **Keycloak (Cloud IAM)** - JWT bearer tokens and role-based access, admin sign-in with PKCE
- **OpenAPI and Scalar** - API documentation (Development only)
- **MCP server** - Content and MyZen tools for Claude at `/api/mcp`

### Frontend  
- **Next.js 16** - React framework with App Router
- **React 19** - Latest React with concurrent features
- **TypeScript** - Type-safe JavaScript development
- **Tailwind CSS** - Utility-first CSS framework
- **Radix UI** - Accessible component primitives

### Infrastructure
- **Docker** - Multi-stage containerization
- **Nginx** - Reverse proxy and load balancing
- **PostgreSQL** - Relational database
- **Scaleway Object Storage** - S3-compatible object storage for blog images
- **GitHub Actions** - CI/CD automation
- **GitVersion** - Semantic versioning
- **Scaleway** - Cloud hosting platform
- **Docker Hub** - Container registry
- **Terraform** - Infrastructure as Code

## 🏗️ Architecture

Built with Clean Architecture principles and Domain-Driven Design:

```
┌─────────────────────────────────┐
│         API Layer               │  ← ASP.NET Core Minimal APIs
├─────────────────────────────────┤
│      Application Layer          │  ← MediatR, FluentValidation  
├─────────────────────────────────┤
│     Infrastructure Layer        │  ← EF Core, PostgreSQL
├─────────────────────────────────┤
│        Domain Layer             │  ← Entities, Value Objects
└─────────────────────────────────┘
```

## 📚 Documentation

Comprehensive technical documentation is available in the [`/docs`](./docs) folder:

- **[Docs index](./docs/README.md)** - All documentation pages
- **[Contributing](./docs/contributing.md)** - How work flows from issue to production via agents
- **[Architecture](./docs/architecture.md)** - Layers, endpoints, authentication and the production container
- **[Development](./docs/development.md)** - Local setup with Aspire, and the [code style and analyzer policy](./docs/development.md#code-style)
- **[Testing](./docs/testing.md)** - Test stack, integration tests and coverage gates
- **[DevOps](./docs/devops.md)** - CI/CD pipelines and deployment
- **[Versioning](./docs/versioning.md)** - GitVersion, trunk-based branching and production releases

## 🚀 Quick Start

1. **Prerequisites**: .NET 10 SDK, Node.js 24 with Corepack (`corepack enable`), Docker
2. **Clone**: `git clone https://github.com/Echarnus/Clercq.It.git && cd Clercq.It`
3. **Run everything**: `dotnet run --project src/Clercq.It.AppHost`

Aspire starts PostgreSQL, Keycloak (with test users), the migrations, the API (`http://localhost:5001`) and the web app (`http://localhost:3000`). Running the parts by hand is described in [Development](./docs/development.md#option-b-manual-setup).

## 🧪 Testing

```bash
# Backend: format, build (warnings are errors), unit, architecture and integration tests
dotnet format Clercq.It.CI.slnf --verify-no-changes
dotnet build Clercq.It.CI.slnf -c Release
dotnet test Clercq.It.CI.slnf -c Release --no-build   # integration tests need Docker

# Frontend
cd src/ClercqIt.Web
pnpm install --frozen-lockfile
pnpm lint && pnpm format:check && pnpm typecheck && pnpm test && pnpm build
```

Coverage gates and thresholds: [Testing](./docs/testing.md#coverage).

### Version Management
- **GitVersion** calculates `{Major}.{Minor}.{commits}` on `main`
- Every production deploy is tagged `prod-<version>` with a GitHub Release
- **Docker Tags**: `<version>`, short SHA and `latest`

## 🐳 Docker

The production Docker container is optimized for deployment and excludes development-only components:

```bash
# Build and run production container
docker build -t clercq-it ./src
docker run -p 80:80 clercq-it
```

The API needs a database connection string and `Keycloak__BaseUrl` to start; see [Development](./docs/development.md#docker-development).

### Container Architecture
- **Included**: API, frontend (Next.js), nginx reverse proxy, runtime dependencies
- **Excluded**: Aspire AppHost, development orchestration, unnecessary workloads  
- **Result**: Single optimized container for production deployment

> **Note**: Aspire components (`Clercq.It.AppHost`) are excluded from Docker builds as they're only needed for local development orchestration.

## 🏗️ Infrastructure

The application uses a **serverless-first architecture** on Scaleway with automatic scaling and cost optimization:

### Scaleway Infrastructure
- **Serverless Container** `clercq-it-app`: 0 to 1 instances, 500 mVCPU, 512 MB, scales to zero
- **Managed PostgreSQL**: Scaleway RDB `DB-DEV-S`, PostgreSQL 16 (always on)
- **Object Storage**: public-read bucket for images
- **Secret Manager**: application secrets ([Secrets](./docs/secrets.md))
- **Verify environment**: a second container, database and bucket that test every image before production

### Infrastructure Management
- **Terraform**: Infrastructure as Code in `infra/terraform/`
- **GitHub Actions**: `infra.yml` plans on PRs and applies after infra changes and after every deploy
- **State**: S3 backend in Scaleway Object Storage (`clercq-it-terraform-state`)

### Quick Infrastructure Setup

```bash
# Navigate to infra directory
cd infra/terraform

# Copy and configure variables
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars; credentials come from SCW_* and AWS_* environment variables

# Initialize and deploy
terraform init
terraform plan
terraform apply
```

For detailed infrastructure documentation, see [DevOps](docs/devops.md) and [Secrets](docs/secrets.md).

## 📊 Monitoring & Observability

- **Build Status**: GitHub Actions workflow badges
- **Test Coverage**: Codecov integration
- **Container Health**: Docker health checks
- **Deployment Status**: Automated status reporting

## 🔒 Security

The project implements **bank-grade security** measures across all stages of development and deployment, aligned with NIST, PCI DSS, ISO 27001, and SOC 2 standards.

### Security Score & Compliance

[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Echarnus/Clercq.It/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Echarnus/Clercq.It)

**Security Level**: Bank-Grade / Financial Services Ready  
**Target Score**: 8.0+ (High Security)

**Compliance Standards**:
- ✅ NIST Cybersecurity Framework (CSF) - Full alignment
- ✅ NIST 800-53 - Security controls for financial institutions
- ✅ PCI DSS - Payment Card Industry standards alignment
- ✅ SOC 2 Type II - Security, availability, integrity principles
- ✅ ISO 27001 - Information security management
- ✅ GDPR - Data protection compliance

See [Security Compliance Guide](./docs/security-compliance.md) for detailed framework alignment.

### Automated Security Scanning
- **Trivy Container Scanning**: Vulnerability scanning of Docker images and filesystems
- **CodeQL Analysis**: Advanced semantic code analysis for C# and JavaScript/TypeScript
- **Dependabot**: Automated dependency updates across all ecosystems (NuGet, npm, Docker, Terraform, GitHub Actions)
- **Secret Scanning**: gitleaks blocks PRs that contain secrets
- **SAST**: Semgrep on every PR
- **OpenSSF Scorecard**: Continuous evaluation of security best practices

### Dependency Security
- **npm audit**: Automated security audits for Node.js dependencies
- **dotnet list package --vulnerable**: Checks for known .NET vulnerabilities
- **Weekly automated updates**: Dependabot creates PRs for security updates
- **Transitive dependency scanning**: Includes indirect dependencies

### Build & Container Security
- **Container Security**: Non-root execution, minimal attack surface with Debian slim images
- **Build Attestation**: Signed build provenance for supply chain security
- **Single platform**: `linux/amd64`, what Scaleway serverless containers run
- **Vulnerability scanning**: Trivy scans the image on every PR (blocking) and after every build
- **Pinned dependencies**: actions by commit SHA, base images by digest, NuGet and pnpm by lock file

### Application Security
- **Multi-Factor Authentication (MFA)**: TOTP on the Keycloak login page
- **JWT Authentication**: Keycloak-issued tokens, sign-in with authorization code and PKCE
- **Role-Based Access Control (RBAC)**: Granular permission management
- **Input Validation**: FluentValidation on all API requests
- **Parameterized Queries**: Entity Framework prevents SQL injection
- **HTTPS Enforcement**: TLS 1.2+ with HSTS headers
- **Encryption**: AES-256 for data at rest, TLS for data in transit
- **Secret Management**: Scaleway Secret Manager for app secrets, GitHub Secrets for pipelines
- **CORS Configuration**: Proper cross-origin resource sharing
- **Security Headers**: CSP, X-Frame-Options, X-Content-Type-Options

### CI/CD Security
- **Least Privilege**: Minimal GitHub Actions permissions
- **Secret Protection**: Sensitive data never exposed in logs
- **Security Workflows**: Daily automated security scans
- **Pull Request Checks**: Security validation before merge
- **Automated Alerts**: Immediate notification of security issues

### Monitoring & Incident Response
- **Centralized Logging**: Scaleway Cockpit for audit trails
- **Real-time Alerts**: Automated security notifications
- **Vulnerability Tracking**: GitHub Security tab integration
- **Incident Response Plan**: Documented procedures in SECURITY.md
- **Audit Trail**: Complete change history via git and logs

For detailed security information, see:
- [SECURITY.md](./SECURITY.md) - Security policy and reporting
- [Security Compliance Guide](./docs/security-compliance.md) - NIST, PCI DSS, ISO 27001 alignment
- [DevOps Security Guide](./docs/devops.md#security-features) - CI/CD security details

## 🚀 Deployment

Every merge to `main` deploys to **Scaleway**:

1. **Build**: tests, GitVersion, image pushed to Docker Hub with signed provenance, migration script
2. **Verify**: the image runs against the verify database and container and must pass a smoke test
3. **Production**: migrations, content import, new image, smoke test; a failed smoke test rolls back automatically
4. **Release**: tag `prod-<version>` and a GitHub Release

### Manual Deployment (rollback)

```bash
gh workflow run deploy.yml -f version=1.0.189
```

## 🤝 Contributing

Work runs through GitHub issues, done by agents: issue → branch `<type>/<issue>-<slug>` → pull request → required checks → auto-merge → production. See [Contributing](./docs/contributing.md).

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📧 Contact

**Echarnus** - [@Echarnus](https://github.com/Echarnus)

Project Link: [https://github.com/Echarnus/Clercq.It](https://github.com/Echarnus/Clercq.It)