# Clercq.It

[![Test](https://github.com/Echarnus/Clercq.It/actions/workflows/test.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/test.yml)
[![Build](https://github.com/Echarnus/Clercq.It/actions/workflows/build.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/build.yml)
[![Deploy](https://github.com/Echarnus/Clercq.It/actions/workflows/deploy.yml/badge.svg)](https://github.com/Echarnus/Clercq.It/actions/workflows/deploy.yml)
[![Docker Hub](https://img.shields.io/docker/pulls/echarnus/clercq-it)](https://hub.docker.com/r/echarnus/clercq-it)

A modern full-stack web application showcasing enterprise-grade development practices with Clean Architecture, Domain-Driven Design, and automated CI/CD pipelines.

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
- **Aspire** - Orchestration and development experience

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

## 🐳 Docker

```bash
# Build and run
docker build -t clercq-it ./src
docker run -p 80:80 clercq-it
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