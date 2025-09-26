# GitHub Secrets Configuration for Infrastructure Deployment

This document describes the GitHub Secrets that need to be configured for the infrastructure deployment workflow.

## Required Secrets

Navigate to your GitHub repository → Settings → Secrets and Variables → Actions

### Scaleway Credentials

#### `SCALEWAY_ACCESS_KEY`
- **Description**: Your Scaleway API access key
- **How to get**: 
  1. Go to [Scaleway Console](https://console.scaleway.com/)
  2. Navigate to "API Keys" in your profile menu
  3. Create a new API key or use an existing one
  4. Copy the "Access Key"

#### `SCALEWAY_SECRET_KEY` 
- **Description**: Your Scaleway API secret key
- **How to get**:
  1. Same as above
  2. Copy the "Secret Key" (shown only once when creating)

#### `SCALEWAY_ORGANIZATION_ID`
- **Description**: Your ClercqIt organization ID
- **How to get**:
  1. Go to [Scaleway Console](https://console.scaleway.com/)
  2. Click on your organization name in the top-left corner
  3. Copy the Organization ID from the organization settings

### Application Secrets

#### `DATABASE_PASSWORD`
- **Description**: Secure password for the PostgreSQL database user
- **Requirements**: 
  - At least 12 characters
  - Include uppercase, lowercase, numbers, and special characters
  - Do not use common passwords
- **Example**: `MySecur3D@t@b@s3P@ssw0rd!`

## Optional Variables

These can be set as Repository Variables (not secrets) in Settings → Secrets and Variables → Actions → Variables tab:

#### `CONTAINER_IMAGE`
- **Description**: Docker image to deploy to the container
- **Default**: `echarnus/clercq-it:latest`
- **Example**: `echarnus/clercq-it:v1.2.3`

#### `CUSTOM_DOMAIN`
- **Description**: Custom domain for the application (optional)
- **Default**: (empty - uses Scaleway provided domain)
- **Example**: `portfolio.clercq.it`

## Security Best Practices

1. **Rotate Keys Regularly**: Update your Scaleway API keys periodically
2. **Principle of Least Privilege**: Ensure API keys have only necessary permissions
3. **Monitor Usage**: Check Scaleway console for unusual API activity
4. **Secure Password**: Use a password manager for the database password
5. **Environment Protection**: Enable required reviewers for production environment

## Testing Configuration

After setting up secrets, you can test the configuration by:

1. **Manual Workflow**: Use the workflow dispatch feature to run a `terraform plan`
2. **PR Test**: Create a PR with infrastructure changes to see validation results
3. **Deployment**: Merge to main branch to trigger automatic infrastructure deployment

## Troubleshooting

### Common Issues

- **Invalid Organization ID**: Ensure you're using the correct ClercqIt organization ID
- **API Key Permissions**: Make sure API keys have sufficient permissions for RDB and Container services
- **Region/Zone Mismatch**: Verify region and zone settings match your Scaleway setup
- **Resource Quotas**: Check if you have reached Scaleway resource limits

### Getting Help

If you encounter issues:
1. Check Scaleway console for error messages
2. Review GitHub Actions logs for detailed error information  
3. Verify all secrets are correctly set in GitHub
4. Consult [Scaleway API documentation](https://developers.scaleway.com/)