using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Common;

namespace ServiceLayer.Data;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddDbContext(this IServiceCollection services)
    {
        services.AddDbContext<ServiceLayerDbContext>(options =>
        {
            var connectionString = EnvironmentVariables.GetRequired("DatabaseConnectionString");

            options.UseSqlServer(connectionString);
        });

        return services;
    }
}
