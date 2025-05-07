FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

# Copy both files to the container
COPY ./init-database.sh /SetUp/Database/init-database.sh
COPY ./InitialTestData.sql /SetUp/Database/InitialTestData.sql

# Make the script executable
RUN chmod +x /SetUp/Database/init-database.sh

USER SA
# ENTRYPOINT ["/bin/bash", "-c", "/opt/mssql/bin/sqlservr & echo 'Waiting for SQL Server to initialize...' && sleep 40 && /bin/bash /SetUp/Database/init-database.sh & wait"]
ENTRYPOINT ["/bin/bash", "-c", "/opt/mssql/bin/sqlservr & sleep 30 && /bin/bash /SetUp/Database/init-database.sh & wait $!"]
