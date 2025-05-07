#!/bin/bash
set -e

# Check for sqlcmd
if [ -d "/opt/mssql-tools18/bin" ]; then
    SQLCMD_PATH="/opt/mssql-tools18/bin/sqlcmd"
elif [ -d "/opt/mssql-tools/bin" ]; then
    SQLCMD_PATH="/opt/mssql-tools/bin/sqlcmd"
else
    echo "ERROR: sqlcmd not found in either /opt/mssql-tools18/bin or /opt/mssql-tools/bin"
    exit 1
fi

echo "Using sqlcmd from: $SQLCMD_PATH"
echo "SQL Server password length: ${#MSSQL_SA_PASSWORD}"
echo "Database name: $DB_NAME"

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to start..."
for i in $(seq 1 60); do
    echo "Attempt $i: Checking if SQL Server is ready..."

    # Add both -N and -C to try to address the SSL issue
    "${SQLCMD_PATH}" -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -Q "SELECT @@SERVERNAME AS ServerName" -C -N

    if [ $? -eq 0 ]; then
        echo "SQL Server is ready!"
        break
    else
        echo "SQL Server not ready yet (exit code: $?), waiting..."

        # Try with additional connection option to trust server certificate
        echo "Trying with TrustServerCertificate=yes..."
        "${SQLCMD_PATH}" -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -Q "SELECT @@SERVERNAME AS ServerName" -C -N --driver-conn-opts "TrustServerCertificate=yes"

        sleep 3
    fi

    if [ $i -eq 60 ]; then
        echo "ERROR: Timed out waiting for SQL Server to start"
        exit 1
    fi
done

# Create database if it doesn't exist
echo "Creating database if it doesn't exist..."
"${SQLCMD_PATH}" -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'${DB_NAME}') BEGIN CREATE DATABASE [${DB_NAME}]; END" -C -N

# Run initialization script
echo "Running initialization script..."
SCRIPT_DIR="$(dirname "$0")"
"${SQLCMD_PATH}" -S localhost -d "${DB_NAME}" -U sa -P "${MSSQL_SA_PASSWORD}" -i "${SCRIPT_DIR}/InitialTestData.sql" -C -N

echo "Database initialization completed successfully"
