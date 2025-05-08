# Use official Microsoft SQL Server image as base
FROM mcr.microsoft.com/mssql/server:2022-latest

# Switch to root to change file permissions
USER root

# Copy the setup script and SQL file to /database inside the container
COPY db-setup-entrypoint.sh /database/db-setup-entrypoint.sh
COPY create_database_statement.sql /database/create_database_statement.sql

# Make sure the entrypoint script is executable
RUN chmod +x /database/db-setup-entrypoint.sh

# Switch back to the default non-root SQL Server user
USER mssql

# Run the entrypoint script when the container starts
ENTRYPOINT ["/database/db-setup-entrypoint.sh"]
