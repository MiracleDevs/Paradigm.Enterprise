using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Paradigm.Enterprise.Data.Context;

namespace Paradigm.Enterprise.Data.SqlServer.Context;

/// <summary>
/// Provides and reuses named <see cref="SqlConnection"/> instances for Entity Framework contexts.
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
///         ["ConnectionStrings:MainDatabase"] = "Server=(local);Database=Main;Integrated Security=true"
///     })
///     .Build();
///
/// using var connections = new SqlServerDbContextConnectionProvider(configuration);
/// SqlConnection connection = connections["MainDatabase"];
/// await connection.OpenAsync();
/// </code>
/// Repeated lookup by the same name returns the cached connection. Application code should not
/// dispose that connection independently of the provider.
/// </example>
public class SqlServerDbContextConnectionProvider : DbContextConnectionProvider
{
    #region Properties

    /// <summary>
    /// The connections
    /// </summary>
    private readonly Dictionary<string, SqlConnection> _connections;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerDbContextConnectionProvider"/> class.
    /// </summary>
    /// <param name="configuration">The configuration containing the named SQL Server connection strings.</param>
    public SqlServerDbContextConnectionProvider(IConfiguration configuration) : base(configuration)
    {
        _connections = [];
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Disposes all cached SQL Server connections.
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();
        foreach (var connection in _connections.Values)
            connection.Dispose();
    }

    /// <summary>
    /// Gets the <see cref="SqlConnection"/> with the specified name.
    /// </summary>
    /// <value>
    /// The <see cref="SqlConnection"/>.
    /// </value>
    /// <param name="name">The configuration connection-string name.</param>
    public SqlConnection this[string name]
    {
        get
        {
            if (!_connections.TryGetValue(name, out SqlConnection? conn))
            {
                var connectionString = _configuration.GetConnectionString(name);
                return _connections[name] = new SqlConnection(connectionString);
            }
            else
                return conn;
        }
    }

    #endregion
}
