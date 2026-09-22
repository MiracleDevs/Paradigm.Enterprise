using Aspire.Hosting.ApplicationModel;

namespace Paradigm.Enterprise.Aspire.DatabaseBootstrap.SqlServer;

/// <summary>
/// Defines the AppHost registration contract for an application-owned SQL Server bootstrap image.
/// </summary>
public sealed class SqlServerDatabaseBootstrapOptions
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
    /// Gets the database name passed to the bootstrap image.
    /// </summary>
    public required string DatabaseName { get; init; }

    /// <summary>
    /// Gets the SQL Server host name visible to the bootstrap container.
    /// </summary>
    public required string SqlServerHost { get; init; }

    /// <summary>
    /// Gets a value indicating whether schema publication is enabled for this startup.
    /// </summary>
    public bool PublishOnStart { get; init; }

    /// <summary>
    /// Gets the resource lifetime for the finite bootstrap container.
    /// </summary>
    public ContainerLifetime Lifetime { get; init; } = ContainerLifetime.Session;

    #endregion
}
