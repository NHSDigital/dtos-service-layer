#!/bin/bash
echo "🔧 Starting DB setup..."
echo "📡 Connecting to SQL Server at db..."
until /opt/mssql-tools/bin/sqlcmd -S db -U SA -P "${DATABASE_PASSWORD}" -Q "SELECT 1;" > /dev/null 2>&1
do
    echo "⏳ Waiting for SQL Server..."
    sleep 5
done
echo "✅ SQL Server is ready."
/opt/mssql-tools/bin/sqlcmd -S db -U SA -P "${DATABASE_PASSWORD}" -d master -i create_database_statement.sql
echo "✅ Database setup completed."
