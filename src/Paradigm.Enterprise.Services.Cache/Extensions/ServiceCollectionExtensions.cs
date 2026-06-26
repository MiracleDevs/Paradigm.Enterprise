using Azure.Identity;
using Microsoft.Extensions.Caching.Distributed;
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
        ArgumentNullException.ThrowIfNull(configuration);

        var cacheConfiguration = new RedisCacheConfiguration();
        configuration.Bind("RedisCacheConfiguration", cacheConfiguration);

        var connectionString = configuration.GetConnectionString(connectionStringName);
        IConnectionMultiplexer? connectionMultiplexer = null;

        try
        {
            var configurationOptions = !string.IsNullOrWhiteSpace(connectionString)
                ? ConfigurationOptions.Parse(connectionString)
                : await BuildManagedIdentityConfigurationOptionsAsync(configuration);

            // Registers the connection multiplexer
            connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(configurationOptions);
            services.AddSingleton(connectionMultiplexer);

            // Only register Redis cache if connection succeeded
            services.AddStackExchangeRedisCache((options) =>
            {
                options.ConnectionMultiplexerFactory = () => Task.FromResult(connectionMultiplexer);

                if (!string.IsNullOrWhiteSpace(connectionString))
                    options.Configuration = connectionString;

                if (!string.IsNullOrWhiteSpace(instanceName))
                    options.InstanceName = instanceName;
            });
        }
        catch (Exception ex) when (!cacheConfiguration.ThrowExceptions && ex is RedisConnectionException or RedisTimeoutException or AuthenticationFailedException)
        {
            // Intentionally ignore startup connection failures to keep API bootstrapping.
            // Register a null-object cache implementation to satisfy DI requirements.
            services.AddSingleton<IDistributedCache, NullDistributedCache>();
        }

        services.AddSingleton<ICacheService, CacheService>();
    }

    /// <summary>
    /// Builds the managed identity configuration options.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Connection string not found and managed identity host is missing in 'RedisCacheConfiguration:ManagedIdentity:Host'.</exception>
    private static async Task<ConfigurationOptions> BuildManagedIdentityConfigurationOptionsAsync(IConfiguration configuration)
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