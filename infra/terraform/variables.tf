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

variable "keycloak_base_url" {
  description = "Keycloak base URL (e.g., https://lemur-6.cloud-iam.com/auth)"
  type        = string
}

variable "keycloak_realm" {
  description = "Keycloak realm name"
  type        = string
  default     = "clercqit"
}

variable "keycloak_web_client_secret" {
  description = "Keycloak client secret for clercqit-web client"
  type        = string
  sensitive   = true
}

variable "keycloak_admin_client_secret" {
  description = "Keycloak client secret for clercqit-admin client"
  type        = string
  sensitive   = true
}

# =============================================================================
# Administration API Variables
# =============================================================================

variable "administration_container_image" {
  description = "Docker image for the Administration API container"
  type        = string
  default     = "echarnus/clercq-it-administration:latest"
}

variable "administration_database_user" {
  description = "Database user for the Administration app"
  type        = string
  default     = "admin_app_user"
}

variable "administration_database_password" {
  description = "Database password for the Administration app user"
  type        = string
  sensitive   = true
}

