FROM mcr.microsoft.com/mssql-tools

WORKDIR /database
COPY . /database
RUN chmod +x /database/db-setup-entrypoint.sh
ENTRYPOINT ["/database/db-setup-entrypoint.sh"]
