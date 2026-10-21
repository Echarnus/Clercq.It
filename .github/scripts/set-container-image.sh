#!/usr/bin/env bash
# Point a Scaleway serverless container at an image and force a fresh instance.
# Used for both deploy and rollback in deploy.yml (issue #232).
#
# Usage: set-container-image.sh <container-id> <image>
# Needs a configured `scw` CLI.
set -euo pipefail

CONTAINER_ID="${1:?container id required}"
IMAGE="${2:?image required}"
SETTLE_SECONDS="${SETTLE_SECONDS:-15}"

echo "Setting container $CONTAINER_ID to $IMAGE"
scw container container update "$CONTAINER_ID" registry-image="$IMAGE" --wait

# Containers with min_scale=0 can keep serving a cached image. Scaling to 1
# and back forces a new instance on the new image.
scw container container update "$CONTAINER_ID" min-scale=1 --wait
sleep "$SETTLE_SECONDS"
scw container container update "$CONTAINER_ID" min-scale=0 --wait
echo "Container $CONTAINER_ID now runs $IMAGE"
