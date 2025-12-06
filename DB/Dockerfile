FROM gradle:8.13-jdk21

WORKDIR /DB

COPY . .

# This is a temporary step to insert mock data. This should be removed once the actual `prod` and `dev` deployment is implemented.
COPY ./docker/db-data/insert_dummy_data_for_local_testing.sql ./postgresql/migrations/dev/afterMigrate__insert_mock_data.sql

RUN gradle dependencies --no-daemon

# Run `flywayClean` to ensure mock-data is cleared to avoid duplicate key conflicts.
# Remove the `flywayClean` step once actual `prod` and `dev` deployment is implemented.
ENTRYPOINT ["gradle", "flywayClean", "flywayMigrate"]
