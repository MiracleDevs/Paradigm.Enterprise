using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Paradigm.Enterprise.Services.BlobStorage.HealthCheck;

namespace Paradigm.Enterprise.Services.BlobStorage.Extensions;
public static class HealthChecksBuilderExtensions
{
    public static IHealthChecksBuilder AddAzureBlobStorageCheck(this IHealthChecksBuilder builder, string configurationSection = nameof(AzureBlobStorageHealthCheck), string name = "AzureBlobStorage", HealthStatus? failureStatus = null, IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(name, (serviceProvider) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var options = new AzureBlobStorageHealthCheckOptions();
            config.Bind(configurationSection, options);

            return new AzureBlobStorageHealthCheck(options);
        }, failureStatus, tags));
    }

    public static IHealthChecksBuilder AddAzureBlobStorageCheck(this IHealthChecksBuilder builder, string connectionStringSection, string? containerNameSection = null, string name = "AzureBlobStorage", HealthStatus? failureStatus = null, IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(name, (serviceProvider) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var options = new AzureBlobStorageHealthCheckOptions();

            options.ConnectionString = config.GetConnectionString(connectionStringSection);

            if (containerNameSection != null)
                options.ContainerName = config.GetValue<string>(containerNameSection);

            return new AzureBlobStorageHealthCheck(options);
        }, failureStatus, tags));
    }
}