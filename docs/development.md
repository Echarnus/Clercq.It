# Local Development Guide

This guide covers local development setup and workflows for the Clercq.It project, including both manual setup and Aspire orchestration.

## Prerequisites

### Required Software
- **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Node.js 23+** - [Download](https://nodejs.org/)
- **pnpm 10.12.4+** - Install with `npm install -g pnpm`
- **Docker** - [Download](https://www.docker.com/products/docker-desktop)
- **Git** - [Download](https://git-scm.com/)

### Optional Tools
- **Visual Studio 2022** or **VS Code** with C# extension
- **pgAdmin** or **DBeaver** for database management
- **Postman** or **Insomnia** for API testing

## Project Structure

```
Clercq.It/
├── src/                          # Source code
│   ├── ClercqIt.Api/            # ASP.NET Core API
│   ├── ClercqIt.Web/            # Next.js frontend
│   ├── Clercq.It.Domain/        # Domain layer
│   ├── Clercq.It.Application/   # Application layer
│   ├── Clercq.It.Infrastructure/ # Infrastructure layer
│   ├── Clercq.It.AppHost/       # Aspire orchestration (dev only)
│   └── Clercq.It.ServiceDefaults/ # Aspire service defaults (dev only)
├── tests/                       # Test projects
│   └── ClercqIt.Api.Tests/      # API unit tests
├── docs/                        # Documentation
└── .github/                     # GitHub workflows & config
```

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/Echarnus/Clercq.It.git
cd Clercq.It
```

### 2. Choose Your Development Approach

You can develop using either **Aspire orchestration** (recommended) or **manual setup**.

## Option A: Aspire Orchestration (Recommended)

> **⚠️ Important**: Aspire is **only for local development**. Production deployments use Docker containers without Aspire components.

### What is Aspire?

.NET Aspire is a development orchestration tool that simplifies running the entire application stack locally with enhanced observability.

### Prerequisites
- .NET 9.0 SDK
- Docker Desktop
- **Aspire workload** (install with `dotnet workload install aspire`)

> **Note**: The Aspire workload is only required for local development when running the AppHost. Production builds and CI/CD pipelines do NOT require this workload.

### Start the Application

```bash
cd src/Clercq.It.AppHost
dotnet run
```

This will automatically:
- Launch the Aspire Dashboard at `http://localhost:15888`
- Start PostgreSQL with pgAdmin web interface
- Launch the API with automatic database connection and migrations
- Start the Next.js frontend (if configured)
- Provide comprehensive observability, logging, and monitoring

### Aspire Dashboard Features

The Aspire Dashboard provides:
- **Service Overview**: Monitor all running services
- **Logs**: Centralized logging from all services
- **Traces**: Distributed tracing across services
- **Metrics**: Performance metrics and health status
- **Configuration**: Service configuration and connection strings

### Services Available

When running with Aspire, the following services are available:

- **PostgreSQL Database**: `postgres` service with pgAdmin
- **API Service**: `clercqit-api` at `https://localhost:7000`
- **Next.js Frontend**: `clercqit-web` (if configured)

### Development Benefits

- **Automatic Service Discovery**: No manual connection string configuration
- **Enhanced Observability**: Distributed tracing and structured logging out-of-the-box
- **Simplified Workflow**: Single command starts entire application stack
- **Integrated Database Management**: pgAdmin web interface for database operations
- **Hot Reload**: Development-optimized configurations

### Aspire vs Production

| Aspect | Local Development (Aspire) | Production |
|--------|---------------------------|------------|
| Architecture | AppHost orchestration | Single Docker container |
| Components | AppHost, ServiceDefaults, Dashboard | API, frontend, nginx only |
| Database | Containerized PostgreSQL + pgAdmin | Managed PostgreSQL (Scaleway, etc.) |
| Features | Hot reload, tracing, metrics | Optimized for performance |
| Workload Required | Yes (`dotnet workload install aspire`) | No - AppHost excluded from builds |

> **Key Point**: The `Clercq.It.AppHost` project is **excluded from production Docker builds and CI/CD pipelines**. The ServiceDefaults project is included in production but does not require the Aspire workload.

## Option B: Manual Setup

If you prefer to run services manually or don't want to use Aspire:

### Database Setup

#### Using Docker (Recommended)

```bash
# Start PostgreSQL container
docker run -d --name clercq-postgres \
  -e POSTGRES_DB=ClercqItDb \
  -e POSTGRES_USER=clercq_user \
  -e POSTGRES_PASSWORD=clercq_pass \
  -p 5432:5432 \
  postgres:16

# Verify container is running
docker ps
```

#### Using Local PostgreSQL Installation

1. Install PostgreSQL 16+
2. Create database and user:

```sql
CREATE DATABASE "ClercqItDb";
CREATE USER clercq_user WITH PASSWORD 'clercq_pass';
GRANT ALL PRIVILEGES ON DATABASE "ClercqItDb" TO clercq_user;
```

### Backend Setup (.NET API)

```bash
# Navigate to solution root
cd Clercq.It

# Restore NuGet packages
dotnet restore

# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Apply database migrations
cd src/Clercq.It.Infrastructure
dotnet ef database update --startup-project ../ClercqIt.Api

# Build the solution
cd ../..
dotnet build

# Run tests to verify setup
dotnet test
```

### Run the API

```bash
cd src/ClercqIt.Api
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7000`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:7000/swagger`
- Scalar UI: `https://localhost:7000/scalar/v1`

### Frontend Setup (Next.js)

```bash
# Navigate to web project
cd src/ClercqIt.Web

# Install dependencies
pnpm install

# Run development server
pnpm dev
```

The web application will be available at `http://localhost:3000`

## Configuration

### Connection Strings

The API uses the following connection string by default:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ClercqItDb;Username=clercq_user;Password=clercq_pass;"
  }
}
```

For different environments, create:
- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`

### Environment Variables

You can override settings using environment variables:

```bash
# Database connection
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=ClercqItDb;Username=clercq_user;Password=clercq_pass;"

# Logging level
export Logging__LogLevel__Default="Information"
```

## Development Workflow

### Making Database Changes

When modifying domain entities:

```bash
cd src/Clercq.It.Infrastructure

# Create new migration
dotnet ef migrations add MigrationName --startup-project ../ClercqIt.Api

# Apply migration to database
dotnet ef database update --startup-project ../ClercqIt.Api

# Review generated SQL (optional)
dotnet ef migrations script --startup-project ../ClercqIt.Api
```

### Adding New Features

Follow Clean Architecture patterns:

1. **Domain**: Add entities, value objects, repository interfaces in `Clercq.It.Domain`
2. **Application**: Add queries/commands, handlers, DTOs, validators in `Clercq.It.Application`
3. **Infrastructure**: Add repository implementations, entity configurations in `Clercq.It.Infrastructure`
4. **API**: Add minimal API endpoints in feature folders in `ClercqIt.Api`

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/ClercqIt.Api.Tests/

# Run tests with detailed output
dotnet test --verbosity normal
```

### Building and Running

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/ClercqIt.Api/

# Run API in watch mode (auto-restart on changes)
dotnet watch run --project src/ClercqIt.Api/
```

## Docker Development

### Build and Run with Docker

```bash
# Build Docker image
docker build -t clercq-it ./src

# Run container
docker run -p 80:80 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=ClercqItDb;Username=clercq_user;Password=clercq_pass;" \
  clercq-it
```

The application will be available at `http://localhost`

## Troubleshooting

### Common Issues

#### Port Already in Use

```bash
# Find process using port 5000/7000
lsof -i :5000
lsof -i :7000

# Kill process
kill -9 <PID>
```

#### Database Connection Failed

- Verify PostgreSQL is running: `docker ps` or check local service
- Check connection string in `appsettings.json`
- Verify firewall settings allow port 5432

#### Migration Issues

```bash
# Reset migrations (CAUTION: drops all data)
dotnet ef database drop --startup-project ../ClercqIt.Api
dotnet ef migrations remove --startup-project ../ClercqIt.Api
dotnet ef migrations add InitialCreate --startup-project ../ClercqIt.Api
dotnet ef database update --startup-project ../ClercqIt.Api
```

#### Build Errors

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

#### Aspire Dashboard Not Starting

1. **Port Conflicts**: Aspire Dashboard typically uses port 15888
2. **Docker Requirements**: PostgreSQL requires Docker Desktop to be running
3. **Network Connectivity**: Ensure Docker networks allow inter-container communication

#### Aspire Workload Issues

If you get an error like `error NETSDK1147: To build this project, the following workloads must be installed: aspire`:

```bash
# Install the Aspire workload (for local development only)
dotnet workload install aspire
```

**Note**: This error should ONLY occur when running the AppHost project locally. Production builds and CI/CD pipelines explicitly exclude the AppHost project and do not require the Aspire workload.

If you're seeing this error in CI/CD or Docker builds, check that:
- The build is not trying to restore/build the `Clercq.It.AppHost` project
- The solution-level restore/build is not being used
- Only the specific production projects are being built

### Debugging

- Check Aspire Dashboard logs for service startup issues
- Verify PostgreSQL container is running: `docker ps`
- Test API health endpoint: `https://localhost:7000/health`

## IDE Configuration

### Visual Studio 2022
- Install ASP.NET Core workload
- Install Entity Framework Core tools
- Set ClercqIt.Api as startup project (or Clercq.It.AppHost for Aspire)

### VS Code

Install recommended extensions:
- C# for Visual Studio Code
- .NET Extension Pack
- REST Client (for API testing)
- PostgreSQL (for database browsing)

### Rider
- Enable .NET Core support
- Configure database connection
- Set run configuration for ClercqIt.Api (or Clercq.It.AppHost for Aspire)

## Next Steps

Once your development environment is set up:

1. Review the [Architecture Documentation](./architecture.md)
2. Understand [Versioning and Branching](./versioning.md)
3. Learn about [CI/CD Pipelines](./devops.md)
4. Explore the API endpoints at `https://localhost:7000/swagger` or `https://localhost:7000/scalar/v1`
5. Check the test coverage with `dotnet test --collect:"XPlat Code Coverage"`
6. Start developing new features following Clean Architecture patterns

## Additional Resources

- [Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Next.js Documentation](https://nextjs.org/docs)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
