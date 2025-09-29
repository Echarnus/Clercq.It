# Clercq.It Infrastructure

This directory contains Terraform configurations for deploying the Clercq.It Portfolio infrastructure to Scaleway.

## Infrastructure Components

### Serverless SQL Database
- **Type**: PostgreSQL 15
- **Node Type**: `db-dev-s` (smallest instance for cost efficiency)
- **Scaling**: Manual scaling based on application needs
- **Storage**: 5GB SSD storage
- **Features**:
  - Automated backups
  - Connection pooling optimized for minimal resources
  - Secure user management

### Serverless Container
- **Scaling**: 0-1 vCPU (automatically scales to zero when not in use)
- **Memory**: 128MB allocated memory
- **Runtime**: Container running the Clercq.It application
- **Features**:
  - Auto-scaling from 0 to 1 instance
  - Environment variable injection
  - Custom domain support (optional)
  - Integrated with database

### Organization Structure
- **Organization**: ClercqIt
- **Namespace**: Portfolio
- **Environment**: Production-ready with proper tagging

## Prerequisites

1. **Scaleway Account**: Access to Scaleway with ClercqIt organization
2. **Terraform**: Version 1.0 or higher
3. **Scaleway Credentials**: Access key and secret key
4. **Container Image**: Docker image pushed to a registry (Docker Hub)

## Setup Instructions

### 1. Configure Variables

Copy the example variables file:
```bash
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars` with your actual values:
- `scaleway_organization_id`: Your ClercqIt organization ID
- `database_password`: Secure password for the database user
- `container_image`: Docker image tag (e.g., `echarnus/clercq-it:v1.0.0`)
- `custom_domain`: Optional custom domain

### 2. Initialize Terraform

```bash
terraform init
```

### 3. Plan Deployment

```bash
terraform plan
```

### 4. Deploy Infrastructure

```bash
terraform apply
```

### 5. Verify Deployment

After successful deployment, Terraform will output:
- Database endpoint and connection details
- Container URL
- Infrastructure summary

## Environment Variables

The following environment variables are automatically configured:

### Container Environment
- `DATABASE_CONNECTION_STRING`: PostgreSQL connection string
- `ASPNETCORE_ENVIRONMENT`: Set to `Production`
- `NODE_ENV`: Set to `production`

## Scaling Behavior

### Database
- **Current**: Fixed small instance (`db-dev-s`)
- **Future**: Can be upgraded to larger instances or switched to serverless offerings as they become available

### Container
- **Idle**: Automatically scales to 0 instances (no cost when not in use)
- **Active**: Scales to 1 instance when requests are received
- **Resources**: 1 vCPU, 128MB memory per instance
- **Timeout**: 30 seconds for efficient resource management

## Cost Optimization

This configuration is optimized for cost-effective operation:
- Container scales to zero when not in use
- Database uses the smallest available instance
- Minimal storage allocation
- Efficient resource limits

## Security Features

- Database user with minimal required permissions
- Environment variables for sensitive configuration
- Proper resource isolation
- Scaleway security best practices

## Monitoring and Observability

Infrastructure includes proper tagging for:
- Cost tracking by project and environment
- Resource organization
- Scaleway native monitoring integration

## Troubleshooting

### Common Issues

1. **Organization Access**: Ensure you have proper permissions in the ClercqIt organization
2. **Resource Limits**: Check Scaleway quotas if deployment fails
3. **Container Image**: Verify the Docker image is accessible and working
4. **Database Connection**: Check network connectivity and credentials

### Useful Commands

```bash
# Check infrastructure state
terraform state list

# View outputs
terraform output

# Destroy infrastructure (be careful!)
terraform destroy
```

## Integration with CI/CD

This infrastructure is designed to integrate with the existing GitHub Actions workflows:
- `build.yml`: Builds and pushes container images
- `deploy.yml`: Can be extended to deploy to this infrastructure
- `infrastructure.yml`: Manages infrastructure deployment and updates

### Pull Request Configuration

For detailed guidance on configuring pull requests with infrastructure changes, see the [Infrastructure PR Configuration Guide](../docs/INFRASTRUCTURE_PR_GUIDE.md).

Key requirements for infrastructure PRs:
- All required Scaleway secrets must be configured
- Terraform configuration must be validated
- Infrastructure impact must be assessed
- Changes require team review and approval

## Next Steps

1. Set up the infrastructure deployment workflow in GitHub Actions
2. Configure automated deployments to use this infrastructure
3. Set up monitoring and alerting
4. Configure backup and disaster recovery procedures