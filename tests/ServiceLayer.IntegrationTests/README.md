Provide the values for all environment variables when running the integration tests, e.g.

```sh
dotnet test \
    -e AZURITE_ACCOUNT_KEY="Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==" \
    -e AZURITE_ACCOUNT_NAME=devstoreaccount1 \
    -e AZURITE_BLOB_PORT=10000 \
    -e MESH_INGEST_PORT=7072 \
    -e MESH_SANDBOX_PORT=8700 \
    -e BLOB_CONTAINER_NAME=incoming-mesh-files \
    -e DATABASE_CONNECTION_STRING="Server=localhost;Database=ServiceLayer;User Id=SA;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

Also, remember to set the FileDiscoveryTimerExpression to a shorter interval e.g. every 5 seconds (*/5 * * * * *)
