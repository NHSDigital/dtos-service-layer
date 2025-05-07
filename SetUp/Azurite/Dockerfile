FROM node:16-alpine

# Install Azurite and curl
RUN npm install -g azurite && \
    apk add --no-cache curl

# Copy the initialization script
COPY init-azurite.sh /init-azurite.sh
RUN chmod +x /init-azurite.sh

# Create data directory
RUN mkdir -p /data

# Run Azurite with improved parameters and then the initialization script
ENTRYPOINT ["/bin/sh", "-c", "azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --location /data --debug /data/debug.log --loose & sleep 10 && sh /init-azurite.sh"]
