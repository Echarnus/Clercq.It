# Clercq.It

[![Test](https://github.com/Echarnus/Clercq.It/actions/workflows/test.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/test.yml)
[![Build](https://github.com/Echarnus/Clercq.It/actions/workflows/build.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/build.yml)
[![Deploy](https://github.com/Echarnus/Clercq.It/actions/workflows/deploy.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/deploy.yml)
[![Docker Hub](https://img.shields.io/docker/pulls/echarnus/clercq-it)](https://hub.docker.com/r/echarnus/clercq-it)

A modern full-stack web application showcasing enterprise-grade development practices and CI/CD automation. This project demonstrates proficiency in .NET, Next.js, containerization, and cloud deployment.

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

## 🏗️ Architecture

This application uses a **single-container, multi-service architecture**:

```
┌─────────────────────────────────────┐
│              Nginx (Port 80)        │
│         Reverse Proxy               │
├─────────────────────────────────────┤
│  /api/* → .NET API (Port 5000)     │
│  /*     → Next.js App (Port 3000)  │
└─────────────────────────────────────┘
```

### Key Architectural Decisions

1. **Single Container Deployment** - Simplified orchestration and reduced operational complexity
2. **Nginx Reverse Proxy** - Efficient request routing and static file serving  
3. **Standalone Next.js Build** - Optimized for containerized deployment
4. **Multi-stage Docker Build** - Minimal production image size
5. **GitHub Flow** - Streamlined branching strategy for continuous deployment

## 🔄 CI/CD Pipeline

### Branching Strategy (GitHub Flow)
- **`main`** - Production branch, triggers deployment
- **`develop`** - Development branch for feature integration  
- **`feature/*`** - Feature branches merged via Pull Requests

### Automated Workflows

#### 🧪 Test Pipeline (`test.yml`)
- Runs on every push and pull request
- **Backend Testing**: xUnit integration tests with coverage
- **Frontend Testing**: Jest unit tests and ESLint
- **Build Validation**: Ensures code compiles successfully

#### 🏗️ Build Pipeline (`build.yml`)  
- Triggered after successful tests
- **GitVersion**: Automatic semantic versioning
- **Multi-platform Build**: AMD64 and ARM64 support
- **Docker Hub**: Automated image publishing
- **Security**: Build attestation and provenance

#### 🚀 Deploy Pipeline (`deploy.yml`)
- Triggered on `main` branch pushes
- **Production Deployment**: Automated Scaleway deployment
- **Health Checks**: Validates deployment success
- **Rollback Support**: Manual version specification

### Version Management
- **GitVersion** automatically calculates semantic versions
- **Branch-based Versioning**: Different strategies per branch type
- **Docker Tags**: Multiple tags for flexible deployment options

## 🐳 Docker

### Multi-Stage Build Process

1. **API Build Stage** - .NET SDK for compilation and publishing
2. **Frontend Build Stage** - Node.js for Next.js build with standalone output
3. **Production Stage** - Alpine Linux with Nginx, .NET runtime, and Node.js

### Container Features
- **Security**: Non-root user execution  
- **Optimization**: Multi-architecture builds (AMD64/ARM64)
- **Efficiency**: Aggressive build caching
- **Monitoring**: Health check endpoints

### Running Locally

```bash
# Build and run the container
docker build -t clercq-it ./src
docker run -p 80:80 clercq-it

# Or use Docker Compose (if available)
docker-compose up --build
```

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
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📋 Project Status

This is a **portfolio project** demonstrating modern development practices:

- ✅ **DevOps Excellence**: Comprehensive CI/CD with GitVersion
- ✅ **Container Strategy**: Production-ready Docker deployment  
- ✅ **Testing Culture**: Automated testing with coverage reporting
- ✅ **Code Quality**: Linting, formatting, and static analysis
- ✅ **Security First**: Container hardening and attestation
- ✅ **Cloud Native**: Scaleable architecture for production workloads

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📧 Contact

**Echarnus** - [@Echarnus](https://github.com/Echarnus)

Project Link: [https://github.com/Echarnus/Clercq.It](https://github.com/Echarnus/Clercq.It)
