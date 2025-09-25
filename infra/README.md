# Clercq.It Infrastructure Documentation

## Overview

This documentation covers the complete infrastructure setup for Clercq.It, including database integration, containerization, and deployment to Scaleway.

## Architecture

### Database Layer
- **PostgreSQL 16 Alpine**: Primary database
- **Entity Framework Core 8.0.8**: ORM for .NET API
- **Health Monitoring**: Database connectivity tracking via `/api/health`

### Container Architecture
- **Single Application Container**: .NET API + Next.js frontend with Nginx
- **Database Container**: PostgreSQL with persistent storage
- **Multi-stage Docker Build**: Optimized production images
- **Health Checks**: Automated monitoring and recovery

### Deployment Strategy
- **Development**: Docker Compose with local build
- **Production**: Scaleway deployment with pre-built images
- **CI/CD**: GitHub Actions automation

## Files Structure

```
├── docker-compose.yml              # Development environment
├── docker-compose.prod.yml         # Production environment  
├── infra/
│   ├── DEVELOPMENT.md              # Local development guide
│   ├── init-db/
│   │   └── 01-init.sql            # Database initialization
│   └── scaleway/
│       ├── README.md              # Scaleway deployment guide
│       ├── deploy.sh              # Deployment script
│       ├── setup-instance.sh      # Instance setup script
│       └── .env.template          # Environment template
├── src/
│   ├── ClercqIt.Api/
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs   # EF Core DbContext
│   │   │   └── HealthCheck.cs            # Health monitoring entity
│   │   └── Program.cs                    # Updated with DB support
│   └── Dockerfile                        # Application container
└── .github/workflows/deploy.yml          # Updated deployment workflow
```

## Database Integration

### Entity Framework Setup
- **Provider**: Npgsql.EntityFrameworkCore.PostgreSQL 8.0.8
- **Context**: ApplicationDbContext with HealthCheck entity
- **Migration**: EnsureCreated() for development, migrations for production
- **Fallback**: InMemory database for testing environments

### Health Monitoring
- **Endpoint**: `GET /api/health`
- **Functionality**: Tests database connectivity and logs status
- **Response**: JSON with health status and timestamp
- **Storage**: Health checks logged to database HealthChecks table

## Deployment Environments

### Development (Local)
```bash
# Start database only
docker compose up postgres -d

# Run API locally with database
export ConnectionStrings__DefaultConnection="Host=localhost;Database=clercqit;Username=clercqit;Password=devpassword"
cd src/ClercqIt.Api && dotnet run

# Full Docker Compose
docker compose up --build
```

### Production (Scaleway)
```bash
# One-time instance setup
curl -sSL https://raw.githubusercontent.com/Echarnus/Clercq.It/main/infra/scaleway/setup-instance.sh | bash

# Deploy (automated via GitHub Actions)
./infra/scaleway/deploy.sh
```

## Security Considerations

### Container Security
- Non-root user execution for all services
- Minimal attack surface with Alpine Linux base
- Network isolation between internal and external services
- Health check endpoints for monitoring

### Database Security
- Dedicated PostgreSQL user with limited privileges
- Password management via environment variables
- Internal networking for database communication
- Persistent storage with Docker volumes

### Deployment Security
- SSH key authentication for Scaleway access
- GitHub Secrets for sensitive configuration
- Firewall configuration (UFW) with minimal open ports
- Automated security updates on Scaleway instances

## Health Monitoring & Maintenance

### Automated Health Checks
- **Database**: `pg_isready` every 10 seconds
- **API**: HTTP health endpoint every 30 seconds
- **Application**: Container restart policies
- **Infrastructure**: systemd service management

### Maintenance Features
- **Log Rotation**: Daily rotation with size limits
- **Image Cleanup**: Weekly cleanup of unused Docker images
- **Database Logs**: Health check history in database
- **System Updates**: Automated security updates

## Environment Configuration

### Required Secrets (GitHub Actions)
| Secret | Description | Example |
|--------|-------------|---------|
| `SCALEWAY_IP` | Instance IP address | `51.15.x.x` |
| `SCALEWAY_USER` | SSH username | `root` |
| `SCALEWAY_SSH_KEY` | Private SSH key | `-----BEGIN RSA PRIVATE KEY-----` |
| `POSTGRES_PASSWORD` | Database password | Strong password |

### Environment Variables
| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment |
| `DOCKER_IMAGE` | `echarnus/clercq-it` | Docker image name |
| `DOCKER_IMAGE_TAG` | `latest` | Image version tag |

## Troubleshooting

### Common Issues

1. **Database Connection Failures**
   - Check PostgreSQL container status: `docker compose ps`
   - Verify connection string format
   - Test with `pg_isready`: `docker compose exec postgres pg_isready -U clercqit`

2. **Build Issues**
   - NuGet restore problems: Check network connectivity
   - Docker build cache: Use `--no-cache` flag
   - SSL certificate issues: Build locally first

3. **Deployment Issues**
   - SSH connection: Verify SSH key and IP address
   - Port conflicts: Check for services using ports 80/443
   - Health check failures: Allow time for startup (40 seconds)

### Monitoring Commands

```bash
# Container status
docker compose ps

# Service logs
docker compose logs -f postgres
docker compose logs -f app

# Database connectivity
docker compose exec postgres psql -U clercqit -d clercqit

# Health check
curl http://localhost/api/health

# System resources
docker compose exec app top
docker system df
```

## Performance Considerations

### Database Optimization
- Connection pooling via Entity Framework
- Health check result caching
- Indexed database fields where appropriate
- Regular maintenance via automated cleanup

### Container Optimization
- Multi-stage Docker builds for minimal image size
- Build caching for faster deployments
- Resource limits and health checks
- Automated restart policies

### Network Optimization
- Internal Docker networks for database communication
- Nginx reverse proxy for efficient routing
- HTTP/2 support in production
- CDN-ready static asset serving

## Future Enhancements

### Planned Improvements
1. **Database Migrations**: Formal migration system for schema changes
2. **Backup Strategy**: Automated database backups to Scaleway Object Storage
3. **Monitoring**: Integration with monitoring services (Prometheus/Grafana)
4. **Load Balancing**: Multi-instance deployment with load balancer
5. **SSL/TLS**: HTTPS support with Let's Encrypt certificates

### Scaling Considerations
- Database connection pooling tuning
- Application instance scaling
- CDN integration for static assets
- Caching layer implementation (Redis)
- Monitoring and alerting setup

This infrastructure provides a production-ready foundation for the Clercq.It application with comprehensive database integration, automated deployment, and robust monitoring capabilities.