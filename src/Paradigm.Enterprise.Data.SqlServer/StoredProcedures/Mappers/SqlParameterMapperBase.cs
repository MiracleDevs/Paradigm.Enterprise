using Microsoft.Data.SqlClient;
using System.Runtime.CompilerServices;

namespace Paradigm.Enterprise.Data.SqlServer.StoredProcedures.Mappers;

/// <summary>
/// Provides a base implementation for building SQL Server parameters from an application object.
/// </summary>
/// <remarks>A mapper instance is intended for one mapping operation and clears its accumulated parameters when disposed.</remarks>
/// <example>
/// Define a mapper for a strongly typed application parameter object:
/// <code>
/// sealed record ArchiveOrderParameters(int OrderId, string? Reason);
///
/// sealed class ArchiveOrderParameterMapper : SqlParameterMapperBase
/// {
///     protected override void AddSqlParameters(object parameters)
///     {
///         var value = (ArchiveOrderParameters)parameters;
///         AddSqlParameter("@OrderId", value.OrderId);
///         AddSqlParameter("@Reason", value.Reason);
///     }
/// }
/// </code>
/// <see cref="AddSqlParameter(string, object)"/> translates a null <c>Reason</c> to
/// <see cref="DBNull.Value"/> before the command executes.
/// </example>
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
    /// Clears the parameters accumulated by this mapper.
    /// </summary>
    public void Dispose() => _sqlParameters.Clear();

    /// <summary>
    /// Maps an application parameter object to a new parameter array.
    /// </summary>
    /// <param name="parameters">The application parameter object to map.</param>
    /// <returns>The parameters accumulated by <see cref="AddSqlParameters(object)"/>.</returns>
    public SqlParameter[] Map(object parameters)
    {
        AddSqlParameters(parameters);
        return _sqlParameters.ToArray();
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Adds all SQL Server parameters represented by an application parameter object.
    /// </summary>
    /// <param name="parameters">The application parameter object to inspect.</param>
    protected abstract void AddSqlParameters(object parameters);

    /// <summary>
    /// Adds a SQL Server parameter to the current mapping operation.
    /// </summary>
    /// <param name="name">The provider parameter name.</param>
    /// <param name="value">The parameter value; <see langword="null"/> is converted to <see cref="DBNull.Value"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void AddSqlParameter(string name, object? value) => _sqlParameters.Add(new SqlParameter(name, value ?? DBNull.Value));

    #endregion
}
