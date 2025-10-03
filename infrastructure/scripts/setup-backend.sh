#!/bin/bash
# Setup script for Terraform backend
# This script creates the S3 bucket for Terraform state storage
# Run this once before the first terraform apply

set -e

echo "🔧 Setting up Terraform backend for Clercq.It infrastructure"
echo ""

# Check for required environment variables
if [ -z "$SCW_ACCESS_KEY" ] || [ -z "$SCW_SECRET_KEY" ]; then
    echo "❌ Error: SCW_ACCESS_KEY and SCW_SECRET_KEY environment variables must be set"
    echo ""
    echo "Example:"
    echo "  export SCW_ACCESS_KEY=your-access-key"
    echo "  export SCW_SECRET_KEY=your-secret-key"
    exit 1
fi

if [ -z "$SCW_DEFAULT_PROJECT_ID" ]; then
    echo "⚠️  Warning: SCW_DEFAULT_PROJECT_ID not set, using default project"
fi

BUCKET_NAME="clercq-it-terraform-state"
REGION="fr-par"

echo "📦 Checking if bucket '$BUCKET_NAME' exists..."

# Check if bucket exists using Scaleway API
BUCKET_EXISTS=$(curl -s -o /dev/null -w "%{http_code}" \
    -H "X-Auth-Token: $SCW_SECRET_KEY" \
    "https://s3.${REGION}.scw.cloud/${BUCKET_NAME}")

if [ "$BUCKET_EXISTS" = "200" ] || [ "$BUCKET_EXISTS" = "301" ]; then
    echo "✅ Bucket already exists, no action needed"
elif [ "$BUCKET_EXISTS" = "404" ]; then
    echo "📦 Creating S3 bucket for Terraform state..."
    
    # Create bucket using AWS CLI (Scaleway is S3-compatible)
    if command -v aws &> /dev/null; then
        AWS_ACCESS_KEY_ID="$SCW_ACCESS_KEY" \
        AWS_SECRET_ACCESS_KEY="$SCW_SECRET_KEY" \
        aws s3 mb "s3://${BUCKET_NAME}" \
            --region "$REGION" \
            --endpoint-url "https://s3.${REGION}.scw.cloud"
        echo "✅ Bucket created successfully"
    else
        echo "⚠️  AWS CLI not found. Please install it or create the bucket manually:"
        echo ""
        echo "Using Scaleway console:"
        echo "  1. Go to https://console.scaleway.com/object-storage/buckets"
        echo "  2. Click 'Create bucket'"
        echo "  3. Name: ${BUCKET_NAME}"
        echo "  4. Region: ${REGION}"
        echo "  5. Click 'Create bucket'"
        echo ""
        echo "Or install AWS CLI and run this script again:"
        echo "  pip install awscli"
        exit 1
    fi
else
    echo "❌ Unexpected response: $BUCKET_EXISTS"
    echo "Please check your credentials and try again"
    exit 1
fi

echo ""
echo "🎉 Backend setup complete!"
echo ""
echo "You can now run:"
echo "  cd infrastructure/terraform"
echo "  terraform init -backend-config=\"access_key=\$SCW_ACCESS_KEY\" -backend-config=\"secret_key=\$SCW_SECRET_KEY\""
echo ""
