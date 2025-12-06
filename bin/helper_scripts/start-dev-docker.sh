#!/bin/sh

set -eu

SCRIPT_PATH="$(
	cd "$(dirname "$0")" >/dev/null 2>&1
	pwd
)"
DEV_DOCKER_PATH="$SCRIPT_PATH/../../DB/docker"

docker compose -f "$DEV_DOCKER_PATH/docker-compose.yml" up --build -d

until docker container exec -it teamsaibot_postgres pg_isready; do
	echo "Waiting for postgres to get ready to accept connections"
	sleep 1
done

# POST CONTAINER CREATION ACTIONS

# Run flyway migration
"$SCRIPT_PATH"/initial-db-setup-postgres.sh
