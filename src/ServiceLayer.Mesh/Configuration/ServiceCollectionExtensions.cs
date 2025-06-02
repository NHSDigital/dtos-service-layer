using Microsoft.Extensions.DependencyInjection;

namespace ServiceLayer.Mesh.Configuration;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddApplicationConfiguration(this IServiceCollection services)
    {
        var implementationType = typeof(AppConfiguration);

        var interfaces = implementationType
            .GetInterfaces()
            .Where(i => i.Namespace == implementationType.Namespace);

        foreach (var serviceType in interfaces)
        {
            services.AddTransient(serviceType, implementationType);
        }

        return services;
    }
}
