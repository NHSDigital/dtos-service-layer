using Azure.Identity;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .Build();

await host.RunAsync();
