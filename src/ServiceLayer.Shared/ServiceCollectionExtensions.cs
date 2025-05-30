using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Data;

namespace ServiceLayer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        return services.AddDbContext();
    }
}
