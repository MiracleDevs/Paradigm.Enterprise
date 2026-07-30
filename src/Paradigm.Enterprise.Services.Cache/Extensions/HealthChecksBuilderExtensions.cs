using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Services.Cache.HealthCheck;

namespace Paradigm.Enterprise.Services.Cache.Extensions;

/// <summary>
/// Adds Redis cache health checks.
/// </summary>
public static class HealthChecksBuilderExtensions
{
    /// <summary>
    /// Adds a health check for the registered Redis connection.
    /// </summary>
    /// <param name="builder">The health-check builder.</param>
    /// <param name="name">The registration name.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IHealthChecksBuilder AddCacheHealthCheck(this IHealthChecksBuilder builder, string name = "Redis")
    {
        builder.AddCheck<RedisHealthCheck>(name);
        return builder;
    }
}
