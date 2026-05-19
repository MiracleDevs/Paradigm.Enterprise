using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Services.Cache.Configuration;
using StackExchange.Redis;

namespace Paradigm.Enterprise.Services.Cache.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the cache service.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="connectionStringName">Name of the connection string.</param>
    /// <param name="instanceName">Name of the instance.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task AddCacheAsync(this IServiceCollection services, IConfiguration configuration, string connectionStringName, string? instanceName = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var connectionString = configuration.GetConnectionString(connectionStringName);

        var configurationOptions = !string.IsNullOrWhiteSpace(connectionString)
            ? ConfigurationOptions.Parse(connectionString)
            : await BuildManagedIdentityConfigurationOptions(configuration);

        // registers the connection multiplexer
        IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
        services.AddSingleton(connectionMultiplexer);

        // registers the distributed cache but using the same connection multiplexer instance
        services.AddStackExchangeRedisCache((options) =>
        {
            options.ConnectionMultiplexerFactory = () => Task.FromResult(connectionMultiplexer);

            if (!string.IsNullOrWhiteSpace(connectionString))
                options.Configuration = connectionString;

            if (!string.IsNullOrWhiteSpace(instanceName))
                options.InstanceName = instanceName;
        });

        services.AddSingleton<ICacheService, CacheService>();
    }

    /// <summary>
    /// Builds the managed identity configuration options.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Connection string not found and managed identity host is missing in 'RedisCacheConfiguration:ManagedIdentity:Host'.</exception>
    private static async Task<ConfigurationOptions> BuildManagedIdentityConfigurationOptions(IConfiguration configuration)
    {
        var cacheConfiguration = new RedisCacheConfiguration();
        configuration.Bind("RedisCacheConfiguration", cacheConfiguration);

        var managedIdentityConfiguration = cacheConfiguration.ManagedIdentity;

        if (managedIdentityConfiguration is null || string.IsNullOrWhiteSpace(managedIdentityConfiguration.Host))
            throw new ArgumentException("Connection string not found and managed identity host is missing in 'RedisCacheConfiguration:ManagedIdentity:Host'.");

        var configurationOptions = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            Ssl = managedIdentityConfiguration.UseSsl,
        };

        configurationOptions.EndPoints.Add(managedIdentityConfiguration.Host, managedIdentityConfiguration.Port);

        if (!string.IsNullOrWhiteSpace(managedIdentityConfiguration.User))
            configurationOptions.User = managedIdentityConfiguration.User;

        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(managedIdentityConfiguration.ClientId))
            credentialOptions.ManagedIdentityClientId = managedIdentityConfiguration.ClientId;

        var credential = new DefaultAzureCredential(credentialOptions);
        await configurationOptions.ConfigureForAzureWithTokenCredentialAsync(credential);

        return configurationOptions;
    }
}