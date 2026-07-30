using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Services.BlobStorage.Configuration;

namespace Paradigm.Enterprise.Services.BlobStorage.Extensions;

/// <summary>
/// Registers an Azure Blob Storage account in dependency injection.
/// </summary>
/// <remarks>
/// Both methods register <see cref="IBlobStorageService"/> as scoped and bind optional retry
/// settings from <c>BlobStorageConfiguration</c>.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the blob storage account using managed identity.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="storageAccountUriSection">The storage account URI section.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <remarks>
    /// Registration itself does not read the configured URI. The scoped factory throws
    /// <see cref="ArgumentException"/> later if <see cref="IBlobStorageService"/> is resolved without
    /// that configuration value.
    /// </remarks>
    /// <example>
    /// Given <c>Storage:AccountUri</c> in configuration:
    /// <code>
    /// services.RegisterBlobStorageAccountUsingManagedIdentity("Storage:AccountUri");
    /// </code>
    /// </example>
    public static IServiceCollection RegisterBlobStorageAccountUsingManagedIdentity(this IServiceCollection services, string storageAccountUriSection)
    {
        return services.AddScoped<IBlobStorageService>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var storageConnection = configuration.GetValue<string>(storageAccountUriSection) ?? throw new ArgumentException("The blob storage account URI couldn't be found.");
            var blobStorageConfiguration = new BlobStorageConfiguration();
            configuration.Bind("BlobStorageConfiguration", blobStorageConfiguration);
            blobStorageConfiguration.StorageConnection = storageConnection;

            return BlobStorageService.CreateUsingManagedIdentity(blobStorageConfiguration);
        });
    }

    /// <summary>
    /// Registers the blob storage account using connection string.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="connectionStringName">Name of the connection string.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <remarks>
    /// Registration itself does not read the connection string. The scoped factory throws
    /// <see cref="ArgumentException"/> later if <see cref="IBlobStorageService"/> is resolved without
    /// the named value.
    /// </remarks>
    /// <example>
    /// Given a <c>BlobStorage</c> entry in <c>ConnectionStrings</c>:
    /// <code>
    /// services.RegisterBlobStorageAccountUsingConnectionString("BlobStorage");
    /// </code>
    /// </example>
    public static IServiceCollection RegisterBlobStorageAccountUsingConnectionString(this IServiceCollection services, string connectionStringName)
    {
        return services.AddScoped<IBlobStorageService>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var storageConnection = configuration.GetConnectionString(connectionStringName) ?? throw new ArgumentException("The blob storage connection string couldn't be found.");
            var blobStorageConfiguration = new BlobStorageConfiguration();
            configuration.Bind("BlobStorageConfiguration", blobStorageConfiguration);
            blobStorageConfiguration.StorageConnection = storageConnection;

            return BlobStorageService.CreateUsingConnectionString(blobStorageConfiguration);
        });
    }
}
