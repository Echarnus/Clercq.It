# Scaleway Configuration Variables

variable "access_key" {
  description = "Scaleway access key"
  type        = string
  sensitive   = true
}

variable "secret_key" {
  description = "Scaleway secret key"
  type        = string
  sensitive   = true
}

variable "organization_id" {
  description = "Scaleway organization ID (ClercqIt)"
  type        = string
  sensitive   = true
}

variable "project_id" {
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

variable "database_password" {
  description = "Password for the database user"
  type        = string
  sensitive   = true
}

variable "custom_domain" {
  description = "Custom domain for the application (optional)"
  type        = string
  default     = ""
}

variable "environment" {
  description = "Environment name"
  type        = string
  default     = "portfolio"
}