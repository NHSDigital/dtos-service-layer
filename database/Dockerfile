FROM mcr.microsoft.com/mssql-tools

RUN useradd -m -s /bin/bash nonroot

WORKDIR /database
COPY db-setup-entrypoint.sh /database/

RUN chmod +x /database/db-setup-entrypoint.sh && \
    chown -R nonroot:nonroot /database

USER nonroot

ENTRYPOINT ["/database/db-setup-entrypoint.sh"]
