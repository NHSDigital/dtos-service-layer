using Microsoft.Extensions.DependencyInjection;

namespace ServiceLayer.Mesh.Configuration;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddApplicationConfiguration(this IServiceCollection services)
    {
        services.AddTransient<IFileDiscoveryFunctionConfiguration, AppConfiguration>();
        services.AddTransient<IFileExtractQueueClientConfiguration, AppConfiguration>();
        services.AddTransient<IFileTransformQueueClientConfiguration, AppConfiguration>();
        services.AddTransient<IMeshHandshakeFunctionConfiguration, AppConfiguration>();
        services.AddTransient<IFileRetryFunctionConfiguration, AppConfiguration>();
        services.AddTransient<IFileTransformFunctionConfiguration, AppConfiguration>();

        return services;
    }
}
