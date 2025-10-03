# Backend configuration for Terraform state management
# 
# This backend uses Scaleway Object Storage (S3-compatible) to store Terraform state
# This ensures state persists between workflow runs and prevents resource conflicts
#
# Prerequisites:
# 1. Create a Scaleway Object Storage bucket named "clercq-it-terraform-state"
# 2. Set up access credentials via AWS-compatible environment variables
#
# Environment variables required:
# - AWS_ACCESS_KEY_ID (set to Scaleway access key)
# - AWS_SECRET_ACCESS_KEY (set to Scaleway secret key)
#
# Note: We use the S3 backend because Scaleway Object Storage is S3-compatible
# and there's no dedicated Scaleway backend provider.
#
# To initialize this backend:
#   export AWS_ACCESS_KEY_ID="your-scaleway-access-key"
#   export AWS_SECRET_ACCESS_KEY="your-scaleway-secret-key"
#   terraform init
#
# The backend configuration in main.tf includes:
# - endpoints block pointing to Scaleway's S3-compatible storage
# - skip_credentials_validation, skip_region_validation, skip_metadata_api_check
#   flags to prevent AWS-specific validation when using Scaleway credentials

