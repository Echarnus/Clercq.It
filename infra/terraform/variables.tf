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
  description = "Seed for the db-app-password secret. Only used when the secret is first created; afterwards Secret Manager owns the value. Leave unset to generate one."
  type        = string
  default     = null
  sensitive   = true
}

variable "keycloak_base_url" {
  description = "Seed for the keycloak-base-url secret (e.g. https://lemur-16.cloud-iam.com/auth). Only used when the secret is first created."
  type        = string
  default     = null
}

variable "keycloak_realm" {
  description = "Keycloak realm name"
  type        = string
  default     = "clercqit"
}


variable "terraform_access_key" {
  description = "Access key (not the secret) of the API key Terraform runs with. Its owner is allowed in the bucket policies (storage_policies.tf), so Terraform and the deploy pipeline keep access to the buckets."
  type        = string

  validation {
    condition     = length(var.terraform_access_key) > 0
    error_message = "Set TF_VAR_terraform_access_key to the access key Terraform uses (the SCW_ACCESS_KEY value)."
  }
}
