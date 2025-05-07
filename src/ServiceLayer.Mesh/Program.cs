using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using NHS.MESH.Client;
using ServiceLayer.Mesh.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services
            .AddMeshClient(_ => _.MeshApiBaseUrl = Environment.GetEnvironmentVariable("MeshApiBaseUrl"))
            .AddMailbox(Environment.GetEnvironmentVariable("BSSMailBox"), new NHS.MESH.Client.Configuration.MailboxConfiguration
            {
                Password = Environment.GetEnvironmentVariable("MeshPassword"),
                SharedKey = Environment.GetEnvironmentVariable("MeshSharedKey"),
                //Cert = cert
            }).Build();

        services.AddDbContext<ServiceLayerDbContext>(options =>
        {
            var databaseConnectionString = Environment.GetEnvironmentVariable("DatabaseConnectionString");
            if (string.IsNullOrEmpty(databaseConnectionString))
                throw new InvalidOperationException("The connection string has not been initialized.");

            options.UseSqlServer(databaseConnectionString);
        });
    });


// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

var app = host.Build();

await app.RunAsync();
