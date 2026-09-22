using Aspire.Hosting.ApplicationModel;

namespace Paradigm.Enterprise.Aspire.DatabaseBootstrap.PostgreSql;

/// <summary>
/// Defines the AppHost registration contract for an application-owned PostgreSQL bootstrap image.
/// </summary>
public sealed class PostgreSqlDatabaseBootstrapOptions
{
    #region Properties

    /// <summary>
    /// Gets the unique bootstrap resource name.
    /// </summary>
    public string ResourceName { get; init; } = "database-bootstrap";

    /// <summary>
    /// Gets the Docker build context relative to the AppHost project.
    /// </summary>
    public required string DockerBuildContext { get; init; }

    /// <summary>
    /// Gets the Dockerfile path relative to the build context.
    /// </summary>
    public required string DockerfilePath { get; init; }

    /// <summary>
    /// Gets the optional application-specific container name.
    /// </summary>
    public string? ContainerName { get; init; }

    /// <summary>
    /// Gets the resource lifetime for the finite bootstrap container.
    /// </summary>
    public ContainerLifetime Lifetime { get; init; } = ContainerLifetime.Session;

    /// <summary>
    /// Gets the environment variable that receives the PostgreSQL connection string.
    /// </summary>
    public string ConnectionStringEnvironmentVariable { get; init; } = "Paradigm_ORM_ConnectionString";

    #endregion
}
