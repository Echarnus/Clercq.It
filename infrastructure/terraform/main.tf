# Scaleway Infrastructure Configuration for Clercq.It Portfolio

terraform {
  required_version = ">= 1.0"
  required_providers {
    scaleway = {
      source  = "scaleway/scaleway"
      version = "~> 2.0"
    }
  }
}

# Configure the Scaleway Provider
provider "scaleway" {
  zone            = var.scaleway_zone
  region          = var.scaleway_region
  organization_id = var.scaleway_organization_id
  project_id      = var.scaleway_project_id
}

# Serverless SQL Database
resource "scaleway_rdb_instance" "portfolio_db" {
  name              = "portfolio-database"
  node_type         = "db-dev-s" # Smallest instance for serverless-like behavior
  engine            = "PostgreSQL-15"
  is_ha_cluster     = false
  disable_backup    = false
  volume_type       = "bssd"
  volume_size_in_gb = 5

  settings = {
    # Configure for minimal resource usage with scaling capabilities
    "max_connections" = "20"
    "shared_buffers"  = "32MB"
  }

  tags = [
    "project=clercq-it",
    "environment=portfolio",
    "namespace=Portfolio"
  ]
}

# Database for the application
resource "scaleway_rdb_database" "portfolio_app_db" {
  instance_id = scaleway_rdb_instance.portfolio_db.id
  name        = "clercqit_portfolio"
}

# Database user for the application
resource "scaleway_rdb_user" "portfolio_app_user" {
  instance_id = scaleway_rdb_instance.portfolio_db.id
  name        = "clercqit_user"
  password    = var.database_password
  is_admin    = false
}

# Serverless Container Namespace
resource "scaleway_container_namespace" "portfolio" {
  name        = "portfolio"
  description = "Container namespace for Clercq.It Portfolio applications"

  environment_variables = {
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "NODE_ENV"               = "production"
  }

  tags = [
    "project=clercq-it",
    "environment=portfolio",
    "namespace=Portfolio"
  ]
}

# Serverless Container for the application
resource "scaleway_container" "portfolio_app" {
  name           = "clercq-it-app"
  namespace_id   = scaleway_container_namespace.portfolio.id
  registry_image = var.container_image
  port           = 80

  # Configure scaling: 0-1 vCPU, 128MB memory
  min_scale = 0
  max_scale = 1

  # Resource limits
  memory_limit = 128  # 128MB
  cpu_limit    = 1000 # 1 vCPU (1000m)

  # Request limits for efficient scaling
  timeout = 30

  environment_variables = {
    "DATABASE_CONNECTION_STRING" = "Host=${scaleway_rdb_instance.portfolio_db.load_balancer[0].ip};Port=${scaleway_rdb_instance.portfolio_db.load_balancer[0].port};Database=clercqit_portfolio;Username=clercqit_user;Password=${var.database_password}"
    "ASPNETCORE_ENVIRONMENT"     = "Production"
    "NODE_ENV"                   = "production"
  }

  tags = [
    "project=clercq-it",
    "environment=portfolio",
    "namespace=Portfolio"
  ]
}

# Container Domain for custom domain setup (optional)
resource "scaleway_container_domain" "portfolio_domain" {
  count        = var.custom_domain != "" ? 1 : 0
  container_id = scaleway_container.portfolio_app.id
  hostname     = var.custom_domain
}