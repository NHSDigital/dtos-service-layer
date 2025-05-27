using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;


namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureNbssAppointmentEvents(this IServiceCollection services)
    {

        services.AddTransient<IFileTransformer, FileTransformer>();

        services.AddTransient<IFileParser, FileParser>();
        services.AddTransient<IValidationRunner, ValidationRunner>();
        services.AddTransient<IStagingPersister, StagingPersister>();

        services.RegisterValidators();

        return services;
    }

    private static IServiceCollection RegisterValidators(this IServiceCollection services)
    {
        services.Scan(s => s.FromAssemblyOf<IFileValidator>()
            .AddClasses(c => c.AssignableToAny(typeof(IFileValidator), typeof(IRecordValidator)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        return services;
    }
}
