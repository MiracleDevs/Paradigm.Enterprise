using Aspire.Hosting.ApplicationModel;

namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Represents a SQL Server dependency and its optional managed Aspire resources.
/// </summary>
public sealed class ParadigmSqlServerDatabaseReference : ParadigmResourceReference
{
    #region Properties

    /// <summary>
    /// Gets the managed SQL Server resource, or <see langword="null"/> for external mode.
    /// </summary>
    public IResourceBuilder<SqlServerServerResource>? Server { get; }

    /// <summary>
    /// Gets the managed database resource, or <see langword="null"/> for external mode.
    /// </summary>
    public IResourceBuilder<SqlServerDatabaseResource>? Database { get; }

    /// <summary>
    /// Gets the managed password parameter, or <see langword="null"/> for external mode.
    /// </summary>
    public IResourceBuilder<ParameterResource>? Password { get; }

    #endregion

    #region Constructors

    internal ParadigmSqlServerDatabaseReference(
        IResourceBuilder<SqlServerServerResource>? server,
        IResourceBuilder<SqlServerDatabaseResource>? database,
        IResourceBuilder<ParameterResource>? password,
        Action<IResourceBuilder<ProjectResource>> configureConsumer)
        : base(configureConsumer)
    {
        Server = server;
        Database = database;
        Password = password;
    }

    #endregion
}
