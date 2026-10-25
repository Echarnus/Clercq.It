#!/usr/bin/env bash
# Point every scheduled Scaleway job at an image (issue #461, docs/jobs.md).
# The jobs run the website image, so deploy.yml moves them to the verified
# digest after the container is updated, and back on a rollback.
#
# Usage: set-job-image.sh <image>
# Needs a configured `scw` CLI and jq. JOB_NAME_PREFIX picks the jobs
# (default: the "clercq-it-job-" prefix infra/terraform/jobs.tf gives them).
set -euo pipefail

IMAGE="${1:?image required}"
PREFIX="${JOB_NAME_PREFIX:-clercq-it-job-}"

# A job wants a full registry address; the website image is a Docker Hub short name.
case "${IMAGE%%/*}" in
  *.* | *:* | localhost) URI="$IMAGE" ;;
  *) URI="docker.io/$IMAGE" ;;
esac

ids=$(scw jobs definition list --output json |
  jq -r --arg prefix "$PREFIX" \
    '(if type == "array" then . else .job_definitions // [] end)[] | select(.name | startswith($prefix)) | .id')

if [[ -z "$ids" ]]; then
  echo "No scheduled job definitions named ${PREFIX}*; nothing to update."
  exit 0
fi

for id in $ids; do
  echo "Setting job definition $id to $URI"
  scw jobs definition update "$id" image-uri="$URI"
done
