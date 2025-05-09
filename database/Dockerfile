FROM mcr.microsoft.com/mssql-tools:v1

# Use root initially to set permissions
USER root
WORKDIR /database
COPY db-setup-entrypoint.sh /database/
COPY create_database_statement.sql /database/

RUN chmod +x /database/db-setup-entrypoint.sh && \
    useradd -m -s /bin/bash mssqltools && \
    chown -R mssqltools:mssqltools /database

# Switch to non-root user for security
USER mssqltools

ENTRYPOINT ["/database/db-setup-entrypoint.sh"]
