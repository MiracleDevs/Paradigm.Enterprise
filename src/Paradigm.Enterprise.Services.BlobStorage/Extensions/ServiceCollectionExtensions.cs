using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Paradigm.Enterprise.Services.BlobStorage.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the blob storage account using managed identity.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="storageAccountUriSection">The storage account URI section.</param>
    /// <returns></returns>
    public static IServiceCollection RegisterBlobStorageAccountUsingManagedIdentity(this IServiceCollection services, string storageAccountUriSection)
    {
        return services.AddScoped<IBlobStorageService>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var storageAccountUri = configuration.GetValue<string>(storageAccountUriSection) ?? throw new ArgumentException("The blob storage account URI couldn't be found.");
            return BlobStorageService.CreateUsingManagedIdentity(storageAccountUri);
        });
    }

    /// <summary>
    /// Registers the blob storage account using connection string.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="connectionStringName">Name of the connection string.</param>
    /// <returns></returns>
    public static IServiceCollection RegisterBlobStorageAccountUsingConnectionString(this IServiceCollection services, string connectionStringName)
    {
        return services.AddScoped<IBlobStorageService>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(connectionStringName) ?? throw new ArgumentException("The blob storage connection string couldn't be found.");
            return BlobStorageService.CreateUsingConnectionString(connectionString);
        });
    }
}