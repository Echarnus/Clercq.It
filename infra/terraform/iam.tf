# =============================================================================
# IAM applications (least-privilege API keys)
# =============================================================================

# Object storage access for the running container, replacing the account-wide
# API key that used to be passed in. Objects only, and the bucket policy keeps
# it to the image bucket (storage_policies.tf).
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
    project_ids = [var.scaleway_project_id]
    # Objects only: no bucket settings and no bucket deletes. The bucket policy
    # (storage_policies.tf) then limits the key to its own bucket.
    permission_set_names = ["ObjectStorageObjectsRead", "ObjectStorageObjectsWrite", "ObjectStorageObjectsDelete"]
  }
}

resource "scaleway_iam_api_key" "app_storage" {
  application_id     = scaleway_iam_application.app_storage.id
  description        = "Clercq.It container object storage key (managed by Terraform)"
  default_project_id = var.scaleway_project_id
}

# Claude cloud sessions may list this project's secrets (names, paths,
# versions) but not read their values: no agent task needs a secret value,
# and the values include the owner's MyZen login (issue #415).
# The API key is created by the owner in the Scaleway console (IAM >
# Applications > clercq-it-claude-agent > API keys) so its secret never lands
# in Terraform state, then stored as environment secrets of the Claude cloud
# environment. See docs/secrets.md.
resource "scaleway_iam_application" "claude_agent" {
  name        = "clercq-it-claude-agent"
  description = "Claude cloud sessions: list Clercq.It secrets, no values"
  tags        = ["clercq-it", "terraform"]
}

resource "scaleway_iam_policy" "claude_agent" {
  name           = "clercq-it-claude-agent"
  description    = "List Clercq.It secrets in Secret Manager (no values)"
  application_id = scaleway_iam_application.claude_agent.id

  rule {
    project_ids          = [var.scaleway_project_id]
    permission_set_names = ["SecretManagerReadOnly"]
  }
}
