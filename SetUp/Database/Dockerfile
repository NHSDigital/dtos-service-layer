FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

# Create directory for SQL scripts
RUN mkdir -p /scripts/db

# Copy database scripts
COPY ./init-database.sh /scripts/
COPY ./InitialTestData.sql /scripts/db/

# Make the script executable
RUN chmod +x /scripts/init-database.sh

# Set up the entrypoint
USER mssql
ENTRYPOINT /bin/bash -c "/opt/mssql/bin/sqlservr & sleep 30 && /scripts/init-database.sh & wait"
