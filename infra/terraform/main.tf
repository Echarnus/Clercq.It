# Scaleway Infrastructure Configuration for Clercq.It Portfolio

terraform {
  required_version = ">= 1.6.3"
  required_providers {
    scaleway = {
      source  = "scaleway/scaleway"
      version = "~> 2.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Backend configuration for remote state storage using Scaleway Object Storage
  # Credentials are provided via AWS-compatible environment variables:
  # - AWS_ACCESS_KEY_ID (set to SCW_ACCESS_KEY)
  # - AWS_SECRET_ACCESS_KEY (set to SCW_SECRET_KEY)
  # See: https://registry.terraform.io/providers/scaleway/scaleway/latest/docs/guides/backend_guide
  backend "s3" {
    bucket                      = "clercq-it-terraform-state"
    key                         = "portfolio/terraform.tfstate"
    region                      = "fr-par"
    endpoints                   = { s3 = "https://s3.fr-par.scw.cloud" }
    skip_credentials_validation = true
    skip_region_validation      = true
    skip_metadata_api_check     = true
    skip_requesting_account_id  = true
    skip_s3_checksum            = true
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

# PostgreSQL Database Instance (managed by Terraform)
resource "scaleway_rdb_instance" "portfolio_db" {
  name           = "portfolio-db"
  node_type      = "DB-DEV-S"
  engine         = "PostgreSQL-16"
  is_ha_cluster  = false
  disable_backup = false
  user_name      = "clercqit_admin"
  region         = var.scaleway_region

  # Nothing uses the admin login, so its password is generated on the fly and
  # written once: it is not in the state, Secret Manager or anywhere else
  # (issue #416). If you ever need it, reset it in the Scaleway console, or
  # bump password_wo_version to have Terraform set a new random one.
  password_wo         = ephemeral.random_password.db_admin.result
  password_wo_version = 1

  # Public endpoint the serverless containers connect to. Declared explicitly:
  # without it the provider leaves load_balancer empty and every
  # load_balancer.0 reference fails at plan time.
  load_balancer {}
}

# Database for the application (managed by Terraform)
resource "scaleway_rdb_database" "portfolio_app_db" {
  instance_id = scaleway_rdb_instance.portfolio_db.id
  name        = "clercqit_portfolio"
}

# Migration login: owns the schema and runs migration.sql in deploy.yml. The
# containers never get this password (issue #416).
resource "scaleway_rdb_user" "portfolio_app_user" {
  instance_id = scaleway_rdb_instance.portfolio_db.id
  name        = "clercqit_user"
  password    = local.secrets["db-app-password"]
  is_admin    = false
}

# Full rights on its own database only
resource "scaleway_rdb_privilege" "portfolio_app_user_privilege" {
  instance_id   = scaleway_rdb_instance.portfolio_db.id
  user_name     = scaleway_rdb_user.portfolio_app_user.name
  database_name = scaleway_rdb_database.portfolio_app_db.name
  permission    = "all"
}

# Runtime login for the website container and the content import: read and
# write rows, no DDL (issue #416). Scaleway grants readwrite on the tables
# that exist when it applies; deploy.yml also runs infra/sql/grant-app-user.sql
# as the migration login after each migration, so new tables are covered too.
resource "scaleway_rdb_user" "portfolio_runtime_user" {
  instance_id = scaleway_rdb_instance.portfolio_db.id
  name        = "clercqit_app"
  password    = random_password.db_runtime.result
  is_admin    = false
}

resource "scaleway_rdb_privilege" "portfolio_runtime_user_privilege" {
  instance_id   = scaleway_rdb_instance.portfolio_db.id
  user_name     = scaleway_rdb_user.portfolio_runtime_user.name
  database_name = scaleway_rdb_database.portfolio_app_db.name
  permission    = "readwrite"
}

# Reference the existing Serverless Container Namespace
# The namespace already exists in Scaleway and is managed outside of Terraform
# Using a data source prevents "409 Conflict: Namespace already exists" errors
data "scaleway_container_namespace" "portfolio" {
  name = "portfolio"
}

# Serverless Container for the application
resource "scaleway_container" "portfolio_app" {
  # The runtime login must exist with its rights before the container uses it.
  depends_on = [scaleway_rdb_privilege.portfolio_runtime_user_privilege]

  name           = "clercq-it-app"
  namespace_id   = data.scaleway_container_namespace.portfolio.id
  registry_image = var.container_image
  port           = 80
  protocol       = "http1"
  http_option    = "redirected"

  min_scale = 0
  max_scale = 1

  # Resource limits
  # Increased from 256MB/250m CPU to support .NET API + Next.js + nginx
  memory_limit = 512 # 512MB (was 256MB)
  cpu_limit    = 500 # 500 mvcpu (was 250m)

  # Request limits for efficient scaling
  timeout = 30

  # Feature flags (var.feature_flags) become FeatureFlags__<Name> variables; see docs/versioning.md#feature-flags.
  environment_variables = merge({
    "ObjectStorage__Endpoint"     = "https://s3.${var.scaleway_region}.scw.cloud"
    "ObjectStorage__BucketName"   = scaleway_object_bucket.blog_images.name
    "ObjectStorage__Region"       = var.scaleway_region
    "ASPNETCORE_ENVIRONMENT"      = "Production"
    "NODE_ENV"                    = "production"
    "OTEL_EXPORTER_OTLP_ENDPOINT" = "https://cockpit.fr-par.scw.cloud:4317"
    "OTEL_EXPORTER_OTLP_PROTOCOL" = "grpc"
    "Keycloak__BaseUrl"           = local.secrets["keycloak-base-url"]
    "Keycloak__Realm"             = var.keycloak_realm
    "Keycloak__ClientId"          = "clercqit-web"
    # The database's TLS certificate: the API pins it (SSL Mode VerifyCA, docs/secrets.md#database-logins).
    "Database__CaCertificate" = scaleway_rdb_instance.portfolio_db.certificate
  }, { for name, enabled in var.feature_flags : "FeatureFlags__${name}" => tostring(enabled) })

  # Secret values come from Scaleway Secret Manager (secrets.tf) and the
  # Terraform-managed storage key (iam.tf); they are not shown in the console.
  secret_environment_variables = {
    "ObjectStorage__AccessKey"             = scaleway_iam_api_key.app_storage.access_key
    "ObjectStorage__SecretKey"             = scaleway_iam_api_key.app_storage.secret_key
    "ConnectionStrings__DefaultConnection" = "${local.db_connection_base};Database=${scaleway_rdb_database.portfolio_app_db.name};Username=${scaleway_rdb_user.portfolio_runtime_user.name};Password=${random_password.db_runtime.result}"
    "OTEL_EXPORTER_OTLP_HEADERS"           = "authorization=Bearer ${scaleway_cockpit_token.portfolio_logs_token.secret_key}"
    "MyZen__Email"                         = local.myzen_login.username
    "MyZen__Password"                      = local.myzen_login.password
    "MyZen__SessionKey"                    = local.secrets["myzen-session-key"]
  }

  tags = [
    "project=clercq-it",
    "environment=portfolio",
    "namespace=Portfolio"
  ]

  # deploy.yml owns the running image (verified digest, issue #243). Without
  # this, every infra apply would reset it to var.container_image (":latest",
  # which the build pushes before verification).
  lifecycle {
    ignore_changes = [registry_image]
  }
}



# Object Storage bucket for blog images
resource "scaleway_object_bucket" "blog_images" {
  name   = "clercq-it-blog-images"
  region = var.scaleway_region

  tags = {
    project     = "clercq-it"
    environment = var.environment
    purpose     = "blog-images"
  }
}

# Object Storage bucket ACL for public read access
resource "scaleway_object_bucket_acl" "blog_images_acl" {
  bucket = scaleway_object_bucket.blog_images.name
  region = var.scaleway_region
  acl    = "public-read"
}

# Cockpit token for container logs and metrics
# Note: Scaleway Cockpit is now enabled by default on all projects
# The scaleway_cockpit resource has been deprecated and removed
resource "scaleway_cockpit_token" "portfolio_logs_token" {
  project_id = var.scaleway_project_id
  name       = "portfolio-container-logs"

  scopes {
    query_logs    = true
    write_logs    = true
    query_metrics = true
    write_metrics = true
  }
}
