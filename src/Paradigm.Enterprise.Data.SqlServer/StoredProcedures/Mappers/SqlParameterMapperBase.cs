using Microsoft.Data.SqlClient;
using System.Runtime.CompilerServices;

namespace Paradigm.Enterprise.Data.SqlServer.StoredProcedures.Mappers;

public abstract class SqlParameterMapperBase : ISqlParameterMapper
{
    #region Properties

    /// <summary>
    /// The SQL parameters
    /// </summary>
    private readonly List<SqlParameter> _sqlParameters = new();

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
    public SqlParameter[] Map(object parameters)
    {
        AddSqlParameters(parameters);
        return _sqlParameters.ToArray();
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Adds the SQL parameters.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    protected abstract void AddSqlParameters(object parameters);

    /// <summary>
    /// Adds the SQL parameter.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void AddSqlParameter(string name, object? value) => _sqlParameters.Add(new SqlParameter(name, value ?? DBNull.Value));

    #endregion
}