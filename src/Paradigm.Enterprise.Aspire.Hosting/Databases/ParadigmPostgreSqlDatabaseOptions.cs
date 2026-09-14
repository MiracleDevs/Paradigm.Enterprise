namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Configures a PostgreSQL database dependency.
/// </summary>
public sealed class ParadigmPostgreSqlDatabaseOptions
{
    #region Properties

    /// <summary>
    /// Gets or sets whether the database is managed locally or supplied externally.
    /// </summary>
    public ParadigmResourceMode Mode { get; init; } = ParadigmResourceMode.Managed;

    /// <summary>
    /// Gets or sets the Aspire PostgreSQL resource name.
    /// </summary>
    public string ServerName { get; init; } = "postgres";

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
    /// Gets or sets the managed PostgreSQL user name.
    /// </summary>
    public string UserName { get; init; } = "postgres";

    /// <summary>
    /// Gets or sets the managed PostgreSQL password.
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

    #endregion
}
