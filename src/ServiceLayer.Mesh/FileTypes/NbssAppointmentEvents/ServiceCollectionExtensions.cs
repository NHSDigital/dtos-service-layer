using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddNbssAppointmentEventServices(this IServiceCollection services)
    {
        services.AddTransient<IFileTransformer, FileTransformer>();
        services.AddTransient<IFileParser, FileParser>();
        services.AddTransient<IStagingPersister, StagingPersister>();
        services.AddSingleton<IValidationRunner, ValidationRunner>();
        services.AddValidators();

        return services;
    }

    private static IServiceCollection AddValidators(this IServiceCollection services)
    {
        foreach (var recordValidator in ValidatorRegistry.GetAllRecordValidators())
        {
            services.AddSingleton<IRecordValidator>(_ => recordValidator);
        }

        foreach (var fileValidator in ValidatorRegistry.GetAllFileValidators())
        {
            services.AddSingleton<IFileValidator>(_ => fileValidator);
        }

        return services;
    }
}
