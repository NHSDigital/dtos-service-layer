using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Azure.Storage.Queues;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using NHS.MESH.Client;
using ServiceLayer.Mesh.Data;
using Azure.Storage.Blobs;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Messaging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isLocalEnvironment = environment == "Development";

        // MESH Client config
        services
            .AddMeshClient(_ => _.MeshApiBaseUrl = Environment.GetEnvironmentVariable("MeshApiBaseUrl"))
            .AddMailbox(Environment.GetEnvironmentVariable("NbssMailboxId"), new NHS.MESH.Client.Configuration.MailboxConfiguration
            {
                Password = Environment.GetEnvironmentVariable("MeshPassword"),
                SharedKey = Environment.GetEnvironmentVariable("MeshSharedKey"),
            }).Build();

        // EF Core DbContext
        services.AddDbContext<ServiceLayerDbContext>(options =>
        {
            var connectionString = Environment.GetEnvironmentVariable("DatabaseConnectionString");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("The connection string has not been initialized.");

            options.UseSqlServer(connectionString);
        });

        // Register QueueClients as singletons
        services.AddSingleton(provider =>
        {
            if (isLocalEnvironment)
            {
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                return new QueueServiceClient(connectionString);
            }

            var meshStorageAccountUrl = Environment.GetEnvironmentVariable("MeshStorageAccountUrl");
            return new QueueServiceClient(new Uri(meshStorageAccountUrl), new DefaultAzureCredential());
        });

        services.AddSingleton<IFileExtractQueueClient, FileExtractQueueClient>();
        services.AddSingleton<IFileTransformQueueClient, FileTransformQueueClient>();

        services.AddSingleton(provider =>
        {
            return new BlobContainerClient(
                Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                Environment.GetEnvironmentVariable("BlobContainerName"));
        });

        services.AddTransient<IFileDiscoveryFunctionConfiguration, AppConfiguration>();
        services.AddTransient<IFileExtractFunctionConfiguration, AppConfiguration>();
        services.AddTransient<IFileExtractQueueClientConfiguration, AppConfiguration>();
        services.AddTransient<IFileTransformQueueClientConfiguration, AppConfiguration>();
        services.AddTransient<IFileRetryFunctionConfiguration, AppConfiguration>();
    });


// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

var app = host.Build();
await app.RunAsync();
