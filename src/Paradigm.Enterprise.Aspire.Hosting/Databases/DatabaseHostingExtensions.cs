using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Provides reusable SQL Server and PostgreSQL hosting recipes.
/// </summary>
public static class DatabaseHostingExtensions
{
    #region Public Methods

    /// <summary>
    /// Adds a SQL Server database or an external database connection.
    /// </summary>
    /// <param name="builder">The Aspire application builder.</param>
    /// <param name="options">Database configuration.</param>
    /// <returns>A consumer reference for the database connection.</returns>
    public static ParadigmSqlServerDatabaseReference AddParadigmSqlServerDatabase(
        this IDistributedApplicationBuilder builder,
        ParadigmSqlServerDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        Validate(options.DatabaseName, nameof(options.DatabaseName));
        Validate(options.ConnectionName, nameof(options.ConnectionName));

        if (options.Mode == ParadigmResourceMode.External)
        {
            var external = builder.AddConnectionString(options.ConnectionName);
            return new ParadigmSqlServerDatabaseReference(
                null,
                null,
                null,
                project => project.WithReference(external));
        }

        Validate(options.ServerName, nameof(options.ServerName));
        Validate(options.DatabaseResourceName, nameof(options.DatabaseResourceName));
        Validate(options.Password, nameof(options.Password));

        var password = builder.AddParameter($"{options.ServerName}-password", options.Password!, secret: true);
        var server = builder.AddSqlServer(options.ServerName, password)
            .WithLifetime(options.Lifetime)
            .WithDataVolume(options.DataVolumeName);
        if (options.HostPort is not null)
            server.WithEndpoint("tcp", endpoint => endpoint.Port = options.HostPort);

        var database = server.AddDatabase(options.DatabaseResourceName, options.DatabaseName);
        if (options.DatabaseResourceName.Equals(options.ConnectionName, StringComparison.OrdinalIgnoreCase))
        {
            return new ParadigmSqlServerDatabaseReference(
                server,
                database,
                password,
                project => project.WithReference(database).WaitFor(database));
        }

        var connection = builder.AddConnectionString(options.ConnectionName, database.Resource.ConnectionStringExpression);
        return new ParadigmSqlServerDatabaseReference(
            server,
            database,
            password,
            project => project.WithReference(connection).WaitFor(database));
    }

    /// <summary>
    /// Adds a PostgreSQL database or an external database connection.
    /// </summary>
    /// <param name="builder">The Aspire application builder.</param>
    /// <param name="options">Database configuration.</param>
    /// <returns>A consumer reference for the database connection.</returns>
    public static ParadigmResourceReference AddParadigmPostgreSqlDatabase(
        this IDistributedApplicationBuilder builder,
        ParadigmPostgreSqlDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        Validate(options.DatabaseName, nameof(options.DatabaseName));
        Validate(options.ConnectionName, nameof(options.ConnectionName));

        if (options.Mode == ParadigmResourceMode.External)
        {
            var external = builder.AddConnectionString(options.ConnectionName);
            return new ParadigmResourceReference(project => project.WithReference(external));
        }

        Validate(options.ServerName, nameof(options.ServerName));
        Validate(options.DatabaseResourceName, nameof(options.DatabaseResourceName));
        Validate(options.UserName, nameof(options.UserName));
        Validate(options.Password, nameof(options.Password));

        var userName = builder.AddParameter($"{options.ServerName}-username", options.UserName);
        var password = builder.AddParameter($"{options.ServerName}-password", options.Password!, secret: true);
        var server = builder.AddPostgres(options.ServerName, userName, password, options.HostPort)
            .WithLifetime(ContainerLifetime.Persistent)
            .WithDataVolume(options.DataVolumeName);
        var database = server.AddDatabase(options.DatabaseResourceName, options.DatabaseName);
        if (options.DatabaseResourceName.Equals(options.ConnectionName, StringComparison.OrdinalIgnoreCase))
        {
            return new ParadigmResourceReference(project => project.WithReference(database).WaitFor(database));
        }

        var connection = builder.AddConnectionString(options.ConnectionName, database.Resource.ConnectionStringExpression);
        return new ParadigmResourceReference(project => project.WithReference(connection).WaitFor(database));
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
