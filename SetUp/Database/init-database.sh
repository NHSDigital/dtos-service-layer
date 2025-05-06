#!/bin/bash
set -e

# Check if we need to use mssql-tools18 or mssql-tools
if [ -d "/opt/mssql-tools18/bin" ]; then
    SQLCMD_PATH="/opt/mssql-tools18/bin/sqlcmd"
elif [ -d "/opt/mssql-tools/bin" ]; then
    SQLCMD_PATH="/opt/mssql-tools/bin/sqlcmd"
else
    echo "ERROR: sqlcmd not found in either /opt/mssql-tools18/bin or /opt/mssql-tools/bin"
    exit 1
fi

echo "Using sqlcmd from: $SQLCMD_PATH"

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to start..."
for i in {1..60}; do
    if $SQLCMD_PATH -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" &> /dev/null; then
        echo "SQL Server is ready"
        break
    fi
    echo "Waiting..."
    sleep 1
done

# Create database if it doesn't exist
echo "Creating database if it doesn't exist..."
$SQLCMD_PATH -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'$DB_NAME') BEGIN CREATE DATABASE [$DB_NAME]; END"

# Run initialization script
echo "Running initialization script..."
$SQLCMD_PATH -S localhost -d $DB_NAME -U sa -P "$MSSQL_SA_PASSWORD" -i /scripts/db/InitialTestData.sql

echo "Database initialization completed successfully"
