using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceLayer.Mesh.Messaging;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddMessagingServices(this IServiceCollection services, bool isLocalEnvironment)
    {
        var queueClientOptions = new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        };

        // Register QueueClients as singletons
        services.AddSingleton(_ =>
        {
            if (isLocalEnvironment)
            {
                var connectionString = EnvironmentVariables.GetRequired("AzureWebJobsStorage");
                return new QueueServiceClient(connectionString, queueClientOptions);
            }

            var meshStorageAccountUrl = EnvironmentVariables.GetRequired("MeshStorageAccountUrl");
            return new QueueServiceClient(new Uri(meshStorageAccountUrl), new ManagedIdentityCredential(), queueClientOptions);
        });

        services.AddSingleton<IFileExtractQueueClient, FileExtractQueueClient>();
        services.AddSingleton<IFileTransformQueueClient, FileTransformQueueClient>();

        return services;
    }
}
