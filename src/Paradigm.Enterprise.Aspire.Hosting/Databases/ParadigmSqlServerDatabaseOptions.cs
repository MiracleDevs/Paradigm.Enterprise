using Aspire.Hosting.ApplicationModel;

namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Configures a SQL Server database dependency.
/// </summary>
public sealed class ParadigmSqlServerDatabaseOptions
{
    #region Properties

    /// <summary>
    /// Gets or sets whether the database is managed locally or supplied externally.
    /// </summary>
    public ParadigmResourceMode Mode { get; init; } = ParadigmResourceMode.Managed;

    /// <summary>
    /// Gets or sets the Aspire SQL Server resource name.
    /// </summary>
    public string ServerName { get; init; } = "sqlserver";

    /// <summary>
    /// Gets or sets the Aspire database resource name.
    /// </summary>
    public string DatabaseResourceName { get; init; } = "database";

    /// <summary>
    /// Gets or sets the connection-string name injected into consumers.
    /// </summary>
    public string ConnectionName { get; init; } = "DatabaseConnection";

    /// <summary>
    /// Gets or sets the physical database name.
    /// </summary>
    public required string DatabaseName { get; init; }

    /// <summary>
    /// Gets or sets the managed SQL Server administrator password.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Gets or sets the optional persistent Docker volume name.
    /// </summary>
    public string? DataVolumeName { get; init; }

    /// <summary>
    /// Gets or sets the optional fixed host port.
    /// </summary>
    public int? HostPort { get; init; }

    /// <summary>
    /// Gets or sets the lifetime for the managed SQL Server container.
    /// </summary>
    public ContainerLifetime Lifetime { get; init; } = ContainerLifetime.Persistent;

    #endregion
}
