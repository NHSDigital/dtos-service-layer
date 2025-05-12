#!/bin/bash
set -e

# Drop and recreate database for fresh start
echo "Checking if database exists..."
/opt/mssql-tools/bin/sqlcmd -S db -U "$DATABASE_USER" -P "$DATABASE_PASSWORD" -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'$DATABASE_NAME') CREATE DATABASE $DATABASE_NAME"
/opt/mssql-tools/bin/sqlcmd -S db -U SA -P "$DATABASE_PASSWORD" -Q "CREATE DATABASE $DATABASE_NAME"

# Run migrations
echo "Running migrations..."
/opt/mssql-tools/bin/sqlcmd -S db -d "$DATABASE_NAME" -U SA -P "$DATABASE_PASSWORD" -i /database/migration.sql

echo "Migration completed successfully!"
