using Azure.Identity;
using Azure.Messaging.EventGrid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceLayer.API.Data;

var eventGridTopicUrl = Environment.GetEnvironmentVariable("EVENT_GRID_TOPIC_URL")
    ?? throw new InvalidOperationException($"Environment variable 'EVENT_GRID_TOPIC_URL' is not set or is empty.");
var eventGridTopicKey = Environment.GetEnvironmentVariable("EVENT_GRID_TOPIC_KEY")
    ?? throw new InvalidOperationException($"Environment variable 'EVENT_GRID_TOPIC_KEY' is not set or is empty.");
var databaseConnectionString = Environment.GetEnvironmentVariable("DatabaseConnectionString")
    ?? throw new InvalidOperationException($"Environment variable 'DatabaseConnectionString' is not set or is empty.");


var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton(sp =>
        {
            var endpoint = new Uri(eventGridTopicUrl);
            if (context.HostingEnvironment.IsDevelopment())
            {
                var credentials = new Azure.AzureKeyCredential(eventGridTopicKey);
                return new EventGridPublisherClient(endpoint, credentials);
            }

            return new EventGridPublisherClient(endpoint, new ManagedIdentityCredential());
        });
        services.AddDbContext<ServiceLayerDbContext>(options =>
        {
            options.UseSqlServer(databaseConnectionString);
        });
        services.AddLogging();
    })
    .Build();

await host.RunAsync();
