import os
from azure.storage.blob import BlobServiceClient
from azure.storage.queue import QueueServiceClient
from azure.core.exceptions import ResourceExistsError

def setup_azurite():
    connection_string = os.getenv("AZURITE_CONNECTION_STRING")

    blob_service_client = BlobServiceClient.from_connection_string(connection_string)
    queue_service_client = QueueServiceClient.from_connection_string(connection_string)
    print("Connected to Azurite")

    try:
        blob_service_client.create_container("blob-data-service")
        queue_service_client.create_queue("queue1")
        print("Queues & blob containers created")
    except ResourceExistsError:
        print("Queues & blob containers already exist")

setup_azurite()
