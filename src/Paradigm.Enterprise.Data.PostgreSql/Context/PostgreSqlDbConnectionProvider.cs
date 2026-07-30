using Microsoft.Extensions.Configuration;
using Npgsql;
using Paradigm.Enterprise.Data.Context;

namespace Paradigm.Enterprise.Data.PostgreSql.Context;

/// <summary>
/// Provides and reuses named <see cref="NpgsqlConnection"/> instances for Entity Framework contexts.
/// </summary>
/// <remarks>
/// Connections are created lazily from the corresponding named connection string and are owned by
/// this provider. Disposing the provider disposes every connection it created.
/// </remarks>
/// <example>
/// Resolve a named connection once and let the provider own its lifetime:
/// <code>
/// IConfiguration configuration = new ConfigurationBuilder()
///     .AddInMemoryCollection(new Dictionary&lt;string, string?&gt;
///     {
///         ["ConnectionStrings:MainDatabase"] = "Host=localhost;Database=main;Username=app"
///     })
///     .Build();
///
/// using var connections = new PostgreSqlDbConnectionProvider(configuration);
/// NpgsqlConnection connection = connections["MainDatabase"];
/// await connection.OpenAsync();
/// </code>
/// Repeated lookup by the same name returns the cached connection. Application code should not
/// dispose that connection independently of the provider.
/// </example>
public class PostgreSqlDbConnectionProvider : DbContextConnectionProvider
{
    #region Properties

    /// <summary>
    /// The connections
    /// </summary>
    private readonly Dictionary<string, NpgsqlConnection> _connections;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDbConnectionProvider"/> class.
    /// </summary>
    /// <param name="configuration">The configuration containing the named PostgreSQL connection strings.</param>
    public PostgreSqlDbConnectionProvider(IConfiguration configuration) : base(configuration)
    {
        _connections = [];
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Disposes all cached PostgreSQL connections.
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();
        foreach (var connection in _connections.Values)
            connection.Dispose();
    }

    /// <summary>
    /// Gets the <see cref="NpgsqlConnection"/> with the specified name.
    /// </summary>
    /// <value>
    /// The <see cref="NpgsqlConnection"/>.
    /// </value>
    /// <param name="name">The configuration connection-string name.</param>
    public NpgsqlConnection this[string name]
    {
        get
        {
            if (!_connections.TryGetValue(name, out NpgsqlConnection? conn))
            {
                var connectionString = _configuration.GetConnectionString(name);
                return _connections[name] = new NpgsqlConnection(connectionString);
            }
            else
                return conn;
        }
    }

    #endregion
}
