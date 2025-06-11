The integration tests require the following environment variables to run:
- AZURITE_ACCOUNT_KEY
- AZURITE_ACCOUNT_NAME
- AZURITE_BLOB_PORT
- MESH_INGEST_PORT
- MESH_SANDBOX_PORT
- MESH_BLOB_CONTAINER_NAME
- DATABASE_CONNECTION_STRING

They can be passed as arguments like so
```sh
dotnet test \
    -e AZURITE_ACCOUNT_KEY="Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==" \
    -e AZURITE_ACCOUNT_NAME=devstoreaccount1 \
    -e AZURITE_BLOB_PORT=10000 \
    -e MESH_INGEST_PORT=7072 \
    -e MESH_SANDBOX_PORT=8700 \
    -e MESH_BLOB_CONTAINER_NAME=incoming-mesh-files \
    -e DATABASE_CONNECTION_STRING="Server=localhost;Database=ServiceLayer;User Id=SA;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```
