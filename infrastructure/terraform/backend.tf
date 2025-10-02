# Backend configuration for Terraform state management
# 
# This backend uses Scaleway Object Storage (S3-compatible) to store Terraform state
# This ensures state persists between workflow runs and prevents resource conflicts
#
# Prerequisites:
# 1. Create a Scaleway Object Storage bucket named "clercq-it-terraform-state"
# 2. Set up access credentials via environment variables or AWS credentials file
#
# Environment variables required:
# - AWS_ACCESS_KEY_ID (set to Scaleway access key)
# - AWS_SECRET_ACCESS_KEY (set to Scaleway secret key)
#
# Note: We use the S3 backend because Scaleway Object Storage is S3-compatible
# and there's no dedicated Scaleway backend provider.
#
# To initialize this backend:
#   terraform init -backend-config="access_key=$SCW_ACCESS_KEY" -backend-config="secret_key=$SCW_SECRET_KEY"
