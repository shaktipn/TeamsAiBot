#!/bin/sh

set -eu

SCRIPT_PATH="$(
	cd "$(dirname "$0")" >/dev/null 2>&1
	pwd
)"
DEV_DOCKER_PATH="$SCRIPT_PATH/../../DB/docker"

docker compose -f "$DEV_DOCKER_PATH/docker-compose.yml" down --remove-orphans --volumes
