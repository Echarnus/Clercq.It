# Scaleway Configuration Variables

variable "scaleway_organization_id" {
  description = "Scaleway organization ID (ClercqIt)"
  type        = string
  sensitive   = true
}

variable "scaleway_project_id" {
  description = "Scaleway project ID"
  type        = string
  sensitive   = true
}

variable "scaleway_zone" {
  description = "Scaleway zone"
  type        = string
  default     = "fr-par-1"
}

variable "scaleway_region" {
  description = "Scaleway region"
  type        = string
  default     = "fr-par"
}

variable "container_image" {
  description = "Docker image for the container application"
  type        = string
  default     = "echarnus/clercq-it:latest"
}

variable "environment" {
  description = "Environment name"
  type        = string
  default     = "portfolio"
}

variable "database_password" {
  description = "Database password for the application user"
  type        = string
  sensitive   = true
}

variable "scaleway_access_key" {
  description = "Scaleway access key for object storage"
  type        = string
  sensitive   = true
}

variable "scaleway_secret_key" {
  description = "Scaleway secret key for object storage"
  type        = string
  sensitive   = true
}

variable "quasr_tenant_id" {
  description = "Quasr.io tenant ID for authentication"
  type        = string
  sensitive   = true
}

variable "quasr_api_key" {
  description = "Quasr.io API key for authentication"
  type        = string
  sensitive   = true
}

variable "quasr_client_redirect_url" {
  description = "Client redirect URL for Quasr.io OAuth callbacks"
  type        = string
  default     = "https://www.clercq.it"
}