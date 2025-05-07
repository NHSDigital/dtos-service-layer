# Use Debian base
FROM debian:bullseye-slim

# Install tools
RUN apt-get update && \
    apt-get install -y curl apt-transport-https gnupg2 && \
    curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add - && \
    curl https://packages.microsoft.com/config/debian/11/prod.list > /etc/apt/sources.list.d/mssql-release.list && \
    apt-get update && \
    ACCEPT_EULA=Y apt-get install -y mssql-tools unixodbc && \
    apt-get clean

# Add tools to PATH
ENV PATH="/opt/mssql-tools/bin:$PATH"

# Copy script and SQL
COPY db-setup-entrypoint.sh .
COPY create_database_statement.sql .

RUN chmod +x ./db-setup-entrypoint.sh

ENTRYPOINT ["./db-setup-entrypoint.sh"]
