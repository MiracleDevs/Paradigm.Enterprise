using Npgsql;
using System.Runtime.CompilerServices;

namespace Paradigm.Enterprise.Data.PostgreSql.StoredProcedures.Mappers;

/// <summary>
/// Provides a base implementation for building PostgreSQL parameters from an application object.
/// </summary>
/// <remarks>A mapper instance is intended for one mapping operation and clears its accumulated parameters when disposed.</remarks>
public abstract class NpgsqlParameterMapperBase : INpgsqlParameterMapper
{
    #region Properties

    /// <summary>
    /// Gets the prefix prepended to each application parameter name.
    /// </summary>
    /// <value>
    /// The parameter prefix.
    /// </value>
    protected abstract string ParameterPrefix { get; }

    /// <summary>
    /// The SQL parameters
    /// </summary>
    private readonly List<NpgsqlParameter> _sqlParameters = new();

    #endregion

    #region Public Methods

    /// <summary>
    /// Clears the parameters accumulated by this mapper.
    /// </summary>
    public void Dispose() => _sqlParameters.Clear();

    /// <summary>
    /// Maps an application parameter object to a new parameter array.
    /// </summary>
    /// <param name="parameters">The application parameter object to map.</param>
    /// <returns>The parameters accumulated by <see cref="AddNpgsqlParameter(object)"/>.</returns>
    public NpgsqlParameter[] Map(object parameters)
    {
        AddNpgsqlParameter(parameters);
        return _sqlParameters.ToArray();
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Adds all PostgreSQL parameters represented by an application parameter object.
    /// </summary>
    /// <param name="parameters">The application parameter object to inspect.</param>
    protected abstract void AddNpgsqlParameter(object parameters);

    /// <summary>
    /// Adds a PostgreSQL parameter to the current mapping operation.
    /// </summary>
    /// <param name="name">The parameter name without <see cref="ParameterPrefix"/>.</param>
    /// <param name="value">The parameter value; <see langword="null"/> is converted to <see cref="DBNull.Value"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void AddNpgsqlParameter(string name, object? value) => _sqlParameters.Add(new NpgsqlParameter($"{ParameterPrefix}{name}", value ?? DBNull.Value));

    #endregion
}
