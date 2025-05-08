FROM mcr.microsoft.com/mssql-tools
WORKDIR /scripts
COPY create_database_statement.sql .
COPY db-setup-entrypoint.sh .
RUN chmod +x ./db-setup-entrypoint.sh
ENTRYPOINT ["/scripts/db-setup-entrypoint.sh"]
