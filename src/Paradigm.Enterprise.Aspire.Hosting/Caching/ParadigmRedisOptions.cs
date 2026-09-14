namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Configures Redis for an Aspire application.
/// </summary>
public sealed class ParadigmRedisOptions
{
    #region Properties

    /// <summary>
    /// Gets or sets whether Redis is managed by Aspire or supplied externally.
    /// </summary>
    public ParadigmResourceMode Mode { get; init; } = ParadigmResourceMode.Managed;

    /// <summary>
    /// Gets or sets the Redis resource name.
    /// </summary>
    public string ResourceName { get; init; } = "redis";

    /// <summary>
    /// Gets or sets the connection-string name injected into consumers.
    /// </summary>
    public string ConnectionName { get; init; } = "RedisCacheConnection";

    /// <summary>
    /// Gets or sets the optional fixed Redis host port.
    /// </summary>
    public int? HostPort { get; init; }

    #endregion
}
