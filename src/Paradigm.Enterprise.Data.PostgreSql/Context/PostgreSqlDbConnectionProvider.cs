using Microsoft.Extensions.Configuration;
using Npgsql;
using Paradigm.Enterprise.Data.Context;

namespace Paradigm.Enterprise.Data.PostgreSql.Context;
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
    /// Initializes a new instance of the <see cref="SqlServerDbContextConnectionProvider"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public PostgreSqlDbConnectionProvider(IConfiguration configuration) : base(configuration)
    {
        _connections = [];
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
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
    /// <param name="name">The name.</param>
    /// <returns></returns>
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