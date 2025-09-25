#!/bin/bash
set -e

# Scaleway instance setup script
# Run this script on a fresh Scaleway Ubuntu instance to prepare it for deployment

echo "🔧 Setting up Scaleway instance for Clercq.It deployment..."

# Update system packages
echo "📦 Updating system packages..."
apt-get update
apt-get upgrade -y

# Install Docker
echo "🐳 Installing Docker..."
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh
rm get-docker.sh

# Install Docker Compose
echo "🔨 Installing Docker Compose..."
apt-get install -y docker-compose-plugin

# Start and enable Docker service
systemctl start docker
systemctl enable docker

# Create application directory
echo "📁 Creating application directory..."
mkdir -p /opt/clercqit
cd /opt/clercqit

# Set up firewall (UFW)
echo "🔒 Configuring firewall..."
ufw --force enable
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp

# Create docker group and add user
usermod -aG docker $USER

# Install useful tools
echo "🛠️ Installing additional tools..."
apt-get install -y curl wget htop nano git

# Create systemd service for auto-start
echo "⚙️ Creating systemd service..."
cat > /etc/systemd/system/clercqit.service << EOF
[Unit]
Description=Clercq.It Application
Requires=docker.service
After=docker.service

[Service]
Type=oneshot
RemainAfterExit=true
WorkingDirectory=/opt/clercqit
ExecStart=/usr/bin/docker compose up -d
ExecStop=/usr/bin/docker compose down
TimeoutStartSec=0

[Install]
WantedBy=multi-user.target
EOF

systemctl enable clercqit.service

# Set up log rotation for Docker containers
echo "📋 Setting up log rotation..."
cat > /etc/logrotate.d/docker-containers << EOF
/var/lib/docker/containers/*/*.log {
    rotate 5
    daily
    compress
    size=10M
    missingok
    delaycompress
    copytruncate
}
EOF

# Create maintenance script
echo "🔧 Creating maintenance script..."
cat > /opt/clercqit/maintenance.sh << 'EOF'
#!/bin/bash
# Clercq.It maintenance script

echo "🧹 Running maintenance tasks..."

# Clean up Docker system
docker system prune -f
docker volume prune -f

# Clean up logs older than 7 days
find /var/lib/docker/containers -name "*.log" -mtime +7 -delete

echo "✅ Maintenance completed"
EOF

chmod +x /opt/clercqit/maintenance.sh

# Add maintenance cron job
echo "⏰ Setting up maintenance cron job..."
(crontab -l 2>/dev/null || echo "") | { cat; echo "0 2 * * 0 /opt/clercqit/maintenance.sh >> /var/log/clercqit-maintenance.log 2>&1"; } | crontab -

echo "✅ Scaleway instance setup completed!"
echo ""
echo "Next steps:"
echo "1. Copy your deployment files to /opt/clercqit/"
echo "2. Configure your .env file with the correct values"
echo "3. Run: systemctl start clercqit.service"
echo ""
echo "Your instance is ready for Clercq.It deployment! 🎉"