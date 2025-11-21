using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Paradigm.Enterprise.Data.Context;

namespace Paradigm.Enterprise.Data.SqlServer.Context;

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
    /// <param name="configuration">The configuration.</param>
    public SqlServerDbContextConnectionProvider(IConfiguration configuration) : base(configuration)
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
    /// Gets the <see cref="SqlConnection"/> with the specified name.
    /// </summary>
    /// <value>
    /// The <see cref="SqlConnection"/>.
    /// </value>
    /// <param name="name">The name.</param>
    /// <returns></returns>
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