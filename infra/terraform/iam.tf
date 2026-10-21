# =============================================================================
# IAM applications (least-privilege API keys)
# =============================================================================

# Object storage access for the running container, replacing the account-wide
# API key that used to be passed in. Scoped to this project's object storage.
resource "scaleway_iam_application" "app_storage" {
  name        = "clercq-it-app-storage"
  description = "Clercq.It container: read/write blog and project images"
  tags        = ["clercq-it", "terraform"]
}

resource "scaleway_iam_policy" "app_storage" {
  name           = "clercq-it-app-storage"
  description    = "Object storage access for the Clercq.It container"
  application_id = scaleway_iam_application.app_storage.id

  rule {
    project_ids          = [var.scaleway_project_id]
    permission_set_names = ["ObjectStorageFullAccess"]
  }
}

resource "scaleway_iam_api_key" "app_storage" {
  application_id     = scaleway_iam_application.app_storage.id
  description        = "Clercq.It container object storage key (managed by Terraform)"
  default_project_id = var.scaleway_project_id
}

# Read-only access to this project's secrets for Claude cloud sessions.
# The API key is created by the owner in the Scaleway console (IAM >
# Applications > clercq-it-claude-agent > API keys) so its secret never lands
# in Terraform state, then stored as environment secrets of the Claude cloud
# environment. See docs/secrets.md.
resource "scaleway_iam_application" "claude_agent" {
  name        = "clercq-it-claude-agent"
  description = "Claude cloud sessions: read-only access to Clercq.It secrets"
  tags        = ["clercq-it", "terraform"]
}

resource "scaleway_iam_policy" "claude_agent" {
  name           = "clercq-it-claude-agent"
  description    = "Read Clercq.It secrets from Secret Manager"
  application_id = scaleway_iam_application.claude_agent.id

  rule {
    project_ids          = [var.scaleway_project_id]
    permission_set_names = ["SecretManagerReadOnly", "SecretManagerSecretAccess"]
  }
}
