#!/bin/bash
# Script that automatically sets up azurite with the required blob containers and files.

set -e

# Define the connection string
CONN_STR="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;"

echo "Setting up Azurite containers and queues..."

# Wait for Azurite to be ready
echo "Waiting for Azurite to start..."
for i in {1..30}; do
    if wget -q --spider http://127.0.0.1:10000/devstoreaccount1?comp=list; then
        echo "Azurite is ready"
        break
    fi
    echo "Waiting..."
    sleep 1
done

# Create the blob container using Azure CLI
echo "Creating blob containers..."
az storage container create --name "parman-data-service-blob" --connection-string "$CONN_STR" || echo "Container already exists"

echo "Azurite setup completed successfully!"

# Keep the script running so container doesn't exit
tail -f /dev/null
