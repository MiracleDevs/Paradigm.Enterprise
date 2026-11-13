using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Paradigm.Enterprise.Data.Context;
using System.Runtime.ExceptionServices;

namespace Paradigm.Enterprise.Data.MySql.Context;

/// <summary>Owns lazily created, named Oracle MySQL connections within one service scope.</summary>
/// <remarks>Contexts and routines must use these connections sequentially. The provider owns their disposal.</remarks>
public sealed class MySqlDbConnectionProvider : DbContextConnectionProvider
{
    #region Fields

    private readonly Dictionary<string, MySqlConnection> _connections = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    #endregion

    #region Properties

    /// <summary>Gets the scoped connection configured under the supplied connection-string name.</summary>
    /// <param name="name">The configuration key beneath <c>ConnectionStrings</c>.</param>
    /// <returns>The cached caller-borrowed connection; lookup does not open it.</returns>
    /// <exception cref="InvalidOperationException">The named connection string is missing or blank.</exception>
    /// <exception cref="ObjectDisposedException">The provider has been disposed.</exception>
    public MySqlConnection this[string name]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (_connections.TryGetValue(name, out var connection))
                return connection;

            var connectionString = _configuration.GetConnectionString(name);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"Connection string '{name}' is missing or empty.");

            connection = new MySqlConnection(connectionString);
            _connections.Add(name, connection);
            return connection;
        }
    }

    #endregion

    #region Constructors

    /// <summary>Creates a scoped provider using named connection strings from configuration.</summary>
    /// <param name="configuration">The application's configuration.</param>
    public MySqlDbConnectionProvider(IConfiguration configuration) : base(configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
    }

    #endregion

    #region Overrides

    /// <summary>Disposes every connection owned by this scope.</summary>
    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Exception? disposalException = null;
        try
        {
            foreach (var connection in _connections.Values)
                try
                {
                    connection.Dispose();
                }
                catch (Exception exception)
                {
                    disposalException ??= exception;
                }
        }
        finally
        {
            _connections.Clear();
            base.Dispose();
        }

        if (disposalException is not null)
            ExceptionDispatchInfo.Capture(disposalException).Throw();
    }

    #endregion
}