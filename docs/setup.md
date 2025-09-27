# Development Setup Guide

This guide will help you set up the development environment for the Clercq.It project.

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
│   └── Clercq.It.Infrastructure/ # Infrastructure layer
├── tests/                       # Test projects
│   └── ClercqIt.Api.Tests/      # API unit tests
├── docs/                        # Documentation
└── .github/                     # GitHub workflows & config
```

## Local Development Setup

### 1. Clone the Repository
```bash
git clone https://github.com/Echarnus/Clercq.It.git
cd Clercq.It
```

### 2. Option A: Run with Aspire (Local Development Only)

The easiest way to run the entire application stack for local development is using .NET Aspire orchestration:

> **Note**: Aspire is only for local development. Production deployments use Docker containers without Aspire components.

#### Prerequisites
- .NET 9.0 SDK
- Docker Desktop

#### Start the Application
```bash
cd src/Clercq.It.AppHost
dotnet run
```

This will automatically:
- Launch the Aspire Dashboard at `http://localhost:15888` 
- Start PostgreSQL with pgAdmin web interface
- Launch the API with automatic database connection and migrations
- Provide comprehensive observability, logging, and monitoring

The Aspire Dashboard provides a unified view of all services, logs, traces, and metrics.

### 2. Option B: Manual Setup

If you prefer to run services manually or don't want to use Aspire:

#### Database Setup

#### Option A: Docker (Recommended)
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

#### Option B: Local PostgreSQL Installation
1. Install PostgreSQL 16+
2. Create database and user:
```sql
CREATE DATABASE "ClercqItDb";
CREATE USER clercq_user WITH PASSWORD 'clercq_pass';
GRANT ALL PRIVILEGES ON DATABASE "ClercqItDb" TO clercq_user;
```

### 3. Backend Setup (.NET API)

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

### 4. Run the API
```bash
cd src/ClercqIt.Api
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7000`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:7000/swagger`

### 5. Frontend Setup (Next.js)

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

### 1. Database Changes
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

### 2. Adding New Features

Follow Clean Architecture patterns:

1. **Domain**: Add entities, value objects, repository interfaces
2. **Application**: Add queries/commands, handlers, DTOs, validators
3. **Infrastructure**: Add repository implementations, entity configurations
4. **API**: Add minimal API endpoints in feature folders

### 3. Testing

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

### 4. Building and Running

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

### Docker Compose (Future)
```yaml
version: '3.8'
services:
  api:
    build: ./src
    ports:
      - "80:80"
    depends_on:
      - postgres
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=ClercqItDb;Username=clercq_user;Password=clercq_pass;
  
  postgres:
    image: postgres:16
    environment:
      - POSTGRES_DB=ClercqItDb
      - POSTGRES_USER=clercq_user  
      - POSTGRES_PASSWORD=clercq_pass
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

## Troubleshooting

### Common Issues

#### 1. Port Already in Use
```bash
# Find process using port 5000/7000
lsof -i :5000
lsof -i :7000

# Kill process
kill -9 <PID>
```

#### 2. Database Connection Failed
- Verify PostgreSQL is running: `docker ps` or check local service
- Check connection string in `appsettings.json`
- Verify firewall settings allow port 5432

#### 3. Migration Issues
```bash
# Reset migrations (CAUTION: drops all data)
dotnet ef database drop --startup-project ../ClercqIt.Api
dotnet ef migrations remove --startup-project ../ClercqIt.Api
dotnet ef migrations add InitialCreate --startup-project ../ClercqIt.Api
dotnet ef database update --startup-project ../ClercqIt.Api
```

#### 4. Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet restore  
dotnet build
```

#### 5. Package Version Conflicts
The project may show warnings about EntityFrameworkCore.Relational version conflicts. These are harmless and will be resolved in future updates.

## IDE Configuration

### Visual Studio 2022
- Install ASP.NET Core workload
- Install Entity Framework Core tools
- Set ClercqIt.Api as startup project

### VS Code
Install recommended extensions:
- C# for Visual Studio Code
- .NET Extension Pack
- REST Client (for API testing)
- PostgreSQL (for database browsing)

### Rider
- Enable .NET Core support
- Configure database connection
- Set run configuration for ClercqIt.Api

## Next Steps

Once your development environment is set up:
1. Review the [Architecture Documentation](./architecture.md)
2. Learn about [Aspire Orchestration](./aspire.md) for enhanced development experience
3. Explore the API endpoints at `https://localhost:7000/swagger` or `https://localhost:7000/scalar/v1`
4. Check the test coverage with `dotnet test --collect:"XPlat Code Coverage"`
5. Start developing new features following Clean Architecture patterns