FROM node:16-alpine

# Install Azurite via npm
RUN npm install -g azurite

# Install curl for API calls
RUN apk add --no-cache curl

# Create directories
RUN mkdir -p /data

# Copy setup script
COPY setup.sh /setup.sh
RUN chmod +x /setup.sh

# Set the entrypoint
ENTRYPOINT ["/bin/sh", "-c", "azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --loose & sleep 10 && /setup.sh"]
