FROM mcr.microsoft.com/azure-storage/azurite:latest

# Install Azure CLI and wget for container and queue creation
USER root
RUN apt-get update && \
    apt-get install -y curl ca-certificates apt-transport-https lsb-release gnupg wget && \
    curl -sL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor | tee /etc/apt/trusted.gpg.d/microsoft.gpg > /dev/null && \
    echo "deb [arch=amd64] https://packages.microsoft.com/repos/azure-cli/ $(lsb_release -cs) main" | tee /etc/apt/sources.list.d/azure-cli.list && \
    apt-get update && \
    apt-get install -y azure-cli && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy setup script
COPY setup.sh /setup.sh
RUN chmod +x /setup.sh

# Set the entrypoint
ENTRYPOINT ["/bin/bash", "-c", "azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --loose & sleep 10 && /setup.sh & wait"]
