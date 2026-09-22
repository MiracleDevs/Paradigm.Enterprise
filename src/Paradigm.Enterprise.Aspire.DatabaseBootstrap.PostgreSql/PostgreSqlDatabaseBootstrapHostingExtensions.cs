using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Paradigm.Enterprise.Aspire.DatabaseBootstrap.PostgreSql;

/// <summary>
/// Provides AppHost composition helpers for finite PostgreSQL bootstrap containers.
/// </summary>
public static class PostgreSqlDatabaseBootstrapHostingExtensions
{
    #region Public Methods

    /// <summary>
    /// Adds an application-owned PostgreSQL bootstrap Dockerfile resource.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="database">The managed PostgreSQL database resource.</param>
    /// <param name="postgres">The managed PostgreSQL server resource.</param>
    /// <param name="options">The bootstrap configuration.</param>
    /// <returns>The finite bootstrap resource.</returns>
    public static IResourceBuilder<ContainerResource> AddPostgreSqlDatabaseBootstrap(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> database,
        IResourceBuilder<PostgresServerResource> postgres,
        PostgreSqlDatabaseBootstrapOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(postgres);
        ArgumentNullException.ThrowIfNull(options);
        Validate(options);

        var bootstrap = builder.AddDockerfile(options.ResourceName, options.DockerBuildContext, options.DockerfilePath)
            .WithLifetime(options.Lifetime)
            .WithReference(database)
            .WithEnvironment(options.ConnectionStringEnvironmentVariable, database)
            .WaitFor(database)
            .WithParentRelationship(postgres);

        if (!string.IsNullOrWhiteSpace(options.ContainerName))
            bootstrap.WithContainerName(options.ContainerName);

        return bootstrap;
    }

    #endregion

    #region Private Methods

    private static void Validate(PostgreSqlDatabaseBootstrapOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ResourceName))
            throw new InvalidOperationException("A bootstrap resource name is required.");
        if (string.IsNullOrWhiteSpace(options.DockerBuildContext))
            throw new InvalidOperationException("A Docker build context is required.");
        if (string.IsNullOrWhiteSpace(options.DockerfilePath))
            throw new InvalidOperationException("A Dockerfile path is required.");
        if (string.IsNullOrWhiteSpace(options.ConnectionStringEnvironmentVariable))
            throw new InvalidOperationException("A PostgreSQL connection-string environment variable is required.");
    }

    #endregion
}
