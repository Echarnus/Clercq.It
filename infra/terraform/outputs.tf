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
# Administration API Outputs
# =============================================================================

output "administration_container_url" {
  description = "Administration API container URL"
  value       = scaleway_container.administration_api.domain_name
}

output "administration_container_id" {
  description = "Administration API container ID (use this for GitHub secrets)"
  value       = scaleway_container.administration_api.id
}

output "administration_container_name" {
  description = "Administration API container name"
  value       = scaleway_container.administration_api.name
}

output "administration_database_user" {
  description = "Administration database user name"
  value       = scaleway_rdb_user.administration_app_user.name
}

output "administration_infrastructure_summary" {
  description = "Summary of Administration API infrastructure"
  sensitive   = true
  value = {
    container = {
      name      = scaleway_container.administration_api.name
      url       = scaleway_container.administration_api.domain_name
      id        = scaleway_container.administration_api.id
      port      = scaleway_container.administration_api.port
      min_scale = scaleway_container.administration_api.min_scale
      max_scale = scaleway_container.administration_api.max_scale
      memory    = "${scaleway_container.administration_api.memory_limit}MB"
      cpu       = "${scaleway_container.administration_api.cpu_limit}m"
    }
    database = {
      instance = scaleway_rdb_instance.portfolio_db.name
      database = scaleway_rdb_database.portfolio_app_db.name
      schema   = "administration"
      user     = scaleway_rdb_user.administration_app_user.name
    }
  }
}
