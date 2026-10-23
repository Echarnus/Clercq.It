# Architecture Overview

This document provides a comprehensive overview of the Clercq.It application architecture, design principles, and technical implementation.

## Architecture Principles

The project follows **Clean Architecture** principles with **Domain-Driven Design (DDD)** to ensure:
- **Separation of Concerns**: Each layer has a distinct responsibility
- **Dependency Inversion**: Dependencies point inward toward the domain
- **Testability**: Business logic is isolated and easily testable
- **Maintainability**: Clear boundaries make the system easier to understand and modify

## Layer Structure

Dependencies point inwards. Domain knows nothing about the other layers; the
Api and the two tools are composition roots that wire Infrastructure in.

```
            ClercqIt.Api ─────────────┐
   (also: ContentImport, EF.Migrations)│
              │                       │
              ▼                       ▼
        Application ◄──────── Infrastructure
   (MediatR, FluentValidation)  (EF Core, Npgsql, S3)
              │                       │
              └─────────► Domain ◄────┘
                (entities, value objects, repository contracts)
```

## Architecture rules

`tests/Clercq.It.Architecture.Tests` enforces these rules with
[NetArchTest](https://github.com/BenMorris/NetArchTest). It runs with the rest
of the suite in CI, so a violation fails the PR.

| Rule | Test |
|------|------|
| Domain depends on no other layer and no framework (EF Core, ASP.NET Core, Npgsql, MediatR, FluentValidation, AWS SDK) | `LayeringTests` |
| Application depends only on Domain, MediatR and FluentValidation | `LayeringTests` |
| Infrastructure does not depend on the Api | `LayeringTests` |
| Endpoint modules under `Clercq.It.Api.Features` go through MediatR, never the DbContext or repositories | `LayeringTests` |
| Only `ClercqIt.Api`, `Clercq.It.Infrastructure.EF.Migrations` and `Clercq.It.ContentImport` reference the Infrastructure project | `LayeringTests` |
| Aggregates (`IAggregateRoot`) live in `Clercq.It.Domain.Entities` and have no public setters (init is fine) | `DomainModelTests` |
| Value objects in `Clercq.It.Domain.ValueObjects` are records or sealed, without public setters | `DomainModelTests` |
| Repository contracts are interfaces in Domain, implemented in Infrastructure | `DomainModelTests` |
| MediatR requests end in `Command` or `Query`, handlers in `Handler`, validators in `Validator`, all in Application | `DomainModelTests` |

Aggregates change state only through their constructor, a `Create` factory and
behaviour methods (`Update`, `ChangeImage`, `Reschedule` on `Blog`, `Feature` and
`Unfeature` on `Project`), which enforce the invariants: a blog needs a short
description, a project a title and an end date on or after its start, and a
certification a title, an issuer and an expiry date on or after its issue date.
A violation throws `ArgumentException`; the Application validators reject the
same input earlier with a 400. EF Core loads rows through a private parameterless
constructor and private setters (#277). Tests build aggregates with the
`Aggregates` helper in `tests/Clercq.It.Application.Tests/Aggregates.cs`.

Run only these tests:

```bash
dotnet test tests/Clercq.It.Architecture.Tests -c Release
```

When a rule fails, the message lists the offending types. Fix the dependency
rather than the rule. If a rule is genuinely wrong, change it in the test and
in this table in the same PR.

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
- **Commands**: `CreateProjectCommand`, `CreateBlogCommand` for creating projects and blog posts with images
- **Handlers**: MediatR query and command handlers implementing CQRS pattern
- **DTOs**: `ProjectDto`, `BlogDto` for data transfer
- **Validation**: FluentValidation validators run for every MediatR request through `ValidationBehavior` (Application/Common/Behaviors); a failure becomes a 400 validation problem via `ValidationExceptionHandler` in the API

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
- **Object Storage**: S3-compatible service for Scaleway Object Storage (blog images)
- **MyZen**: client for the owner's MyZen account and its encrypted session store (`MyZen/`, see [MyZen](./myzen.md))
- **Configuration**: Settings for object storage and authentication

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
- **Authentication**: JWT Bearer authentication for protected endpoints
- **OpenAPI**: Swagger documentation with summaries
- **DI Configuration**: Clean Architecture layer registration

**Key Principles:**
- Thin layer focused on HTTP concerns
- Uses minimal APIs for modern, performant endpoints
- Handles request/response mapping
- Configures dependency injection
- Implements authentication for write operations

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
- `LongDescription` (text, required) - Markdown content
- `Image` (text, required) - URL to Scaleway Object Storage
- `CreatedDate` (timestamp, required)
- `PublishDate` (timestamp, required)
- `Tags` (text, required) - Semicolon-separated values

## API Endpoints

### Projects
- `GET /api/projects` - Get all projects
- `GET /api/projects/featured` - Get featured projects only
- `POST /api/projects/images` - Upload a project image (requires authentication)
  - Accepts multipart/form-data with:
    - `image`: Image file (jpg, jpeg, png, gif, webp)
  - Returns the image URL for use in project creation
- `POST /api/projects` - Create a new project (requires authentication)
  - Accepts multipart/form-data with:
    - `title`: Project title (max 200 chars)
    - `shortDescription`: Brief description (max 500 chars)
    - `longDescription`: Full markdown content
    - `imageUrl`: URL of previously uploaded image (from `/api/projects/images`)
    - `startDate`: Project start date (ISO 8601 format)
    - `endDate`: Project end date (ISO 8601 format)
    - `featured`: Whether the project is featured (true/false, defaults to false)
    - `skills`: Comma-separated list of skills

### Blogs
- `GET /api/blogs` - Get all blogs
- `POST /api/blogs` - Create a new blog (requires authentication)
  - Accepts multipart/form-data with:
    - `shortDescription`: Brief description (max 500 chars)
    - `longDescription`: Full markdown content
    - `image`: Image file (jpg, jpeg, png, gif, webp)
    - `tags`: Comma-separated list of tags

All endpoints return JSON and include OpenAPI documentation.

## Authentication

The API uses JWT Bearer token authentication for protected endpoints (blog and project creation).

JWT tokens are generated and signed by **Cloud IAM** (external identity service). The backend validates these tokens using Cloud IAM's Authority for public key discovery.

**Configuration:**
- `CloudIAM:ApiUrl` - Cloud IAM API endpoint (also used as JWT Authority for token validation)
- `CloudIAM:ApiKey` - API key for authenticating with Cloud IAM
- `CloudIAM:ClientRedirectUrl` - Frontend URL for OAuth redirects

## Object Storage

Project and blog images are stored in Scaleway Object Storage (S3-compatible).

**Configuration:**
- `ObjectStorage:Endpoint` - S3 endpoint URL
- `ObjectStorage:BucketName` - Bucket name for project and blog images
- `ObjectStorage:Region` - Scaleway region
- `ObjectStorage:AccessKey` - S3 access key
- `ObjectStorage:SecretKey` - S3 secret key

## Feature flags

Config-driven flags hide unfinished work in production (issue #253). The API owns them: `IFeatureFlags` (`src/ClercqIt.Api/FeatureFlags/`) reads the `FeatureFlags` configuration section, `RequireFeature("Name")` turns an endpoint or group into a 404 while its flag is off, and the anonymous `GET /api/features` returns all configured flags. The Next.js app reads that endpoint through `isEnabled()` in `lib/featureFlags.ts` and fails closed. How to add, use and remove a flag: [versioning.md](./versioning.md#feature-flags).

## Technology Stack

### Backend
- **.NET 10** (LTS) - Minimal APIs
- **ASP.NET Core** - High-performance web framework
- **Entity Framework Core 10** - ORM with PostgreSQL provider
- **PostgreSQL 16+** - Primary database
- **MediatR** - CQRS and mediator pattern implementation
- **FluentValidation** - Request validation
- **xUnit** - Unit testing framework

### Frontend
- **Next.js 16** - React framework with App Router
- **React 19** - Latest React with concurrent features
- **TypeScript** - Type-safe JavaScript development
- **Tailwind CSS 4** - Utility-first CSS framework, configured CSS-first in `app/globals.css`
- **Radix UI** - Accessible component primitives

### Infrastructure & DevOps
- **Docker** - Multi-stage containerization
- **nginx** - Reverse proxy and load balancing
- **GitHub Actions** - CI/CD automation
- **GitVersion** - Semantic versioning
- **Scaleway** - Cloud hosting platform
- **Terraform** - Infrastructure as Code

### Environments

| Environment | Container | Database | Bucket | Secrets |
|-------------|-----------|----------|--------|---------|
| Production | `clercq-it-app` (www.clercq.it) | `clercqit_portfolio` | `clercq-it-blog-images` | `/clercq-it` |
| Verify | `clercq-it-verify` (Scaleway domain, scales to zero) | `clercqit_verify` | `clercq-it-verify-images` | `/clercq-it/verify` |

Both run the same image and share only the RDB instance `portfolio-db` and the container namespace. Every image runs in verify before it reaches production; see [DevOps: Verify stage](./devops.md#verify-stage). Verify is defined in `infra/terraform/verify.tf`.

### Production Container Architecture

The production deployment uses a single Docker container with multiple services:

**Container Components:**
1. **.NET API** (port 5000) - Backend REST API
2. **Next.js Frontend** (port 3000) - Server-side rendered React application
3. **nginx** (port 80) - Reverse proxy routing requests to backend services

**Startup Sequence** (`src/start.sh`, PID 1 in the container):
1. .NET API starts in background and writes logs to `/var/log/api.log`
2. Next.js frontend starts in background and writes logs to `/var/log/frontend.log`. It gets `API_INTERNAL_URL=http://localhost:5000`, so server-rendered pages (homepage, portfolio, certifications) call the API directly; the browser uses relative `/api` URLs through nginx (#348)
3. Health check waits for API `/health` endpoint (up to 60 seconds)
4. Health check waits for Next.js on port 3000 (up to 60 seconds)
5. nginx starts after all services are healthy
6. The script then supervises all three and exits with code 1 as soon as one stops, printing the last 50 lines of that service's log, so Scaleway restarts the container (#222)

**Non-root (#280):** the image ends with `USER clercqit` (uid 10001), so nginx, the API and Next.js all run unprivileged and no process has uid 0. nginx still listens on port 80, which Terraform sets as the container port, because its binary carries the `cap_net_bind_service` file capability. Its pid file is `/tmp/nginx.pid`, and `/var/cache/nginx`, `/var/log/api.log` and `/var/log/frontend.log` belong to `clercqit`. If a runtime ever ignores file capabilities, nginx fails to bind port 80 and the verify stage stops the deploy; the fix then is to listen on 8080 and change `port` on both containers in Terraform.

**Patched and slim (#281):** the final stage runs `apt-get upgrade` on the `nginx:1.29-bookworm` base, so each build picks up the Debian security fixes released since that image was published, and it copies only the `node` binary from `node:24-slim` (no npm or npx; `start.sh` runs `node server.js`). Trivy blocks on fixable HIGH and CRITICAL findings in the image.

**Key Features:**
- **No Bad Gateway errors**: nginx only starts after backend services are ready
- **Process monitoring**: the script detects a crash during startup and afterwards, and exits instead of serving 502s
- **Detailed logging**: Service logs captured for debugging failed startups
- **Clean shutdown**: on SIGTERM (the image's `STOPSIGNAL`, and what Scaleway sends) the script stops all three services and exits 0
- **Health check**: the image's `HEALTHCHECK` checks the API `/health` and the site through nginx, for `docker run` and local tooling; Scaleway probes the port itself
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
dotnet test Clercq.It.CI.slnf

# Run with coverage
dotnet test Clercq.It.CI.slnf --collect:"XPlat Code Coverage"
```

### Test Pyramid
- **Unit Tests**: Domain logic and application handlers
- **Architecture Tests**: Layering and DDD rules (see [Architecture rules](#architecture-rules))
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
- **JWT Authentication**: Secure token-based authentication for protected endpoints
- **File Upload Validation**: Strict validation of file types and sizes for blog images

## Monitoring & Observability

- **Health Checks**: Built-in health endpoints
- **Structured Logging**: Comprehensive logging throughout
- **OpenTelemetry**: Ready for distributed tracing (via Aspire in development)
- **Metrics**: Performance and business metrics collection

## References

- [Development Guide](./development.md) - Local development setup
- [DevOps Guide](./devops.md) - CI/CD pipeline and deployment
- [Versioning Guide](./versioning.md) - GitVersion and branching strategy
