#!/bin/sh
# Executed ONLY by a manual private-network Container Apps job.
# Password values enter psql via environment and never appear in arguments/logs.
set -eu
for domain in identity catalog order fulfilment; do
  case "$domain" in
    identity) export APP_PASSWORD="$IDENTITY_PASSWORD" ;;
    catalog) export APP_PASSWORD="$CATALOG_PASSWORD" ;;
    order) export APP_PASSWORD="$ORDER_PASSWORD" ;;
    fulfilment) export APP_PASSWORD="$FULFILMENT_PASSWORD" ;;
  esac
  export APP_ROLE="${domain}_app"
  psql -X -v ON_ERROR_STOP=1 -v dbname="$domain" <<'SQL'
\getenv app_role APP_ROLE
\getenv app_password APP_PASSWORD
SELECT format('CREATE ROLE %I LOGIN', :'app_role') WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = :'app_role') \gexec
SELECT format('ALTER ROLE %I PASSWORD %L', :'app_role', :'app_password') \gexec
REVOKE ALL ON DATABASE :"dbname" FROM PUBLIC;
GRANT CONNECT ON DATABASE :"dbname" TO :"app_role";
\connect :dbname
REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO :"app_role";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO :"app_role";
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO :"app_role";
ALTER DEFAULT PRIVILEGES FOR ROLE jikeda_admin IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO :"app_role";
ALTER DEFAULT PRIVILEGES FOR ROLE jikeda_admin IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO :"app_role";
SQL
done
echo 'Four service runtime roles and database privileges configured.'
