-- Gives the runtime login (rows only) access to every table and sequence the
-- migration login owns, now and in the future (issue #416, docs/secrets.md#database-logins).
-- deploy.yml runs it as the migration login right after migration.sql:
--   psql ... -v app_user=clercqit_app -f infra/sql/grant-app-user.sql
-- Idempotent. The runtime login gets no DDL, no TRUNCATE and no REFERENCES.

\set ON_ERROR_STOP on

GRANT USAGE ON SCHEMA public TO :"app_user";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO :"app_user";
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO :"app_user";

-- Tables and sequences that later migrations create
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO :"app_user";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO :"app_user";

-- The migration history is for the migration login only
REVOKE ALL ON TABLE "__EFMigrationsHistory" FROM :"app_user";
