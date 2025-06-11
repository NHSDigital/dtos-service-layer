using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NHS.MESH.Client;
using ServiceLayer;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Storage;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        var environment = EnvironmentVariables.GetRequired("ASPNETCORE_ENVIRONMENT");
        var isLocalEnvironment = environment == "Development";

        ConfigureMeshClient(services);

        services.AddCommonServices();

        services.AddMessagingServices(isLocalEnvironment);
        services.AddStorageServices(isLocalEnvironment);

        services.AddApplicationConfiguration();
        services.AddNbssAppointmentEventServices();
    });

var app = host.Build();
await app.RunAsync();
return;

void ConfigureMeshClient(IServiceCollection services)
{
    services
        .AddMeshClient(_ => _.MeshApiBaseUrl = EnvironmentVariables.GetRequired("MeshApiBaseUrl"))
        .AddMailbox(EnvironmentVariables.GetRequired("NbssMailboxId"), new NHS.MESH.Client.Configuration.MailboxConfiguration
        {
            Password = EnvironmentVariables.GetRequired("MeshPassword"),
            SharedKey = EnvironmentVariables.GetRequired("MeshSharedKey"),
        }).Build();
}
