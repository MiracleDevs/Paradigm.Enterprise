using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Paradigm.Enterprise.Services.BlobStorage.HealthCheck;

namespace Paradigm.Enterprise.Services.BlobStorage.Extensions;

/// <summary>
/// Adds Azure Blob Storage connectivity health checks.
/// </summary>
public static class HealthChecksBuilderExtensions
{
    /// <summary>
    /// Adds a health check whose complete options are bound from a configuration section.
    /// </summary>
    /// <param name="builder">The health-check builder.</param>
    /// <param name="configurationSection">The section containing connection, account URI, and optional container settings.</param>
    /// <param name="name">The registration name.</param>
    /// <param name="failureStatus">The status reported when the check is unhealthy.</param>
    /// <param name="tags">Optional tags attached to the registration.</param>
    /// <returns>The same builder for chaining.</returns>
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

    /// <summary>
    /// Adds a health check using a named connection string and optional container setting.
    /// </summary>
    /// <param name="builder">The health-check builder.</param>
    /// <param name="connectionStringSection">The name of the connection string.</param>
    /// <param name="containerNameSection">An optional configuration key containing the container name.</param>
    /// <param name="name">The registration name.</param>
    /// <param name="failureStatus">The status reported when the check is unhealthy.</param>
    /// <param name="tags">Optional tags attached to the registration.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IHealthChecksBuilder AddAzureBlobStorageCheck(this IHealthChecksBuilder builder, string connectionStringSection, string? containerNameSection = null, string name = "AzureBlobStorage", HealthStatus? failureStatus = null, IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(name, (serviceProvider) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var options = new AzureBlobStorageHealthCheckOptions();

            options.ConnectionString = config.GetConnectionString(connectionStringSection);

            if (containerNameSection is not null)
                options.ContainerName = config.GetValue<string>(containerNameSection);

            return new AzureBlobStorageHealthCheck(options);
        }, failureStatus, tags));
    }
}
