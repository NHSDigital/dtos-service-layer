FROM mcr.microsoft.com/mssql-tools:latest

# Use root initially to set permissions
USER root
WORKDIR /database
COPY db-setup-entrypoint.sh /database/
COPY create_database_statement.sql /database/
RUN chmod +x /database/db-setup-entrypoint.sh

# Create a non-root user if needed (mssql-tools doesn't come with a default non-root user)
RUN useradd -m -s /bin/bash mssqltools && \
    chown -R mssqltools:mssqltools /database

# Switch to non-root user for security
USER mssqltools

ENTRYPOINT ["/database/db-setup-entrypoint.sh"]
