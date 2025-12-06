#!/bin/sh

set -eu

SCRIPT_PATH="$(
	cd "$(dirname "$0")" >/dev/null 2>&1
	pwd
)"
DB_MIGRATIONS_DIR="$SCRIPT_PATH/../../DB"
cd "$DB_MIGRATIONS_DIR"

./gradlew -Dflyway.configFiles="$DB_MIGRATIONS_DIR/postgresql/flyway/local.conf" flywayMigrate

# Generate jOOQ code
cd "$SCRIPT_PATH/../../WS"
./gradlew jooqCodegen

docker exec -it teamsaibot_postgres /bin/bash -c "PGPASSWORD=Teamsaibot@1234 psql -U teamsaibot -d teamsaibot < /var/lib/teamsaibot/postgresql/db-data/insert_dummy_data.sql"
