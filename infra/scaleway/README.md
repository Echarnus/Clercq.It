# Infrastructure Deployment for Scaleway

This directory contains scripts and configuration files for deploying Clercq.It to Scaleway infrastructure with PostgreSQL database support.

## Architecture

The deployment uses a multi-container setup with:
- **PostgreSQL 16 Alpine**: Database server with persistent storage
- **Clercq.It Application**: .NET API + Next.js frontend in a single container
- **Docker Compose**: Container orchestration
- **Health Checks**: Monitoring and automated recovery

## Files Overview

### Production Deployment
- `deploy.sh` - Main deployment script for Scaleway instances
- `setup-instance.sh` - One-time Scaleway instance setup script
- `.env.template` - Environment configuration template

### Configuration
- `../docker-compose.prod.yml` - Production Docker Compose configuration
- `../docker-compose.yml` - Development Docker Compose configuration
- `../init-db/01-init.sql` - Database initialization script

## Quick Start

### 1. Prepare Scaleway Instance
```bash
# Run on your Scaleway Ubuntu instance
curl -sSL https://raw.githubusercontent.com/Echarnus/Clercq.It/main/infra/scaleway/setup-instance.sh | bash
```

### 2. Configure GitHub Secrets
Add these secrets to your GitHub repository:
- `SCALEWAY_IP` - IP address of your Scaleway instance
- `SCALEWAY_USER` - SSH username (usually 'root' for Scaleway)
- `SCALEWAY_SSH_KEY` - Private SSH key for instance access
- `POSTGRES_PASSWORD` - Secure password for PostgreSQL database

### 3. Deploy
Deployment happens automatically via GitHub Actions when you push to `main` branch, or manually trigger the deploy workflow.

## Manual Deployment

```bash
# Set environment variables
export SCALEWAY_IP="your.instance.ip"
export SCALEWAY_USER="root"
export SSH_KEY_PATH="~/.ssh/scaleway_key"
export POSTGRES_PASSWORD="your-secure-password"
export DOCKER_IMAGE="echarnus/clercq-it"
export VERSION="latest"

# Run deployment
./deploy.sh
```

## Local Development

```bash
# Start development environment
docker compose up -d

# Access the application
open http://localhost

# View logs
docker compose logs -f

# Stop environment
docker compose down
```

## Database

The PostgreSQL database includes:
- **Database Name**: clercqit
- **User**: clercqit
- **Port**: 5432 (internal), mapped to 5432 on host
- **Persistent Storage**: Docker volume `postgres_data`
- **Health Checks**: Automatic monitoring with pg_isready

### Database Connection String
```
Host=postgres;Database=clercqit;Username=clercqit;Password=${POSTGRES_PASSWORD}
```

## Health Monitoring

The deployment includes comprehensive health checks:
- **Database**: PostgreSQL connectivity via `pg_isready`
- **API**: HTTP health endpoint at `/api/health`
- **Application**: Container health monitoring with automatic restart

## Security Features

- **Non-root containers**: Both database and application run as non-privileged users
- **Network isolation**: Internal network for database communication
- **Secret management**: Passwords via environment variables
- **Firewall**: UFW configured for ports 22, 80, 443 only

## Maintenance

The setup includes automatic maintenance:
- **Log Rotation**: Docker container logs rotated daily
- **Image Cleanup**: Weekly cleanup of unused Docker images
- **System Updates**: Automated security updates
- **Monitoring**: systemd service for application lifecycle

## Troubleshooting

### Check Application Status
```bash
# On Scaleway instance
cd /opt/clercqit
docker compose ps
docker compose logs app
docker compose logs postgres
```

### Manual Health Check
```bash
# Test API health endpoint
curl http://localhost/api/health

# Test database connectivity
docker compose exec postgres pg_isready -U clercqit -d clercqit
```

### Restart Services
```bash
# Restart all services
systemctl restart clercqit

# Or manually with docker compose
cd /opt/clercqit
docker compose restart
```

## Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `SCALEWAY_IP` | Scaleway instance IP address | - | Yes |
| `SCALEWAY_USER` | SSH username | root | No |
| `SSH_KEY_PATH` | Path to SSH private key | ~/.ssh/id_rsa | No |
| `POSTGRES_PASSWORD` | Database password | - | Yes |
| `DOCKER_IMAGE` | Docker image name | echarnus/clercq-it | No |
| `VERSION` | Image version/tag | latest | No |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET environment | Production | No |