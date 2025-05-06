using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using NHS.MESH.Client;

var host = new HostBuilder();

host.ConfigureFunctionsWebApplication();

host.ConfigureServices(services =>
    {
        services
            .AddMeshClient(_ => _.MeshApiBaseUrl = Environment.GetEnvironmentVariable("MeshApiBaseUrl"))
            .AddMailbox(Environment.GetEnvironmentVariable("BSSMailBox"), new NHS.MESH.Client.Configuration.MailboxConfiguration
            {
                Password = Environment.GetEnvironmentVariable("MeshPassword"),
                SharedKey = Environment.GetEnvironmentVariable("MeshSharedKey"),
                //Cert = cert
            })
            .Build();
    });


// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

var app = host.Build();

await app.RunAsync();
