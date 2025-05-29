using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceLayer.Mesh.Storage;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddStorageServices(this IServiceCollection services, bool isLocalEnvironment)
    {
        services.AddSingleton(_ =>
        {
            var containerName = EnvironmentVariables.GetRequired("BlobContainerName");

            if (isLocalEnvironment)
            {
                return new BlobContainerClient(EnvironmentVariables.GetRequired("AzureWebJobsStorage"),containerName);
            }

            var meshStorageAccountUrl = EnvironmentVariables.GetRequired("MeshStorageAccountUrl");

            var serviceClient = new BlobServiceClient(new Uri(meshStorageAccountUrl), new ManagedIdentityCredential());
            return serviceClient.GetBlobContainerClient(containerName);
        });

        services.AddSingleton<IMeshFilesBlobStore, MeshFilesBlobStore>();

        return services;
    }
}
