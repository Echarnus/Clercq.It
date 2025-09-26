# Clean Architecture Documentation

This directory contains comprehensive technical documentation for the Clercq.It project.

## Architecture

The project follows Clean Architecture principles with Domain-Driven Design (DDD):

### Layer Structure

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

### Domain Layer (`src/Clercq.It.Domain`)
- **Entities**: `Project` and `Blog` aggregate roots
- **Value Objects**: `Skills` and `Tags` collections  
- **Abstractions**: `IAggregateRoot`, `IRepository<T>`, `IUnitOfWork`
- **Repository Interfaces**: `IProjectRepository`, `IBlogRepository`

### Application Layer (`src/Clercq.It.Application`)
- **Features**: Organized by aggregate (Projects, Blogs)
- **Queries**: `GetAllProjectsQuery`, `GetFeaturedProjectsQuery`, `GetAllBlogsQuery`
- **Handlers**: MediatR query handlers for each feature
- **DTOs**: `ProjectDto`, `BlogDto` for data transfer
- **Validation**: FluentValidation for request validation (ready for commands)

### Infrastructure Layer (`src/Clercq.It.Infrastructure`)
- **DbContext**: `ApplicationDbContext` with PostgreSQL provider
- **Entity Configurations**: EF Core fluent mappings for domain entities
- **Repositories**: Generic `Repository<T>` and specific implementations
- **Unit of Work**: Transaction management and change tracking

### API Layer (`src/ClercqIt.Api`)
- **Minimal APIs**: Feature-based endpoint organization
- **Endpoints**: `/api/projects`, `/api/blogs`
- **OpenAPI**: Swagger documentation with summaries
- **DI Configuration**: Clean Architecture layer registration

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

- **.NET 9** - Latest LTS with minimal APIs
- **ASP.NET Core** - High-performance web framework
- **Entity Framework Core 9** - ORM with PostgreSQL provider
- **PostgreSQL** - Primary database
- **MediatR** - CQRS and mediator pattern
- **FluentValidation** - Request validation
- **xUnit** - Unit testing framework
- **OpenAPI/Swagger** - API documentation

## Development Setup

1. **Prerequisites**
   - .NET 9.0 SDK
   - PostgreSQL 16+ or Docker
   - Visual Studio 2022 or VS Code

2. **Database Setup**
   ```bash
   # Run PostgreSQL with Docker
   docker run -d --name clercq-postgres \
     -e POSTGRES_DB=ClercqItDb \
     -e POSTGRES_USER=clercq_user \
     -e POSTGRES_PASSWORD=clercq_pass \
     -p 5432:5432 postgres:16
   
   # Apply migrations
   cd src/Clercq.It.Infrastructure
   dotnet ef database update --startup-project ../ClercqIt.Api
   ```

3. **Run the API**
   ```bash
   cd src/ClercqIt.Api
   dotnet run
   ```

4. **Access Swagger UI**
   Navigate to `https://localhost:7000/swagger` in development

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Future Enhancements

The architecture is designed to support:
- Command operations (Create, Update, Delete) with validation
- Domain events and event handlers
- Integration with Aspire for orchestration
- Caching strategies (Redis, in-memory)
- Authentication and authorization
- API versioning
- Background jobs with Hangfire or Quartz