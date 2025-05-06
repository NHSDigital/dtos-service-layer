#!/bin/sh

# Define variables
ACCOUNT_NAME="devstoreaccount1"
ACCOUNT_KEY="Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="
DATE=$(date -u '+%a, %d %b %Y %H:%M:%S GMT')
CONTAINER_NAME="parman-data-service-blob"

echo "Waiting for Azurite to be ready..."
sleep 10

# Create the container with proper authentication
echo "Creating blob container: $CONTAINER_NAME"

# Use shared key authentication
STRING_TO_SIGN="PUT\n\n\n\n\n\n\n\n\n\n\n\nx-ms-date:$DATE\nx-ms-version:2019-12-12\n/$ACCOUNT_NAME/$CONTAINER_NAME\nrestype:container"
SIGNATURE=$(echo -en "$STRING_TO_SIGN" | openssl dgst -sha256 -hmac $(echo -n $ACCOUNT_KEY | base64 -d) -binary | base64)
AUTH_HEADER="SharedKey $ACCOUNT_NAME:$SIGNATURE"

# Create container
curl -v -X PUT \
  "http://127.0.0.1:10000/$ACCOUNT_NAME/$CONTAINER_NAME?restype=container" \
  -H "x-ms-date: $DATE" \
  -H "x-ms-version: 2019-12-12" \
  -H "Authorization: $AUTH_HEADER" || echo "Container creation failed"

echo "Azurite setup completed"

# Keep the container running
tail -f /dev/null
