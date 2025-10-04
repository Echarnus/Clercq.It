# Database Migrations Guide

This document describes the database migration setup for the Clercq.It application, including local development with Aspire and automated deployments via CI/CD.

## Overview

The application uses **Entity Framework Core** with **PostgreSQL** for database management. Migrations are automatically applied in local development via Aspire and deployed to production through GitHub Actions workflows.

### Key Components

1. **Clercq.It.Infrastructure.EF.Migrations** - Console application for applying migrations
2. **Aspire AppHost** - Orchestrates local PostgreSQL and runs migrations on startup
3. **GitHub Actions** - Generates migration SQL scripts and deploys to production

## Migration Architecture

### Migration Location

All EF Core migrations are stored in:
```
src/Clercq.It.Infrastructure/EF/Migrations/
```

Namespace: `Clercq.It.Infrastructure.EF.Migrations`

### Migration Console Project

The `Clercq.It.Infrastructure.EF.Migrations` project is a standalone console application that:
- Reads database connection strings from configuration
- Applies pending migrations using `DbContext.Database.MigrateAsync()`
- Provides clear console output for success/failure

**Configuration:**
- `appsettings.json` - Default connection string for local development
- Environment variables - Override connection strings (e.g., from Aspire)

## Local Development with Aspire

### Setup

When running the application with Aspire:

```bash
cd src/Clercq.It.AppHost
dotnet run
```

### What Happens

1. **PostgreSQL** container starts with persistent volume
2. **pgAdmin** web interface becomes available
3. **Migration project** runs automatically, applying all pending migrations
4. **API** waits for migrations to complete before starting
5. **Frontend** starts after API is ready

### Aspire Configuration

In `src/Clercq.It.AppHost/Program.cs`:

```csharp
// PostgreSQL with pgAdmin
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("ClercqItDb");

// Migrations run first
var migrations = builder.AddProject<Projects.Clercq_It_Infrastructure_EF_Migrations>("migrations")
    .WithReference(database)
    .WaitFor(database);

// API waits for migrations to complete
var api = builder.AddProject<Projects.ClercqIt_Api>("clercqit-api")
    .WithReference(database)
    .WaitForCompletion(migrations);
```

### Connection Details (Local)

- **Host:** localhost
- **Port:** 5432 (dynamically assigned by Aspire)
- **Database:** ClercqItDb
- **Username:** postgres (default)
- **Password:** Managed by Aspire

Access **pgAdmin** through the Aspire Dashboard to inspect the database.

## Creating New Migrations

### Command Line

From the repository root:

```bash
cd src/Clercq.It.Infrastructure

# Create a new migration
dotnet ef migrations add MigrationName --startup-project ../ClercqIt.Api

# Review the generated migration
# Files will be created in EF/Migrations/

# Apply migrations locally (if not using Aspire)
dotnet ef database update --startup-project ../ClercqIt.Api
```

### Preview Migration SQL

To see what SQL will be executed:

```bash
cd src/Clercq.It.Infrastructure
dotnet ef migrations script --startup-project ../ClercqIt.Api
```

Generate SQL for a specific migration range:

```bash
dotnet ef migrations script FromMigration ToMigration --startup-project ../ClercqIt.Api
```

### Migration Best Practices

1. **Descriptive Names**: Use clear migration names (e.g., `AddUserEmailIndex`, `CreateOrdersTable`)
2. **Small Changes**: Keep migrations focused on single features or changes
3. **Test Locally**: Always test migrations with Aspire before committing
4. **Idempotent Scripts**: Use `--idempotent` flag for production scripts
5. **Backwards Compatibility**: Consider data in `Down()` methods
6. **Review Generated SQL**: Always review the SQL that EF Core generates

## CI/CD Deployment

### Deploy Workflow

The application uses a unified **Deploy** workflow that handles testing, building, infrastructure deployment, database migrations, and container deployment in a single pipeline.

#### Workflow Structure

The Deploy workflow consists of the following jobs:

1. **Test**: Runs .NET and Next.js tests using reusable composite actions
2. **Build**: Builds and pushes Docker image, generates migration script
3. **Deploy Infrastructure**: Applies Terraform infrastructure changes
4. **Migrate Database**: Executes database migrations using the generated script
5. **Deploy Container**: Deploys the container to Scaleway

#### Migration Generation and Execution

The build job generates an idempotent migration SQL script using the `build-docker` composite action:

```yaml
- name: Build and push Docker image
  uses: ./.github/actions/build-docker
  with:
    docker-username: ${{ secrets.DOCKER_USERNAME }}
    docker-password: ${{ secrets.DOCKER_PASSWORD }}
    registry: docker.io
    image-name: echarnus/clercq-it
    sem-ver: ${{ steps.gitversion.outputs.semVer }}
    short-sha: ${{ steps.gitversion.outputs.shortSha }}
```

The migration script is then applied by the `migrate-database` composite action:

```yaml
- name: Migrate database
  uses: ./.github/actions/migrate-database
  with:
    scaleway-access-key: ${{ secrets.SCALEWAY_ACCESS_KEY }}
    scaleway-secret-key: ${{ secrets.SCALEWAY_SECRET_KEY }}
    scaleway-organization-id: ${{ secrets.SCALEWAY_ORGANIZATION_ID }}
    scaleway-project-id: ${{ secrets.SCALEWAY_PROJECT_ID }}
    database-password: ${{ secrets.DATABASE_PASSWORD }}
```

**Benefits of Unified Workflow:**
- Artifacts are shared within the same workflow run (no cross-workflow artifact issues)
- Clear dependency chain between jobs
- Easier to trace the full deployment pipeline
- Uses only official GitHub Actions

### Database Connection (Production)

The production PostgreSQL database is managed by **Scaleway RDB** (Database as a Service):

- **Provider:** Scaleway Managed PostgreSQL
- **Engine:** PostgreSQL 16
- **Instance Type:** DB-DEV-S (configurable via Terraform)
- **Backups:** Enabled by default
- **High Availability:** Configurable

Connection details are retrieved from Terraform outputs during deployment.

## Production Database

### Infrastructure (Terraform)

Database infrastructure is defined in `infrastructure/terraform/main.tf`:

```hcl
resource "scaleway_rdb_instance" "portfolio_db" {
  name           = "portfolio-db"
  node_type      = "DB-DEV-S"
  engine         = "PostgreSQL-16"
  is_ha_cluster  = false
  disable_backup = false
  user_name      = "clercqit_admin"
  password       = var.database_password
}

resource "scaleway_rdb_database" "portfolio_app_db" {
  instance_id = scaleway_rdb_instance.portfolio_db.id
  name        = "clercqit_portfolio"
}

resource "scaleway_rdb_user" "portfolio_app_user" {
  instance_id = scaleway_rdb_instance.portfolio_db.id
  name        = "clercqit_user"
  password    = var.database_password
  is_admin    = false
}
```

### Accessing Production Database

For troubleshooting or manual operations:

1. **Via Scaleway Console:**
   - Navigate to Database > Managed Databases
   - Select "portfolio-db"
   - Use connection details shown

2. **Via `psql` Client:**
   ```bash
   # Get connection details from Terraform
   cd infrastructure/terraform
   terraform output database_endpoint
   terraform output database_port
   
   # Connect
   psql -h <endpoint> -p <port> -U clercqit_user -d clercqit_portfolio
   ```

3. **Via Migration Console (Manual):**
   ```bash
   cd src/Clercq.It.Infrastructure.EF.Migrations
   
   # Update appsettings.json with production connection string
   # Then run:
   dotnet run
   ```

## Troubleshooting

### Local Development

#### Migrations Not Applied

**Problem:** API starts but database tables are missing.

**Solution:**
1. Check Aspire Dashboard for migration project logs
2. Verify PostgreSQL container is running
3. Manually run: `cd src/Clercq.It.Infrastructure.EF.Migrations && dotnet run`

#### Connection String Issues

**Problem:** "Could not connect to database" errors.

**Solution:**
1. Check Aspire is running: `http://localhost:15888`
2. Verify PostgreSQL service is healthy in dashboard
3. Check connection string in migration console logs

#### Migration Conflicts

**Problem:** Migration files conflict during merge.

**Solution:**
```bash
# Remove conflicting migration
dotnet ef migrations remove --startup-project ../ClercqIt.Api

# Pull latest changes
git pull

# Recreate your migration
dotnet ef migrations add YourMigrationName --startup-project ../ClercqIt.Api
```

### CI/CD Issues

#### Migration Script Not Generated

**Problem:** Migration script artifact not found during deployment.

**Solution:**
1. Check the Build job completed successfully in the Deploy workflow
2. Verify "Generate migration SQL script" step succeeded in the build-docker action
3. Ensure artifact was uploaded (check workflow logs)
4. The unified Deploy workflow eliminates cross-workflow artifact issues since everything runs in a single workflow

#### Migration Execution Failed

**Problem:** Migration fails to execute on production database.

**Solution:**
1. Check Terraform outputs are accessible
2. Verify database credentials secret (`DATABASE_PASSWORD`)
3. Review migration SQL for syntax errors
4. Check database logs in Scaleway console

#### Database Connection Timeout

**Problem:** Can't connect to production database from GitHub Actions.

**Solution:**
1. Verify Scaleway firewall rules allow GitHub Actions IPs
2. Check database instance is running
3. Verify credentials are correct in GitHub secrets

## Required GitHub Secrets

The following secrets must be configured in GitHub repository settings:

| Secret | Description | Used In |
|--------|-------------|---------|
| `SCALEWAY_ACCESS_KEY` | Scaleway API access key | Infrastructure, Deploy |
| `SCALEWAY_SECRET_KEY` | Scaleway API secret key | Infrastructure, Deploy |
| `SCALEWAY_ORGANIZATION_ID` | Scaleway organization ID | Infrastructure, Deploy |
| `SCALEWAY_PROJECT_ID` | Scaleway project ID | Infrastructure, Deploy |
| `DATABASE_PASSWORD` | PostgreSQL database password | Infrastructure, Deploy |

## Manual Migration Rollback

If a migration needs to be rolled back in production:

### Option 1: Via EF Core

```bash
cd src/Clercq.It.Infrastructure

# Rollback to specific migration
dotnet ef database update PreviousMigrationName --startup-project ../ClercqIt.Api \
  --connection "Host=<prod-host>;Port=<port>;Database=clercqit_portfolio;Username=clercqit_user;Password=<password>"
```

### Option 2: Via SQL Script

```bash
# Generate down migration script
cd src/Clercq.It.Infrastructure
dotnet ef migrations script CurrentMigration PreviousMigration --startup-project ../ClercqIt.Api

# Execute manually via psql
PGPASSWORD="<password>" psql -h <host> -p <port> -U clercqit_user -d clercqit_portfolio -f rollback.sql
```

### Option 3: Remove Migration (Development Only)

```bash
# ONLY for unreleased migrations in development
dotnet ef migrations remove --startup-project ../ClercqIt.Api
```

## Monitoring & Observability

### Migration Logs

- **Local:** View in Aspire Dashboard under "migrations" project logs
- **CI/CD:** View in GitHub Actions workflow run logs
- **Production:** Captured in application logs via Scaleway Cockpit

### Database Metrics

- **Scaleway Console:** Database > Managed Databases > Metrics
- **Cockpit:** Centralized observability for all Scaleway resources

## Best Practices Summary

1. ✅ **Always test migrations locally** with Aspire before committing
2. ✅ **Use descriptive migration names** that explain what changes
3. ✅ **Review generated SQL** before deploying to production
4. ✅ **Keep migrations small** and focused on single changes
5. ✅ **Use idempotent scripts** for production deployments
6. ✅ **Monitor deployment logs** to catch migration failures early
7. ✅ **Have rollback plan** before applying breaking changes
8. ✅ **Backup production database** before major schema changes

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                      Local Development                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Aspire AppHost                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                                                            │  │
│  │  1. PostgreSQL Container (with pgAdmin)                   │  │
│  │  2. Migration Console → Applies migrations to PostgreSQL  │  │
│  │  3. API → Waits for migrations, then starts              │  │
│  │  4. Frontend → Waits for API                              │  │
│  │                                                            │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                     CI/CD Pipeline (Production)                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  deploy.yml (Unified Pipeline)                                    │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ Job 1: Test                                               │  │
│  │   - Test .NET API (uses .github/actions/test-dotnet)     │  │
│  │   - Test Next.js Frontend (uses test-frontend)           │  │
│  │                                                            │  │
│  │ Job 2: Build                                              │  │
│  │   - Generate idempotent migration SQL script             │  │
│  │   - Upload migration script as artifact                  │  │
│  │   - Build & push Docker image (uses build-docker)        │  │
│  │                                                            │  │
│  │ Job 3: Deploy Infrastructure                             │  │
│  │   - Apply Terraform (uses deploy-infra)                  │  │
│  │                                                            │  │
│  │ Job 4: Migrate Database                                  │  │
│  │   - Download migration artifact                           │  │
│  │   - Get DB credentials from Terraform outputs            │  │
│  │   - Execute migration script (uses migrate-database)     │  │
│  │                                                            │  │
│  │ Job 5: Deploy Container                                  │  │
│  │   - Deploy container to Scaleway (uses deploy-container) │  │
│  │   - Run health checks                                     │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  Composite Actions (.github/actions/)                            │
│  - test-dotnet       - Reusable .NET test logic                  │
│  - test-frontend     - Reusable Next.js test logic               │
│  - build-docker      - Build & push Docker image                 │
│  - deploy-infra      - Deploy infrastructure with Terraform      │
│  - migrate-database  - Execute database migrations               │
│  - deploy-container  - Deploy container to Scaleway              │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Production Infrastructure                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Scaleway (Managed by Terraform)                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                                                            │  │
│  │  • Managed PostgreSQL 16 (Scaleway RDB)                  │  │
│  │  • Serverless Container (API + Frontend)                 │  │
│  │  • Cockpit (Monitoring & Logs)                           │  │
│  │                                                            │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Additional Resources

- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Scaleway Database Documentation](https://www.scaleway.com/en/docs/managed-databases/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

## Related Documentation

- [Local Development Guide](./development.md) - Setting up local environment with Aspire
- [DevOps & CI/CD Pipeline](./devops.md) - Complete pipeline documentation
- [Infrastructure Guide](./infrastructure.md) - Terraform and Scaleway setup
