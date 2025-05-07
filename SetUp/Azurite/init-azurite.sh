#!/bin/sh
set -e

echo "Initializing Azurite containers..."

# Account credentials for Azurite
ACCOUNT_NAME="devstoreaccount1"
ACCOUNT_KEY="Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="
DATE=$(date -u "+%a, %d %b %Y %H:%M:%S GMT")

# Wait for Azurite to fully initialize
sleep 5

# Create the blob container
CONTAINER_NAME="parman-data-service-blob"
echo "Creating blob container: $CONTAINER_NAME"

# Generate signature for SharedKey authentication
curl -v -X PUT "http://127.0.0.1:10000/$ACCOUNT_NAME/$CONTAINER_NAME?restype=container" \
  -H "x-ms-date: $DATE" \
  -H "x-ms-version: 2025-05-05" \
  -H "Authorization: SharedKey $ACCOUNT_NAME:$ACCOUNT_KEY"

echo "Creating queue: parman-data-service-queue"
# Create a queue for your application
curl -v -X PUT "http://127.0.0.1:10001/$ACCOUNT_NAME/parman-data-service-queue" \
  -H "x-ms-date: $DATE" \
  -H "x-ms-version: 2025-05-05" \
  -H "Authorization: SharedKey $ACCOUNT_NAME:$ACCOUNT_KEY"

echo "Azurite initialization complete"

# Keep the container running if needed
# Comment this out if using as init script in Docker Compose
tail -f /dev/null
