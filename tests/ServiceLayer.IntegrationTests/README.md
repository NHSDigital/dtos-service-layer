# Integration Tests

The integration tests start up docker containers using [compose.yaml](../../compose.yaml) and provide them with the environment variables defined in [.env.tests](.env.tests). Once the containers are up, the integration tests are executed, after the tests have finished, the containers are stopped.

## How to run the tests

To run the integration tests, you also need to pass the following environment variables as arguments when executing `dotnet test`:

- AZURITE_ACCOUNT_KEY
- AZURITE_ACCOUNT_NAME
- AZURITE_BLOB_PORT
- MESH_INGEST_PORT
- MESH_SANDBOX_PORT
- MESH_BLOB_CONTAINER_NAME
- DATABASE_CONNECTION_STRING

E.g.

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
