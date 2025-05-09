FROM mcr.microsoft.com/mssql-tools

WORKDIR /database
COPY db-setup-entrypoint.sh /database/
RUN chmod +x /database/db-setup-entrypoint.sh
# USER mssql
ENTRYPOINT ["/database/db-setup-entrypoint.sh"]
