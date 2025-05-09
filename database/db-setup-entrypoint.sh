#!/bin/bash
echo "🔧 Starting DB setup..."

# Check if the SQL script exists
if [ ! -f /database/create_database_statement.sql ]; then
  echo "❌ SQL script not found!"
  exit 1
fi

echo "📡 Connecting to SQL Server at db..."
until /opt/mssql-tools/bin/sqlcmd -S db -U SA -P "${DATABASE_PASSWORD}" -Q "SELECT 1;" > /dev/null 2>&1
do
    echo "⏳ Waiting for SQL Server..."
    sleep 5
done
echo "✅ SQL Server is ready."

# Run the SQL setup script to create the database
/opt/mssql-tools/bin/sqlcmd -S db -U SA -P "${DATABASE_PASSWORD}" -d master -i /database/create_database_statement.sql

echo "✅ Database setup completed."
