# GitHub Secrets Setup Guide

This document describes the required GitHub secrets for the CI/CD pipelines to work properly.

## Required Secrets

### Docker Hub Configuration (Optional)

For the build pipeline to push Docker images to Docker Hub, configure these secrets:

- `DOCKER_USERNAME` - Your Docker Hub username
- `DOCKER_PASSWORD` - Your Docker Hub access token or password

**Note**: If these secrets are not configured, the build pipeline will still run and build the Docker image locally, but will skip the push step.

### Scaleway Infrastructure Configuration

For the infrastructure pipeline to deploy to Scaleway, configure these secrets:

- `SCALEWAY_ACCESS_KEY` - Your Scaleway access key
- `SCALEWAY_SECRET_KEY` - Your Scaleway secret key  
- `SCALEWAY_ORGANIZATION_ID` - Your Scaleway organization ID
- `DATABASE_PASSWORD` - Password for the PostgreSQL database user

## How to Set Up Secrets

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add each secret with its name and value

## Security Best Practices

- Use access tokens instead of passwords when possible
- Regularly rotate your secrets
- Follow the principle of least privilege
- Never commit secrets to your repository

## Testing Without Secrets

The pipelines are designed to work gracefully when secrets are missing:

- **Build Pipeline**: Will build and test code, but skip Docker Hub push
- **Infrastructure Pipeline**: Will fail if Scaleway credentials are missing
- **Deploy Pipeline**: Will skip if dependent pipelines fail

## Troubleshooting

### Docker Hub Push Issues
- Verify `DOCKER_USERNAME` and `DOCKER_PASSWORD` are set correctly
- Check that the Docker Hub access token has push permissions
- Ensure the repository exists on Docker Hub

### Scaleway Deployment Issues
- Verify all Scaleway secrets are set
- Check that the Scaleway credentials have necessary permissions
- Ensure the organization ID is correct

### General Pipeline Issues
- Check GitHub Actions logs for detailed error messages
- Verify secret names match exactly (case-sensitive)
- Ensure secrets are set at the repository level, not environment level

## References

- [GitHub Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Docker Hub Access Tokens](https://docs.docker.com/docker-hub/access-tokens/)
- [Scaleway API Keys](https://www.scaleway.com/en/docs/identity-and-access-management/iam/how-to/create-api-keys/)