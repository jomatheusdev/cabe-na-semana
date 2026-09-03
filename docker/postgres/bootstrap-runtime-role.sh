#!/bin/sh
set -eu

: "${POSTGRES_DB:?POSTGRES_DB is required}"
: "${POSTGRES_USER:?POSTGRES_USER is required}"
: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}"
: "${APP_DB_USER:?APP_DB_USER is required}"
: "${APP_DB_PASSWORD:?APP_DB_PASSWORD is required}"

PGHOST="${PGHOST:-postgres}"
PGPORT="${PGPORT:-5432}"

if [ "$APP_DB_USER" = "$POSTGRES_USER" ]; then
  echo "The runtime PostgreSQL role must differ from the bootstrap role." >&2
  exit 1
fi

case "$POSTGRES_DB" in
  '' | *[!A-Za-z0-9_-]*)
    echo "POSTGRES_DB must contain only letters, numbers, underscores, or hyphens." >&2
    exit 1
    ;;
esac

case "$APP_DB_USER" in
  '' | *[!A-Za-z0-9_-]*)
    echo "APP_DB_USER must contain only letters, numbers, underscores, or hyphens." >&2
    exit 1
    ;;
esac

export APP_DB_USER APP_DB_PASSWORD PGHOST PGPORT
export PGPASSWORD="$POSTGRES_PASSWORD"
export PGCONNECT_TIMEOUT="${PGCONNECT_TIMEOUT:-10}"

psql \
  --no-psqlrc \
  --username="$POSTGRES_USER" \
  --dbname="$POSTGRES_DB" \
  --set=ON_ERROR_STOP=1 <<'SQL'
\getenv app_user APP_DB_USER
\getenv app_password APP_DB_PASSWORD
\getenv app_database POSTGRES_DB
\getenv bootstrap_user POSTGRES_USER

SET password_encryption = 'scram-sha-256';

SELECT format(
    'CREATE ROLE %I WITH LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS',
    :'app_user',
    :'app_password'
)
WHERE NOT EXISTS (
    SELECT 1
    FROM pg_catalog.pg_roles
    WHERE rolname = :'app_user'
)
\gexec

SELECT format(
    'ALTER ROLE %I WITH LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS',
    :'app_user',
    :'app_password'
)
\gexec

SELECT format('REVOKE %I FROM %I', granted_role.rolname, member_role.rolname)
FROM pg_catalog.pg_auth_members AS membership
JOIN pg_catalog.pg_roles AS granted_role ON granted_role.oid = membership.roleid
JOIN pg_catalog.pg_roles AS member_role ON member_role.oid = membership.member
WHERE member_role.rolname = :'app_user'
\gexec

REVOKE ALL PRIVILEGES ON DATABASE :"app_database" FROM :"app_user";
GRANT CONNECT ON DATABASE :"app_database" TO :"app_user";

REVOKE ALL PRIVILEGES ON SCHEMA public FROM :"app_user";
GRANT USAGE, CREATE ON SCHEMA public TO :"app_user";

REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM :"app_user";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO :"app_user";

REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public FROM :"app_user";
GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO :"app_user";

ALTER DEFAULT PRIVILEGES FOR ROLE :"bootstrap_user" IN SCHEMA public
    REVOKE ALL ON TABLES FROM :"app_user";
ALTER DEFAULT PRIVILEGES FOR ROLE :"bootstrap_user" IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO :"app_user";

ALTER DEFAULT PRIVILEGES FOR ROLE :"bootstrap_user" IN SCHEMA public
    REVOKE ALL ON SEQUENCES FROM :"app_user";
ALTER DEFAULT PRIVILEGES FOR ROLE :"bootstrap_user" IN SCHEMA public
    GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO :"app_user";

ALTER ROLE :"app_user" IN DATABASE :"app_database" SET search_path = public;
SQL

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
exec /bin/sh "$script_directory/verify-runtime-role.sh"
