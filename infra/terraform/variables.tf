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

variable "auth0_domain" {
  description = "Auth0 tenant domain (e.g., your-tenant.auth0.com)"
  type        = string
  sensitive   = false
}

variable "auth0_client_id" {
  description = "Auth0 application client ID"
  type        = string
  sensitive   = false
}

variable "auth0_client_secret" {
  description = "Auth0 application client secret"
  type        = string
  sensitive   = true
}

variable "auth0_audience" {
  description = "Auth0 API audience identifier"
  type        = string
  sensitive   = false
}

variable "auth0_client_redirect_url" {
  description = "Frontend URL for Auth0 OAuth redirects"
  type        = string
  default     = "https://www.clercq.it"
}