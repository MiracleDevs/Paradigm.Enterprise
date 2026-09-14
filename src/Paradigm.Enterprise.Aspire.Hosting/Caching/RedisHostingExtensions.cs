using Aspire.Hosting;

namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Provides reusable Redis hosting recipes.
/// </summary>
public static class RedisHostingExtensions
{
    #region Public Methods

    /// <summary>
    /// Adds a managed Redis resource or an external connection-string resource.
    /// </summary>
    /// <param name="builder">The Aspire application builder.</param>
    /// <param name="options">Redis configuration.</param>
    /// <returns>A consumer reference for the Redis connection.</returns>
    public static ParadigmResourceReference AddParadigmRedis(
        this IDistributedApplicationBuilder builder,
        ParadigmRedisOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        Validate(options.ConnectionName, nameof(options.ConnectionName));

        if (options.Mode == ParadigmResourceMode.External)
        {
            var external = builder.AddConnectionString(options.ConnectionName);
            return new ParadigmResourceReference(project => project.WithReference(external));
        }

        Validate(options.ResourceName, nameof(options.ResourceName));
        var redis = builder.AddRedis(options.ResourceName, options.HostPort).WithPassword(null);
        var connection = builder.AddConnectionString(options.ConnectionName, redis.Resource.ConnectionStringExpression);
        return new ParadigmResourceReference(project => project.WithReference(connection).WaitFor(redis));
    }

    #endregion

    #region Private Methods

    private static void Validate(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{name} is required.");
    }

    #endregion
}
