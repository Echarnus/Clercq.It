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

#### `SCALEWAY_PROJECT_ID`
- **Description**: Your Scaleway project ID
- **How to get**:
  1. Go to [Scaleway Console](https://console.scaleway.com/)
  2. Navigate to your project (or create a new one)
  3. Click on "Project settings" in the left menu
  4. Copy the Project ID
- **Note**: Each Scaleway project belongs to an organization. Make sure to use a project within your ClercqIt organization.

#### `DATABASE_PASSWORD`
- **Description**: Password for the database user (clercqit_user)
- **Requirements**:
  - Between 8 and 128 characters
  - At least one digit, one uppercase letter, one lowercase letter, and one special character
- **How to create**: Use a password manager to generate a strong password
- **Note**: This password is used for the database user that the application uses to connect to the PostgreSQL database

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

## How the Workflow Uses These Secrets

The GitHub Actions workflow (`.github/workflows/infra.yml`) uses these secrets in two ways:

1. **Terraform Variables** (TF_VAR_*):
   - Used by Terraform to configure the provider
   - Example: `TF_VAR_scaleway_project_id` → `var.scaleway_project_id` in Terraform

2. **Scaleway SDK Environment Variables** (SCW_*):
   - Used by the Scaleway SDK when making API calls
   - Required: `SCW_ACCESS_KEY`, `SCW_SECRET_KEY`
   - Recommended: `SCW_DEFAULT_ORGANIZATION_ID`, `SCW_DEFAULT_PROJECT_ID`, `SCW_DEFAULT_REGION`, `SCW_DEFAULT_ZONE`

Both are needed for proper authentication and authorization. See [SCW_ENV_VARS_FIX.md](./SCW_ENV_VARS_FIX.md) for detailed explanation.

## Troubleshooting

### Common Issues

- **Invalid Organization ID**: Ensure you're using the correct ClercqIt organization ID
- **Invalid Project ID**: Ensure you're using a valid project ID within your organization
- **API Key Permissions**: Make sure API keys have sufficient permissions for RDB and Container services
- **Region/Zone Mismatch**: Verify region and zone settings match your Scaleway setup
- **Resource Quotas**: Check if you have reached Scaleway resource limits
- **403 Forbidden Errors**: This usually means the SCW_DEFAULT_* environment variables are missing or incorrect. The workflow now sets these automatically from your secrets. See [SCW_ENV_VARS_FIX.md](./SCW_ENV_VARS_FIX.md) for details.

### Getting Help

If you encounter issues:
1. Check Scaleway console for error messages
2. Review GitHub Actions logs for detailed error information  
3. Verify all secrets are correctly set in GitHub
4. Consult [Scaleway API documentation](https://developers.scaleway.com/)
5. See [SCW_ENV_VARS_FIX.md](./SCW_ENV_VARS_FIX.md) for common 403 Forbidden error resolution