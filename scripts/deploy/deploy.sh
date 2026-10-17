#!/usr/bin/env bash
# Deploys one tagged release with docker compose and waits until every service is healthy.
# The same script performs a rollback: run it again with the previous tag.
#
#   IMAGE_TAG=sha-1a2b3c4d5e6f REGISTRY=ghcr.io/owner scripts/deploy/deploy.sh [compose-file]
#
# Exit code 0 only when `docker compose up --wait` reports all services healthy.
set -euo pipefail

COMPOSE_FILE="${1:-docker-compose.staging.yml}"
: "${IMAGE_TAG:?IMAGE_TAG is required (for example sha-1a2b3c4d5e6f)}"
: "${REGISTRY:?REGISTRY is required (for example ghcr.io/owner)}"
export IMAGE_TAG REGISTRY

echo "Deploying ${REGISTRY}/takeout-{api,frontend}:${IMAGE_TAG} with ${COMPOSE_FILE}"
docker compose -f "$COMPOSE_FILE" pull api frontend
docker compose -f "$COMPOSE_FILE" up -d --remove-orphans --wait --wait-timeout 240

# Remember what is running so the next release knows what to roll back to.
printf '%s\n' "$IMAGE_TAG" > .deployed-tag
docker compose -f "$COMPOSE_FILE" ps
