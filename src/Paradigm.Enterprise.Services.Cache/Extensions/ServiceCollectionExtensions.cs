using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Paradigm.Enterprise.Services.Cache.Extensions;

public static class ServiceCollectionExtensions
{
    public static ServiceCollection AddCache(this ServiceCollection services, IConfiguration configuration, string connectionStringName, string? instanceName = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException($"Connection string '{connectionStringName}' not found.");

        var configurationOptions = ConfigurationOptions.Parse(connectionString);

        // registers the connection multiplexer
        IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
        services.AddSingleton(connectionMultiplexer);

        // registers the distributed cache but using the same connection multiplexer instance
        services.AddStackExchangeRedisCache((options) =>
        {
            options.Configuration = connectionString;
            options.ConnectionMultiplexerFactory = () => Task.FromResult(connectionMultiplexer);

            if (!string.IsNullOrWhiteSpace(instanceName))
            {
                options.InstanceName = instanceName;
            }
        });

        services.AddScoped<ICacheService, CacheService>();
        return services;
    }
}
