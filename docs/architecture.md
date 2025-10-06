# Architecture Overview

This document provides a comprehensive overview of the Clercq.It application architecture, design principles, and technical implementation.

## Architecture Principles

The project follows **Clean Architecture** principles with **Domain-Driven Design (DDD)** to ensure:
- **Separation of Concerns**: Each layer has a distinct responsibility
- **Dependency Inversion**: Dependencies point inward toward the domain
- **Testability**: Business logic is isolated and easily testable
- **Maintainability**: Clear boundaries make the system easier to understand and modify

## Layer Structure

```
┌─────────────────────────────────┐
│         API Layer               │  ← ASP.NET Core Minimal APIs
├─────────────────────────────────┤
│      Application Layer          │  ← MediatR, FluentValidation
├─────────────────────────────────┤
│     Infrastructure Layer        │  ← EF Core, PostgreSQL, Repos
├─────────────────────────────────┤
│        Domain Layer             │  ← Entities, Value Objects, Rules
└─────────────────────────────────┘
```

## Layer Responsibilities

### Domain Layer (`src/Clercq.It.Domain`)

The core of the application containing business logic and domain models.

**Components:**
- **Entities**: `Project` and `Blog` aggregate roots
- **Value Objects**: `Skills` and `Tags` collections for complex domain concepts
- **Abstractions**: `IAggregateRoot`, `IRepository<T>`, `IUnitOfWork`
- **Repository Interfaces**: `IProjectRepository`, `IBlogRepository`

**Key Principles:**
- No dependencies on other layers
- Contains all business rules and domain logic
- Defines repository interfaces (implemented in Infrastructure)
- Uses value objects to encapsulate complex domain concepts

### Application Layer (`src/Clercq.It.Application`)

Orchestrates application workflows and use cases.

**Components:**
- **Features**: Organized by aggregate (Projects, Blogs)
- **Queries**: `GetAllProjectsQuery`, `GetFeaturedProjectsQuery`, `GetAllBlogsQuery`
- **Handlers**: MediatR query handlers implementing CQRS pattern
- **DTOs**: `ProjectDto`, `BlogDto` for data transfer
- **Validation**: FluentValidation for request validation (ready for commands)

**Key Principles:**
- Depends only on Domain layer
- Implements CQRS with MediatR
- Defines application-specific DTOs
- Contains validation logic for requests

### Infrastructure Layer (`src/Clercq.It.Infrastructure`)

Implements external concerns and data persistence.

**Components:**
- **DbContext**: `ApplicationDbContext` with PostgreSQL provider
- **Entity Configurations**: EF Core fluent mappings for domain entities
- **Repositories**: Generic `Repository<T>` and specific implementations
- **Unit of Work**: Transaction management and change tracking

**Key Principles:**
- Implements repository interfaces from Domain
- Handles database migrations and schema
- Contains EF Core configurations
- Manages external dependencies (database, file system, etc.)

### API Layer (`src/ClercqIt.Api`)

Exposes HTTP endpoints and handles web concerns.

**Components:**
- **Minimal APIs**: Feature-based endpoint organization
- **Endpoints**: `/api/projects`, `/api/blogs`
- **OpenAPI**: Swagger documentation with summaries
- **DI Configuration**: Clean Architecture layer registration

**Key Principles:**
- Thin layer focused on HTTP concerns
- Uses minimal APIs for modern, performant endpoints
- Handles request/response mapping
- Configures dependency injection

## Database Schema

### Projects Table
- `Id` (GUID, Primary Key)
- `Title` (varchar(200), required)
- `ShortDescription` (varchar(500), required)
- `LongDescription` (text, required)
- `Image` (text, required) - Base64 encoded
- `StartDate` (timestamp, required)
- `EndDate` (timestamp, required)
- `Featured` (boolean, required)
- `Skills` (text, required) - Semicolon-separated values

### Blogs Table
- `Id` (GUID, Primary Key)
- `ShortDescription` (varchar(500), required)
- `LongDescription` (text, required) - Rich text with images
- `Image` (text, required) - Base64 encoded
- `CreatedDate` (timestamp, required)
- `PublishDate` (timestamp, required)
- `Tags` (text, required) - Semicolon-separated values

## API Endpoints

### Projects
- `GET /api/projects` - Get all projects
- `GET /api/projects/featured` - Get featured projects only

### Blogs
- `GET /api/blogs` - Get all blogs

All endpoints return JSON and include OpenAPI documentation.

## Technology Stack

### Backend
- **.NET 9** - Latest LTS with minimal APIs
- **ASP.NET Core** - High-performance web framework
- **Entity Framework Core 9** - ORM with PostgreSQL provider
- **PostgreSQL 16+** - Primary database
- **MediatR** - CQRS and mediator pattern implementation
- **FluentValidation** - Request validation
- **xUnit** - Unit testing framework

### Frontend
- **Next.js 15** - React framework with App Router
- **React 19** - Latest React with concurrent features
- **TypeScript** - Type-safe JavaScript development
- **Tailwind CSS** - Utility-first CSS framework
- **Radix UI** - Accessible component primitives

### Infrastructure & DevOps
- **Docker** - Multi-stage containerization
- **nginx** - Reverse proxy and load balancing
- **GitHub Actions** - CI/CD automation
- **GitVersion** - Semantic versioning
- **Scaleway** - Cloud hosting platform
- **Terraform** - Infrastructure as Code

### Production Container Architecture

The production deployment uses a single Docker container with multiple services:

**Container Components:**
1. **.NET API** (port 5000) - Backend REST API
2. **Next.js Frontend** (port 3000) - Server-side rendered React application
3. **nginx** (port 80) - Reverse proxy routing requests to backend services

**Startup Sequence:**
1. .NET API starts in background and writes logs to `/var/log/api.log`
2. Next.js frontend starts in background and writes logs to `/var/log/frontend.log`
3. Health check waits for API `/health` endpoint (up to 60 seconds)
4. Health check waits for Next.js on port 3000 (up to 60 seconds)
5. nginx starts in foreground after all services are healthy

**Key Features:**
- **No Bad Gateway errors**: nginx only starts after backend services are ready
- **Process monitoring**: Startup script detects if services crash during initialization
- **Detailed logging**: Service logs captured for debugging failed startups
- **Clean shutdown**: Signal handling ensures graceful termination of all services
- **Resource limits**: 512MB RAM, 500m CPU (optimized for Scaleway serverless containers)

## Design Patterns

### CQRS (Command Query Responsibility Segregation)
- Queries and commands are separated
- MediatR handles request/response pipeline
- Queries return DTOs, commands modify state

### Repository Pattern
- Abstracts data access logic
- Generic repository for common operations
- Specific repositories for aggregate-specific queries

### Unit of Work Pattern
- Manages database transactions
- Ensures consistency across multiple repository operations
- Tracks changes and commits as a single unit

### Dependency Injection
- Constructor injection for dependencies
- Service registration in API layer
- Supports testing with mock implementations

## Future Enhancements

The architecture is designed to support:
- **Command Operations**: Create, Update, Delete with validation
- **Domain Events**: Event-driven architecture with handlers
- **Caching Strategies**: Redis or in-memory caching
- **Authentication & Authorization**: JWT-based security
- **API Versioning**: Support multiple API versions
- **Background Jobs**: Hangfire or Quartz for scheduled tasks
- **Real-time Features**: SignalR for live updates

## Testing Strategy

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Pyramid
- **Unit Tests**: Domain logic and application handlers
- **Integration Tests**: API endpoints and database operations
- **End-to-End Tests**: Frontend integration (planned)

## Performance Considerations

- **Minimal APIs**: Lower overhead than controller-based APIs
- **Async/Await**: All I/O operations are asynchronous
- **Connection Pooling**: EF Core connection pool management
- **Caching**: Ready for implementation with IMemoryCache or Redis
- **Pagination**: Support for large data sets (to be implemented)

## Security Features

- **Parameterized Queries**: EF Core prevents SQL injection
- **Input Validation**: FluentValidation on all requests
- **HTTPS**: Enforced in production
- **CORS**: Configurable for frontend integration
- **Container Security**: Non-root execution, minimal attack surface

## Monitoring & Observability

- **Health Checks**: Built-in health endpoints
- **Structured Logging**: Comprehensive logging throughout
- **OpenTelemetry**: Ready for distributed tracing (via Aspire in development)
- **Metrics**: Performance and business metrics collection

## References

- [Development Guide](./development.md) - Local development setup
- [DevOps Guide](./devops.md) - CI/CD pipeline and deployment
- [Versioning Guide](./versioning.md) - GitVersion and branching strategy
