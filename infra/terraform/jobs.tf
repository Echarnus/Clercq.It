# =============================================================================
# Scheduled jobs (issue #461, docs/jobs.md)
# =============================================================================
#
# Scaleway Serverless Jobs run a command from the website image on a cron
# schedule, then stop. The command is `dotnet Clercq.It.Jobs.dll <name>`; the
# jobs themselves are C# classes in src/Clercq.It.Jobs. To add a job, write the
# class, register it in JobsHost.cs, and add one entry to scheduled_jobs below.

locals {
  scheduled_jobs = {
    # Keeps the stored MyZen session alive while the website sleeps (docs/myzen.md#keep-alive).
    "myzen-keepalive" = {
      schedule = "*/10 * * * *" # every 10 minutes, UTC
      timeout  = "2m"
      # Secret Manager secrets the job reads, by environment variable. The database
      # login every job gets is added to these below.
      secrets = {
        "MyZen__SessionKey" = "myzen-session-key"
      }
    }
  }

  # Secret Manager secrets a job can name in its `secrets` map.
  job_secret_ids = merge(
    { for name, secret in scaleway_secret.app : name => secret.id },
    { "db-runtime-connection" = scaleway_secret.db_runtime_connection.id },
  )

  # Every job reads and writes the database with the runtime login.
  job_common_secrets = {
    "ConnectionStrings__DefaultConnection" = "db-runtime-connection"
  }
}

resource "scaleway_job_definition" "scheduled" {
  for_each = local.scheduled_jobs

  # The runtime login must exist with its rights before a job uses it.
  depends_on = [scaleway_rdb_privilege.portfolio_runtime_user_privilege]

  name = "clercq-it-job-${each.key}"

  # The smallest size: a job only starts the .NET host, makes one or two calls
  # and exits. Billed per second of run time.
  cpu_limit              = 120
  memory_limit           = 256
  local_storage_capacity = 1000
  timeout                = each.value.timeout

  # The website image carries the jobs (src/Dockerfile). A job wants a full registry
  # address, so a Docker Hub short name gets docker.io in front. deploy.yml moves
  # this to the deployed image after each smoke-tested deploy (set-job-image.sh);
  # Terraform only sets the first value.
  image_uri       = length(regexall("^[^/]*[.:]", var.container_image)) > 0 ? var.container_image : "docker.io/${var.container_image}"
  startup_command = ["/usr/share/dotnet/dotnet", "/app/api/Clercq.It.Jobs.dll", each.key]

  cron {
    schedule = each.value.schedule
    timezone = "UTC"
  }

  # Not secret: the same plain values the website container gets. Feature flags
  # are shared, so a flag that switches a feature off also pauses its job.
  env = merge({
    "Database__CaCertificate" = scaleway_rdb_instance.portfolio_db.certificate
  }, { for name, enabled in var.feature_flags : "FeatureFlags__${name}" => tostring(enabled) })

  dynamic "secret_reference" {
    for_each = merge(local.job_common_secrets, each.value.secrets)

    content {
      secret_id   = local.job_secret_ids[secret_reference.value]
      environment = secret_reference.key
    }
  }

  lifecycle {
    ignore_changes = [image_uri]
  }
}

output "job_definition_ids" {
  description = "Scheduled job definition IDs by job name"
  value       = { for name, job in scaleway_job_definition.scheduled : name => job.id }
}
