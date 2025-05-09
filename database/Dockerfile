FROM mcr.microsoft.com/mssql-tools:latest


WORKDIR /database
COPY db-setup-entrypoint.sh /database/
COPY create_database_statement.sql /database/
RUN chmod +x /database/db-setup-entrypoint.sh


ENTRYPOINT ["/database/db-setup-entrypoint.sh"]

