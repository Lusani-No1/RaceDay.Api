using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RaceDay.Api.Services;

/// <summary>
/// Chooses the image storage provider once at startup: Azure Blob Storage when
/// Storage:Provider is "Azure", otherwise the local wwwroot/uploads provider.
/// </summary>
public static class FileStorageRegistration
{
    public static IServiceCollection AddRaceDayFileStorage(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));

        var provider = configuration["Storage:Provider"] ?? "Local";
        if (provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
        else
            services.AddSingleton<IFileStorage, LocalFileStorage>();

        return services;
    }
}
