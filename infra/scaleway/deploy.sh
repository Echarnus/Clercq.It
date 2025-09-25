#!/bin/bash
set -e

# Scaleway deployment script for Clercq.It
# This script deploys the application to a Scaleway instance using Docker Compose

# Configuration
SCALEWAY_IP="${SCALEWAY_IP}"
SCALEWAY_USER="${SCALEWAY_USER:-root}"
SSH_KEY="${SSH_KEY_PATH:-~/.ssh/id_rsa}"
DOCKER_IMAGE="${DOCKER_IMAGE:-echarnus/clercq-it}"
VERSION="${VERSION:-latest}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD}"

# Validate required environment variables
if [ -z "$SCALEWAY_IP" ]; then
    echo "Error: SCALEWAY_IP environment variable is required"
    exit 1
fi

if [ -z "$POSTGRES_PASSWORD" ]; then
    echo "Error: POSTGRES_PASSWORD environment variable is required"
    exit 1
fi

echo "🚀 Deploying Clercq.It to Scaleway..."
echo "   Instance: $SCALEWAY_IP"
echo "   Image: $DOCKER_IMAGE:$VERSION"

# Create temporary deployment directory
TEMP_DIR=$(mktemp -d)
trap "rm -rf $TEMP_DIR" EXIT

# Copy deployment files to temporary directory
cp docker-compose.prod.yml "$TEMP_DIR/docker-compose.yml"
cp infra/scaleway/.env.template "$TEMP_DIR/.env"

# Update environment file
sed -i "s/DOCKER_IMAGE_TAG=.*/DOCKER_IMAGE_TAG=$VERSION/" "$TEMP_DIR/.env"
sed -i "s/POSTGRES_PASSWORD=.*/POSTGRES_PASSWORD=$POSTGRES_PASSWORD/" "$TEMP_DIR/.env"

# Copy files to Scaleway instance
echo "📁 Copying deployment files..."
scp -i "$SSH_KEY" -o StrictHostKeyChecking=no \
    "$TEMP_DIR/docker-compose.yml" \
    "$TEMP_DIR/.env" \
    "$SCALEWAY_USER@$SCALEWAY_IP:/opt/clercqit/"

# Run deployment on Scaleway instance
echo "🐳 Deploying containers..."
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no "$SCALEWAY_USER@$SCALEWAY_IP" << EOF
    cd /opt/clercqit
    
    # Pull latest images
    docker compose pull
    
    # Stop current containers gracefully
    docker compose down --timeout 30
    
    # Start new containers
    docker compose up -d
    
    # Wait for services to be healthy
    echo "⏳ Waiting for services to be healthy..."
    timeout 120 docker compose exec -T app curl -f http://localhost/api/health || {
        echo "❌ Health check failed after 2 minutes"
        docker compose logs app
        exit 1
    }
    
    # Clean up old images
    docker image prune -f
    
    echo "✅ Deployment completed successfully!"
EOF

echo "🎉 Clercq.It has been deployed successfully to Scaleway!"
echo "🌐 Access your application at: http://$SCALEWAY_IP"