# Terraform Outputs

output "database_endpoint" {
  description = "Database endpoint"
  value       = data.scaleway_rdb_instance.portfolio_db.endpoint_ip
  sensitive   = true
}

output "database_port" {
  description = "Database port"
  value       = data.scaleway_rdb_instance.portfolio_db.endpoint_port
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

output "custom_domain_url" {
  description = "Custom domain URL (if configured)"
  value       = var.custom_domain != "" ? "https://${var.custom_domain}" : ""
}

output "infrastructure_summary" {
  description = "Summary of deployed infrastructure"
  value = {
    database = {
      name     = data.scaleway_rdb_instance.portfolio_db.name
      endpoint = "${data.scaleway_rdb_instance.portfolio_db.endpoint_ip}:${data.scaleway_rdb_instance.portfolio_db.endpoint_port}"
      type     = data.scaleway_rdb_instance.portfolio_db.node_type
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
  }
}