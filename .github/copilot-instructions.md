# GitHub Copilot Instructions

You are an expert in:
- **Cloud Infrastructure**: Azure, Scaleway, Docker, Docker Compose, Kubernetes, nginx, CI/CD pipelines
- **.NET Development**: .NET 9, ASP.NET Core, Entity Framework Core, Clean Architecture, DDD, CQRS, MediatR, FluentValidation
- **React Development**: Next.js 15, React 19, TypeScript, Tailwind CSS, Radix UI, modern frontend practices

## Repository Overview

This is a modern full-stack web application built with Clean Architecture principles:

### Architecture
- **Domain Layer** (`src/Clercq.It.Domain`): Core domain models, aggregates, value objects, and repository interfaces
- **Application Layer** (`src/Clercq.It.Application`): MediatR handlers, queries, commands, DTOs, FluentValidation
- **Infrastructure Layer** (`src/Clercq.It.Infrastructure`): Entity Framework Core, PostgreSQL, repository implementations
- **API Layer** (`src/ClercqIt.Api`): ASP.NET Core minimal APIs, dependency injection, OpenAPI/Swagger
- **Web Layer** (`src/ClercqIt.Web`): Next.js frontend with TypeScript and Tailwind CSS

### Key Technologies
- **.NET 9** with minimal APIs and OpenAPI
- **PostgreSQL** with Entity Framework Core 9
- **Clean Architecture** with DDD patterns
- **MediatR** for CQRS implementation
- **FluentValidation** for request validation
- **Next.js 15** with React 19 and TypeScript
- **Docker** multi-stage builds with nginx proxy
- **Aspire** for orchestration and development experience

### Technical Documentation
All detailed technical information, setup guides, and architecture decisions are documented in the `/docs` folder. Always refer to these files for:
- Architecture decisions and patterns
- Setup and development guides
- Deployment and infrastructure details
- API documentation
- Database schema and migrations

**Documentation Guidelines:**
- All documentation must be placed in the `/docs` folder
- Do NOT create per-case documentation files in other directories (e.g., infrastructure/, src/, etc.)
- Update existing documentation files in `/docs` rather than creating new fix-specific files
- Keep documentation centralized and organized by topic, not by individual fixes or changes

### Development Guidelines
- Follow Clean Architecture dependency rules
- Use DDD patterns for domain modeling  
- Implement CQRS with MediatR
- Apply FluentValidation for input validation
- Use Entity Framework migrations for schema changes
- Follow minimal API patterns for endpoints
- Maintain comprehensive tests with xUnit
- Use Docker for containerization
- Follow GitFlow branching strategy

### Project Structure Notes
- All projects follow .NET 9 conventions
- Domain entities use DDD aggregate patterns
- Repository pattern with Unit of Work
- Value objects for complex domain concepts
- Feature-based organization in Application layer
- Minimal APIs organized by features
- PostgreSQL as the primary database
- Entity configurations in Infrastructure layer

When working on this codebase, prioritize maintainability, testability, and adherence to Clean Architecture principles.