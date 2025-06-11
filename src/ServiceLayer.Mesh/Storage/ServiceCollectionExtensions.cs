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
            var containerName = EnvironmentVariables.GetRequired("MeshBlobContainerName");

            if (isLocalEnvironment)
            {
                return new BlobContainerClient(EnvironmentVariables.GetRequired("AzureWebJobsStorage"),containerName);
            }

            var meshBlobStorageUrl = EnvironmentVariables.GetRequired("MeshBlobStorageUrl");

            var serviceClient = new BlobServiceClient(new Uri(meshBlobStorageUrl), new ManagedIdentityCredential());
            return serviceClient.GetBlobContainerClient(containerName);
        });

        services.AddSingleton<IMeshFilesBlobStore, MeshFilesBlobStore>();

        return services;
    }
}
