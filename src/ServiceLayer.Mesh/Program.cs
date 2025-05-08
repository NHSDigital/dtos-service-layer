using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Azure.Storage.Queues;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using NHS.MESH.Client;
using ServiceLayer.Mesh.Data;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // MESH Client config
        services
            .AddMeshClient(_ => _.MeshApiBaseUrl = Environment.GetEnvironmentVariable("MeshApiBaseUrl"))
            .AddMailbox(Environment.GetEnvironmentVariable("BSSMailBox"), new NHS.MESH.Client.Configuration.MailboxConfiguration
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

        // Register QueueClient as singleton
        services.AddSingleton(provider =>
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var queueUrl = Environment.GetEnvironmentVariable("QueueUrl");

            if (string.IsNullOrWhiteSpace(queueUrl))
                throw new InvalidOperationException("QueueUrl environment variable is not set.");

            if (environment == "Development")
            {
                return new QueueClient("UseDevelopmentStorage=true", "my-local-queue");
            }
            else
            {
                var credential = new ManagedIdentityCredential();
                return new QueueClient(new Uri(queueUrl), credential);
            }
        });
    });


// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

var app = host.Build();
await app.RunAsync();
