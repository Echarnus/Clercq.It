# Terraform Outputs

output "database_endpoint" {
  description = "Database endpoint"
  value       = scaleway_rdb_instance.portfolio_db.load_balancer.0.ip
  sensitive   = true
}

output "database_port" {
  description = "Database port"
  value       = scaleway_rdb_instance.portfolio_db.load_balancer.0.port
}

output "database_name" {
  description = "Database name"
  value       = scaleway_rdb_database.portfolio_app_db.name
}

output "container_url" {
  description = "Container application URL"
  value       = scaleway_container.portfolio_app.domain_name
}

output "container_namespace_id" {
  description = "Container namespace ID"
  value       = data.scaleway_container_namespace.portfolio.id
}

output "container_id" {
  description = "Container ID"
  value       = scaleway_container.portfolio_app.id
}

output "container_name" {
  description = "Container name"
  value       = scaleway_container.portfolio_app.name
}

output "infrastructure_summary" {
  description = "Summary of deployed infrastructure"
  sensitive   = true
  value = {
    database = {
      name     = scaleway_rdb_instance.portfolio_db.name
      endpoint = "${scaleway_rdb_instance.portfolio_db.load_balancer.0.ip}:${scaleway_rdb_instance.portfolio_db.load_balancer.0.port}"
      type     = scaleway_rdb_instance.portfolio_db.node_type
    }
    container = {
      name      = scaleway_container.portfolio_app.name
      url       = scaleway_container.portfolio_app.domain_name
      min_scale = scaleway_container.portfolio_app.min_scale
      max_scale = scaleway_container.portfolio_app.max_scale
      memory    = "${scaleway_container.portfolio_app.memory_limit}MB"
      cpu       = "${scaleway_container.portfolio_app.cpu_limit}m"
    }
    namespace = {
      name = data.scaleway_container_namespace.portfolio.name
      id   = data.scaleway_container_namespace.portfolio.id
    }
    cockpit = {
      project_id = var.scaleway_project_id
      token_id   = scaleway_cockpit_token.portfolio_logs_token.id
    }
  }
}

output "cockpit_project_id" {
  description = "Scaleway Project ID for Cockpit access"
  value       = var.scaleway_project_id
  sensitive   = true
}

output "cockpit_token_id" {
  description = "Scaleway Cockpit Token ID for logs and metrics"
  value       = scaleway_cockpit_token.portfolio_logs_token.id
}

output "object_storage_bucket_name" {
  description = "Object storage bucket name for blog images"
  value       = scaleway_object_bucket.blog_images.name
}

output "object_storage_endpoint" {
  description = "Object storage endpoint for blog images"
  value       = scaleway_object_bucket.blog_images.endpoint
}

# =============================================================================
# Secret Manager and IAM Outputs
# =============================================================================

output "secret_ids" {
  description = "Secret Manager secret IDs by name (path /clercq-it)"
  value       = { for name, secret in scaleway_secret.app : name => secret.id }
}

output "claude_agent_application_id" {
  description = "IAM application to create the read-only API key for Claude cloud sessions"
  value       = scaleway_iam_application.claude_agent.id
}

output "app_storage_application_id" {
  description = "IAM application whose API key the container uses for object storage"
  value       = scaleway_iam_application.app_storage.id
}

# Verify environment (verify.tf), read by deploy.yml

output "verify_container_id" {
  description = "Verify container ID"
  value       = scaleway_container.verify_app.id
}

output "verify_container_url" {
  description = "Verify container domain"
  value       = scaleway_container.verify_app.domain_name
}

output "verify_database_name" {
  description = "Verify database name"
  value       = scaleway_rdb_database.verify.name
}

output "verify_database_user" {
  description = "Verify database user"
  value       = scaleway_rdb_user.verify.name
}

output "verify_database_password" {
  description = "Verify database password (from Secret Manager)"
  value       = local.verify_secrets["db-password"]
  sensitive   = true
}

output "verify_bucket_name" {
  description = "Verify object storage bucket"
  value       = scaleway_object_bucket.verify_images.name
}

output "verify_storage_access_key" {
  description = "Verify object storage access key"
  value       = scaleway_iam_api_key.verify_storage.access_key
  sensitive   = true
}

output "verify_storage_secret_key" {
  description = "Verify object storage secret key"
  value       = scaleway_iam_api_key.verify_storage.secret_key
  sensitive   = true
}
