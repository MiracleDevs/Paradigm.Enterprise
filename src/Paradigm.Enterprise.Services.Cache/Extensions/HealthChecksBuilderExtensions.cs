using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Services.Cache.HealthCheck;

namespace Paradigm.Enterprise.Services.Cache.Extensions;

public static class HealthChecksBuilderExtensions
{
    public static IHealthChecksBuilder AddCacheHealthCheck(this IHealthChecksBuilder builder, string name = "Redis")
    {
        builder.AddCheck<RedisHealthCheck>(name);
        return builder;
    }
}