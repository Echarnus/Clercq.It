# Pull Request Template

## Description
Brief description of changes made in this pull request.

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Infrastructure change (changes to Terraform, CI/CD, or deployment configuration)
- [ ] Documentation update

## Infrastructure Changes Checklist
*Complete this section if your PR includes infrastructure changes*

### Scaleway Configuration Requirements
- [ ] **Required Secrets Configured**: Verify all required GitHub secrets are set
  - [ ] `SCALEWAY_ACCESS_KEY` - Scaleway API access key
  - [ ] `SCALEWAY_SECRET_KEY` - Scaleway API secret key  
  - [ ] `SCALEWAY_ORGANIZATION_ID` - ClercqIt organization ID
  - [ ] `DATABASE_PASSWORD` - Secure PostgreSQL database password (12+ chars)

- [ ] **Optional Variables Configured** (if needed):
  - [ ] `CONTAINER_IMAGE` - Custom Docker image tag (default: `echarnus/clercq-it:latest`)
  - [ ] `CUSTOM_DOMAIN` - Custom domain name (optional, leave empty for Scaleway domain)

- [ ] **Terraform Configuration**:
  - [ ] All Terraform files follow proper formatting (`terraform fmt`)
  - [ ] Configuration validates successfully (`terraform validate`)
  - [ ] Changes tested with `terraform plan` in development environment
  - [ ] Resource naming follows project conventions (clercq-it, portfolio namespace)

- [ ] **Infrastructure Impact Assessment**:
  - [ ] Changes are backward compatible with existing infrastructure
  - [ ] Database schema changes are properly handled with migrations
  - [ ] No breaking changes to container configuration
  - [ ] Cost impact has been considered (serverless scaling maintained)

### Required Actions for Infrastructure PRs
- [ ] **Validation**: Infrastructure pipeline validation job passed
- [ ] **Plan Review**: Terraform plan output reviewed and approved
- [ ] **Documentation**: Updated relevant documentation in `/infrastructure/` or `/docs/`
- [ ] **Environment Protection**: Production deployment will require manual approval

## Testing Performed
- [ ] Unit tests pass locally
- [ ] Integration tests pass
- [ ] Manual testing completed
- [ ] Infrastructure changes tested in development environment

## Documentation Updates
- [ ] Code comments updated where necessary
- [ ] README.md updated (if applicable)
- [ ] API documentation updated (if applicable)
- [ ] Infrastructure documentation updated (if applicable)

## Screenshots
*Add screenshots for UI changes or infrastructure deployments*

## Additional Notes
*Any additional information, context, or considerations for reviewers*

---

### For Reviewers
- Verify all checklist items are completed before approval
- For infrastructure changes, ensure the Terraform plan is reviewed
- Check that required secrets and variables are properly configured
- Validate that changes follow security best practices