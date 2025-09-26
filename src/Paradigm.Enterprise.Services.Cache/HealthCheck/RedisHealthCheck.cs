using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Paradigm.Enterprise.Services.Cache.HealthCheck;
internal class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var result = await db.PingAsync();
            return result.TotalMilliseconds < 300 ? HealthCheckResult.Healthy("Redis is healthy") : HealthCheckResult.Degraded("Redis is slow");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unhealthy", ex);
        }
    }
}