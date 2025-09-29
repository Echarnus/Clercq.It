# Clercq.It

[![Test](https://github.com/Echarnus/Clercq.It/actions/workflows/test.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/test.yml)
[![Build](https://github.com/Echarnus/Clercq.It/actions/workflows/build.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/build.yml)
[![Deploy](https://github.com/Echarnus/Clercq.It/actions/workflows/deploy.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/deploy.yml)
[![Infra](https://github.com/Echarnus/Clercq.It/actions/workflows/infra.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/infra.yml)
[![Docker Hub](https://img.shields.io/docker/pulls/echarnus/clercq-it)](https://hub.docker.com/r/echarnus/clercq-it)

A modern full-stack web application showcasing enterprise-grade development practices with Clean Architecture, Domain-Driven Design, and automated CI/CD pipelines. This project demonstrates proficiency in .NET, Next.js, containerization, and cloud deployment.

## 🚀 Tech Stack

### Backend
- **.NET 9.0** - Modern C# web API with minimal APIs
- **ASP.NET Core** - High-performance web framework
- **OpenAPI/Swagger** - API documentation and testing

### Frontend  
- **Next.js 15** - React framework with App Router
- **React 19** - Latest React with concurrent features
- **TypeScript** - Type-safe JavaScript development
- **Tailwind CSS** - Utility-first CSS framework
- **Radix UI** - Accessible component primitives

### Infrastructure
- **Docker** - Multi-stage containerization
- **Nginx** - Reverse proxy and load balancing
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

## 🚀 Tech Stack

- **.NET 9** - Modern C# with minimal APIs
- **PostgreSQL** - Primary database with EF Core
- **MediatR** - CQRS and mediator pattern implementation  
- **FluentValidation** - Request validation
- **Next.js 15** - React framework with TypeScript
- **Docker** - Containerization with multi-stage builds
- **Aspire** - Local development orchestration and tooling

## 📚 Documentation

All technical documentation is available in the [`/docs`](./docs) folder:

- **[Setup Guide](./docs/setup.md)** - Development environment setup
- **[Architecture](./docs/architecture.md)** - Detailed architecture documentation  
- **[API Reference](./docs/api.md)** - Endpoint documentation *(coming soon)*
- **[Deployment](./docs/deployment.md)** - Production deployment guide *(coming soon)*

## 🚀 Quick Start

1. **Prerequisites**: .NET 9, Docker, Node.js 23+
2. **Clone**: `git clone https://github.com/Echarnus/Clercq.It.git`
3. **Database**: `docker run -d --name clercq-postgres -e POSTGRES_DB=ClercqItDb -e POSTGRES_USER=clercq_user -e POSTGRES_PASSWORD=clercq_pass -p 5432:5432 postgres:16`
4. **Migrate**: `cd src/Clercq.It.Infrastructure && dotnet ef database update --startup-project ../ClercqIt.Api`
5. **Run API**: `cd src/ClercqIt.Api && dotnet run`
6. **Run Web**: `cd src/ClercqIt.Web && pnpm install && pnpm dev`

API available at `https://localhost:7000/swagger` • Web at `http://localhost:3000`

## 🧪 Testing

```bash
# Run all tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

#### 🏗️ Infra Pipeline (`infra.yml`)
- Triggered on infrastructure changes or manual dispatch
- **Infrastructure as Code**: Terraform-based Scaleway provisioning
- **Serverless Architecture**: Auto-scaling database and container
- **Cost Optimized**: Scales to zero when not in use

### Version Management
- **GitVersion** automatically calculates semantic versions
- **Branch-based Versioning**: Different strategies per branch type
- **Docker Tags**: Multiple tags for flexible deployment options

## 🐳 Docker

The production Docker container is optimized for deployment and excludes development-only components:

```bash
# Build and run production container
docker build -t clercq-it ./src
docker run -p 80:80 clercq-it
```

### Container Architecture
- **Included**: API, frontend (Next.js), nginx reverse proxy, runtime dependencies
- **Excluded**: Aspire AppHost, development orchestration, unnecessary workloads  
- **Result**: Single optimized container for production deployment

> **Note**: Aspire components (`Clercq.It.AppHost`) are excluded from Docker builds as they're only needed for local development orchestration.

## 🛠️ Development

### Prerequisites
- .NET 9.0 SDK
- Node.js 23+  
- pnpm 10.12.4+
- Docker

### Local Development Setup

```bash
# Clone the repository
git clone https://github.com/Echarnus/Clercq.It.git
cd Clercq.It

# Backend Development
cd src/ClercqIt.Api
dotnet restore
dotnet run

# Frontend Development (new terminal)
cd src/ClercqIt.Web  
pnpm install
pnpm dev
```

### Running Tests

```bash
# .NET Tests
dotnet test

# Frontend Tests  
cd src/ClercqIt.Web
pnpm test

# All Tests via GitHub Actions locally
act -j test
```

### Code Quality

```bash
# .NET Code Analysis
dotnet build --verbosity normal

# Frontend Linting
cd src/ClercqIt.Web
pnpm lint
```

## 🏗️ Infrastructure

The application uses a **serverless-first architecture** on Scaleway with automatic scaling and cost optimization:

### Scaleway Infrastructure
- **Serverless Container**: Auto-scales 0-1 vCPU with 128MB memory
- **Serverless SQL**: PostgreSQL database with minimal resource allocation
- **Organization**: ClercqIt with Portfolio namespace
- **Cost Optimization**: Infrastructure scales to zero when not in use

### Infrastructure Management
- **Terraform**: Infrastructure as Code in `/infrastructure/terraform/`
- **GitHub Actions**: Automated provisioning via `infrastructure.yml` workflow
- **Environment Protection**: Production deployments require approval
- **State Management**: Terraform state with proper gitignore patterns

### Quick Infrastructure Setup

```bash
# Navigate to infrastructure directory
cd infrastructure/terraform

# Copy and configure variables
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your Scaleway credentials

# Initialize and deploy
terraform init
terraform plan
terraform apply
```

For detailed infrastructure documentation, see [`infrastructure/README.md`](infrastructure/README.md).

## 📊 Monitoring & Observability

- **Build Status**: GitHub Actions workflow badges
- **Test Coverage**: Codecov integration
- **Container Health**: Docker health checks
- **Deployment Status**: Automated status reporting

## 🔒 Security

- **Container Security**: Non-root execution, minimal attack surface
- **Build Attestation**: Signed build provenance  
- **Secret Management**: GitHub Secrets for sensitive data
- **Dependency Scanning**: Automated vulnerability detection

## 🚀 Deployment

The application is automatically deployed to **Scaleway** on every push to the `main` branch. The deployment process:

1. **Version Calculation**: GitVersion determines the release version
2. **Image Build**: Multi-platform Docker image built and pushed to Docker Hub
3. **Health Validation**: Ensures the new version is healthy before deployment
4. **Automatic Rollout**: Zero-downtime deployment with health monitoring

### Manual Deployment

```bash
# Deploy specific version
gh workflow run deploy.yml -f version=1.0.0
>>>>>>> main
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📧 Contact

**Echarnus** - [@Echarnus](https://github.com/Echarnus)

Project Link: [https://github.com/Echarnus/Clercq.It](https://github.com/Echarnus/Clercq.It)