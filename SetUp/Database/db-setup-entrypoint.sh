#!/bin/bash

echo "🔧 Starting DB setup..."
echo "📡 Connecting to SQL Server at ${DATABASE_HOST}..."

# Wait for SQL Server to be ready
until /opt/mssql-tools/bin/sqlcmd -S ${DATABASE_HOST} -U SA -P "${DATABASE_PASSWORD}" -Q "SELECT 1;" > /dev/null 2>&1
do
    echo "⏳ Waiting for SQL Server..."
    sleep 5
done

echo "✅ SQL Server is ready."

# Run setup SQL
/opt/mssql-tools/bin/sqlcmd -S ${DATABASE_HOST} -U SA -P "${DATABASE_PASSWORD}" -d master -i create_database_statement.sql

echo "✅ Database setup completed."
