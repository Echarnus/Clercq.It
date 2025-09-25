# Development Environment Setup with Docker Compose

This guide helps you set up and test the Clercq.It application with PostgreSQL database locally.

## Prerequisites

- Docker and Docker Compose installed
- .NET 9.0 SDK (for local development)
- Node.js and pnpm (for local development)

## Quick Start with Docker Compose

### 1. Start PostgreSQL Only (for development)
```bash
# Start just the database
docker compose up postgres -d

# Check database is running
docker compose ps
docker compose logs postgres
```

### 2. Run Application Locally with Database
```bash
# Set environment variable for database connection
export ConnectionStrings__DefaultConnection="Host=localhost;Database=clercqit;Username=clercqit;Password=devpassword"

# Run the API
cd src/ClercqIt.Api
dotnet run

# In another terminal, run the frontend
cd src/ClercqIt.Web
npm run dev
```

### 3. Full Stack with Docker Compose
```bash
# Build and start everything
docker compose up --build

# Or start in background
docker compose up --build -d

# Check status
docker compose ps

# View logs
docker compose logs -f

# Stop everything
docker compose down
```

## Health Checks

Once running, test the health endpoints:

```bash
# Test API health (includes database connectivity)
curl http://localhost/api/health

# Test weather API
curl http://localhost/weatherforecast

# Access the web application
open http://localhost
```

## Database Management

### Connect to PostgreSQL
```bash
# Connect using psql
docker compose exec postgres psql -U clercqit -d clercqit

# Or connect from host (if psql is installed)
psql -h localhost -p 5432 -U clercqit -d clercqit
```

### Database Operations
```sql
-- List tables
\dt

-- Check health check logs
SELECT * FROM "HealthChecks" ORDER BY "Timestamp" DESC LIMIT 10;

-- Database info
\l
\d
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `POSTGRES_PASSWORD` | `devpassword` | PostgreSQL password |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET environment |
| `ConnectionStrings__DefaultConnection` | Auto-generated | Database connection string |

## Troubleshooting

### Database Connection Issues
1. Verify PostgreSQL is running: `docker compose ps`
2. Check logs: `docker compose logs postgres`
3. Test connection: `docker compose exec postgres pg_isready -U clercqit`

### Build Issues
1. SSL Certificate problems: 
   - Try building locally first: `dotnet build`
   - Use `--no-cache` flag: `docker compose build --no-cache`

### Port Conflicts
1. Check what's using port 80: `lsof -i :80`
2. Change ports in `docker-compose.yml` if needed

## Development Workflow

1. **Database First**: Start PostgreSQL container
2. **API Development**: Run .NET API locally against containerized DB
3. **Frontend Development**: Run Next.js locally
4. **Integration Testing**: Use full Docker Compose setup
5. **Production Testing**: Use `docker-compose.prod.yml`

## Data Persistence

- PostgreSQL data is stored in Docker volume `postgres_data`
- Data persists across container restarts
- To reset database: `docker compose down -v` (removes volumes)

## Security Notes

- Default password is for development only
- Change `POSTGRES_PASSWORD` for production
- Database is accessible on localhost:5432 in development
- Production setup uses internal networking only