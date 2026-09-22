using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Paradigm.Enterprise.Aspire.DatabaseBootstrap.SqlServer;

/// <summary>
/// Provides AppHost composition helpers for finite SQL Server bootstrap containers.
/// </summary>
public static class SqlServerDatabaseBootstrapHostingExtensions
{
    #region Public Methods

    /// <summary>
    /// Adds an application-owned SQL Server bootstrap Dockerfile resource.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="database">The managed SQL Server database resource.</param>
    /// <param name="sqlServer">The managed SQL Server resource.</param>
    /// <param name="password">The secret SQL Server password parameter.</param>
    /// <param name="options">The bootstrap configuration.</param>
    /// <returns>The finite bootstrap resource.</returns>
    public static IResourceBuilder<ContainerResource> AddSqlServerDatabaseBootstrap(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerDatabaseResource> database,
        IResourceBuilder<SqlServerServerResource> sqlServer,
        IResourceBuilder<ParameterResource> password,
        SqlServerDatabaseBootstrapOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(sqlServer);
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(options);
        Validate(options);

        var bootstrap = builder.AddDockerfile(options.ResourceName, options.DockerBuildContext, options.DockerfilePath)
            .WithLifetime(options.Lifetime)
            .WithReference(database)
            .WithEnvironment("DATABASE_NAME", options.DatabaseName)
            .WithEnvironment("PUBLISH_ON_START", options.PublishOnStart.ToString().ToLowerInvariant())
            .WithEnvironment("SA_PASSWORD", password)
            .WithEnvironment("SQL_SERVER", options.SqlServerHost)
            .WaitFor(database)
            .WithParentRelationship(sqlServer);

        if (!string.IsNullOrWhiteSpace(options.ContainerName))
            bootstrap.WithContainerName(options.ContainerName);

        return bootstrap;
    }

    #endregion

    #region Private Methods

    private static void Validate(SqlServerDatabaseBootstrapOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ResourceName))
            throw new InvalidOperationException("A bootstrap resource name is required.");
        if (string.IsNullOrWhiteSpace(options.DockerBuildContext))
            throw new InvalidOperationException("A Docker build context is required.");
        if (string.IsNullOrWhiteSpace(options.DockerfilePath))
            throw new InvalidOperationException("A Dockerfile path is required.");
        if (string.IsNullOrWhiteSpace(options.DatabaseName))
            throw new InvalidOperationException("A database name is required.");
        if (string.IsNullOrWhiteSpace(options.SqlServerHost))
            throw new InvalidOperationException("A SQL Server host is required.");
    }

    #endregion
}
