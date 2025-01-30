using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Paradigm.Enterprise.Services.BlobStorage.Extensions;
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the blob storage account.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="connectionStringName">Name of the connection string.</param>
    public static IServiceCollection RegisterBlobStorageAccount(this IServiceCollection services, string connectionStringName)
    {
        return services.AddScoped<IBlobStorageService>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(connectionStringName) ?? throw new ArgumentException("The blob storage connection string couldn't be found.");
            return new BlobStorageService(connectionString);
        });
    }
}