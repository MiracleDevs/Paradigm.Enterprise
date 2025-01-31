using Npgsql;
using System.Runtime.CompilerServices;

namespace Paradigm.Enterprise.Data.PostgreSql.StoredProcedures.Mappers;

public abstract class NpgsqlParameterMapperBase : INpgsqlParameterMapper
{
    #region Properties

    /// <summary>
    /// Gets the parameter prefix.
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
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() => _sqlParameters.Clear();

    /// <summary>
    /// Maps the specified parameters.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public NpgsqlParameter[] Map(object parameters)
    {
        AddNpgsqlParameter(parameters);
        return _sqlParameters.ToArray();
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Adds the SQL parameters.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    protected abstract void AddNpgsqlParameter(object parameters);

    /// <summary>
    /// Adds the SQL parameter.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void AddNpgsqlParameter(string name, object? value) => _sqlParameters.Add(new NpgsqlParameter($"{ParameterPrefix}{name}", value ?? DBNull.Value));

    #endregion
}