#!/usr/bin/env bash
# One-command redeploy for the production server.
# Usage (on the droplet):  cd /opt/ContractCanary && ./deploy.sh
set -euo pipefail
cd "$(dirname "$0")"

echo "==> Pulling latest code…"
git pull

echo "==> Rebuilding and restarting containers (takes a few minutes)…"
docker compose -f docker-compose.server.yml up -d --build

echo "==> Recent API logs (look for 'Initialization complete'):"
docker compose -f docker-compose.server.yml logs api --tail 30

echo "==> Done. Site: https://contract-canary.com"
