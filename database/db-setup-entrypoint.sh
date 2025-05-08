#!/bin/bash
echo "🔧 Starting DB setup..."

# Verify that the script is executable
if [ ! -x /database/db-setup-entrypoint.sh ]; then
  echo "❌ Entry point script is not executable"
  exit 1
fi

# Verify the content of the script is present and not empty
echo "📄 Content of the script:"
cat /database/db-setup-entrypoint.sh

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
