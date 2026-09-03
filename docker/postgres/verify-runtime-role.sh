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

export APP_DB_USER APP_DB_PASSWORD PGHOST PGPORT
export PGPASSWORD="$POSTGRES_PASSWORD"
export PGCONNECT_TIMEOUT="${PGCONNECT_TIMEOUT:-10}"

role_state=$(psql \
  --no-psqlrc \
  --username="$POSTGRES_USER" \
  --dbname="$POSTGRES_DB" \
  --tuples-only \
  --no-align \
  --set=ON_ERROR_STOP=1 <<'SQL'
\getenv app_user APP_DB_USER

SELECT concat_ws(
    '|',
    role.rolcanlogin,
    NOT role.rolsuper,
    NOT role.rolcreatedb,
    NOT role.rolcreaterole,
    NOT role.rolinherit,
    NOT role.rolreplication,
    NOT role.rolbypassrls,
    NOT EXISTS (
        SELECT 1
        FROM pg_catalog.pg_auth_members AS membership
        WHERE membership.member = role.oid
    ),
    has_database_privilege(role.rolname, current_database(), 'CONNECT'),
    has_schema_privilege(role.rolname, 'public', 'USAGE'),
    has_schema_privilege(role.rolname, 'public', 'CREATE'),
    split_part(role.rolpassword, '$', 1) = 'SCRAM-SHA-256'
)
FROM pg_catalog.pg_authid AS role
WHERE role.rolname = :'app_user';
SQL
)

if [ "$role_state" != "t|t|t|t|t|t|t|t|t|t|t|t" ]; then
  echo "Runtime PostgreSQL role validation failed." >&2
  exit 1
fi

object_grants=$(psql \
  --no-psqlrc \
  --username="$POSTGRES_USER" \
  --dbname="$POSTGRES_DB" \
  --tuples-only \
  --no-align \
  --set=ON_ERROR_STOP=1 <<'SQL'
\getenv app_user APP_DB_USER

SELECT concat_ws(
    '|',
    COALESCE(
        bool_and(
            has_table_privilege(
                :'app_user',
                format('%I.%I', schemaname, tablename),
                'SELECT,INSERT,UPDATE,DELETE'
            )
        ),
        true
    ),
    COALESCE(
        (
            SELECT bool_and(
                has_sequence_privilege(
                    :'app_user',
                    format('%I.%I', sequence_schema, sequence_name),
                    'USAGE,SELECT,UPDATE'
                )
            )
            FROM information_schema.sequences
            WHERE sequence_schema = 'public'
        ),
        true
    )
)
FROM pg_catalog.pg_tables
WHERE schemaname = 'public';
SQL
)

if [ "$object_grants" != "t|t" ]; then
  echo "Runtime PostgreSQL object grants validation failed." >&2
  exit 1
fi

PGPASSWORD="$APP_DB_PASSWORD" psql \
  --no-psqlrc \
  --username="$APP_DB_USER" \
  --dbname="$POSTGRES_DB" \
  --tuples-only \
  --no-align \
  --set=ON_ERROR_STOP=1 \
  --command='SELECT 1;' >/dev/null

echo "Runtime PostgreSQL role verified with least-privilege attributes and application grants."
