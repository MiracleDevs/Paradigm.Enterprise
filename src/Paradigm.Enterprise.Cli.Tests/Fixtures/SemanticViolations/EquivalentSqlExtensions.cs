using System.Data;
using System.Data.Common;

namespace SemanticViolations;

public static class EquivalentSqlExtensions
{
    #region Public Methods

    public static Task<IReadOnlyList<T>> QueryRowsAsync<T>(
        this DbConnection connection,
        string sql) => Task.FromResult<IReadOnlyList<T>>([]);

    public static Task<IReadOnlyList<T>> QueryContractAsync<T>(
        this IDbConnection connection,
        string commandText) => Task.FromResult<IReadOnlyList<T>>([]);

    public static Task<IReadOnlyList<T>> QueryContractInterpolatedAsync<T>(
        this IDbConnection connection,
        FormattableString commandText) => Task.FromResult<IReadOnlyList<T>>([]);

    public static Task<int> ExecuteStatementAsync(
        this DbConnection connection,
        string statement) => Task.FromResult(0);

    public static Task<int> ExecuteWithMisleadingSqlAsync(
        this DbConnection connection,
        int sql) => Task.FromResult(sql);

    #endregion
}
