using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Azure.Storage.Queues;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using NHS.MESH.Client;
using Azure.Storage.Blobs;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Data;
using ServiceLayer.Mesh.Storage;
using ServiceLayer.Common;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        var environment = EnvironmentVariables.GetRequired("ASPNETCORE_ENVIRONMENT");
        var isLocalEnvironment = environment == "Development";

        // MESH Client config
        services
            .AddMeshClient(_ => _.MeshApiBaseUrl = EnvironmentVariables.GetRequired("MeshApiBaseUrl"))
            .AddMailbox(EnvironmentVariables.GetRequired("NbssMailboxId"), new NHS.MESH.Client.Configuration.MailboxConfiguration
            {
                Password = EnvironmentVariables.GetRequired("MeshPassword"),
                SharedKey = EnvironmentVariables.GetRequired("MeshSharedKey"),
            }).Build();

        // EF Core DbContext
        services.AddDbContext<ServiceLayerDbContext>(options =>
        {
            var connectionString = EnvironmentVariables.GetRequired("DatabaseConnectionString");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("The connection string has not been initialized.");

            options.UseSqlServer(connectionString);
        }, ServiceLifetime.Scoped);

        var queueClientOptions = new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        };

        // Register QueueClients as singletons
        services.AddSingleton(provider =>
        {
            if (isLocalEnvironment)
            {
                var connectionString = EnvironmentVariables.GetRequired("AzureWebJobsStorage");
                return new QueueServiceClient(connectionString, queueClientOptions);
            }

            var meshStorageAccountUrl = EnvironmentVariables.GetRequired("MeshStorageAccountUrl");
            return new QueueServiceClient(new Uri(meshStorageAccountUrl), new DefaultAzureCredential(), queueClientOptions);
        });

        services.AddSingleton<IFileExtractQueueClient, FileExtractQueueClient>();
        services.AddSingleton<IFileTransformQueueClient, FileTransformQueueClient>();

        services.AddSingleton(provider =>
        {
            return new BlobContainerClient(
                EnvironmentVariables.GetRequired("AzureWebJobsStorage"),
                EnvironmentVariables.GetRequired("BlobContainerName"));
        });

        services.AddSingleton<IMeshFilesBlobStore, MeshFilesBlobStore>();

        services.AddTransient<IFileDiscoveryFunctionConfiguration, AppConfiguration>();
        services.AddTransient<IFileExtractQueueClientConfiguration, AppConfiguration>();
        services.AddTransient<IFileTransformQueueClientConfiguration, AppConfiguration>();
        services.AddTransient<IMeshHandshakeFunctionConfiguration, AppConfiguration>();
        services.AddTransient<IFileRetryFunctionConfiguration, AppConfiguration>();
        services.AddTransient<IFileTransformFunctionConfiguration, AppConfiguration>();

        services.ConfigureNbssAppointmentEvents();
    });


// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

var app = host.Build();
await app.RunAsync();
