using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Paradigm.Enterprise.Services.Cache.Extensions;
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the blob storage account.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="connectionStringName">Name of the connection string.</param>
    /// <param name="instanceName">Name of the instance.</param>
    /// <param name="getSerializerOptions">The get serializer options.</param>
    /// <returns></returns>
    public static IServiceCollection RegisterHybridCache(this IServiceCollection services, string connectionStringName, string instanceName, Func<JsonSerializerOptions>? getSerializerOptions = null)
    {
        services.AddFusionCache()
            .WithSerializer(new FusionCacheSystemTextJsonSerializer(getSerializerOptions?.Invoke()))
            .WithDistributedCache(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var redisCacheOptions = new RedisCacheOptions
                {
                    Configuration = configuration.GetConnectionString(connectionStringName),
                    InstanceName = instanceName
                };
                return new RedisCache(redisCacheOptions);
            })
            .AsHybridCache();

        return services;
    }
}