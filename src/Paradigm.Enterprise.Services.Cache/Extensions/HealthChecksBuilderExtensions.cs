using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Services.Cache.HealthCheck;
using StackExchange.Redis;

namespace Paradigm.Enterprise.Services.Cache.Extensions;
public static class HealthChecksBuilderExtensions
{
    public static IHealthChecksBuilder AddRedisCheck(this IHealthChecksBuilder builder, string redisConnectionString, string name = "Redis")
    {
        var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);

        builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        builder.AddCheck<RedisHealthCheck>(name);
        return builder;
    }
}