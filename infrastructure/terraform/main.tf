# Scaleway Infrastructure Configuration for Clercq.It Portfolio

terraform {
  required_version = ">= 1.0"
  required_providers {
    scaleway = {
      source  = "scaleway/scaleway"
      version = "~> 2.0"
    }
  }

  # Backend configuration for remote state storage
  # This ensures state persists between workflow runs
  backend "s3" {
    bucket = "clercq-it-terraform-state"
    key    = "portfolio/terraform.tfstate"
    region = "us-east-1"
    endpoints = {
      s3 = "https://s3.fr-par.scw.cloud"
    }
    skip_credentials_validation = true
    skip_region_validation      = true
    skip_metadata_api_check     = true
    use_path_style              = true
  }
}

# Configure the Scaleway Provider
provider "scaleway" {
  zone            = var.scaleway_zone
  region          = var.scaleway_region
  organization_id = var.scaleway_organization_id
  project_id      = var.scaleway_project_id
}

# Reference the existing PostgreSQL Database Instance
# The database instance already exists in Scaleway and is managed outside of Terraform
# Using a data source prevents creating duplicate database instances
data "scaleway_rdb_instance" "portfolio_db" {
  name = "DB-DEV-S"
}

# Database for the application (managed by Terraform)
resource "scaleway_rdb_database" "portfolio_app_db" {
  instance_id = data.scaleway_rdb_instance.portfolio_db.id
  name        = "clercqit_portfolio"
}

# Database user for the application (managed by Terraform)
resource "scaleway_rdb_user" "portfolio_app_user" {
  instance_id = data.scaleway_rdb_instance.portfolio_db.id
  name        = "clercqit_user"
  password    = var.database_password
  is_admin    = false
}

# Reference the existing Serverless Container Namespace
# The namespace already exists in Scaleway and is managed outside of Terraform
# Using a data source prevents "409 Conflict: Namespace already exists" errors
data "scaleway_container_namespace" "portfolio" {
  name = "cae-portfolio"
}

# Serverless Container for the application
resource "scaleway_container" "portfolio_app" {
  name           = "clercq-it-app"
  namespace_id   = data.scaleway_container_namespace.portfolio.id
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
    "DATABASE_CONNECTION_STRING" = "Host=${data.scaleway_rdb_instance.portfolio_db.endpoint_ip};Port=${data.scaleway_rdb_instance.portfolio_db.endpoint_port};Database=clercqit_portfolio;Username=clercqit_user;Password=${var.database_password}"
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