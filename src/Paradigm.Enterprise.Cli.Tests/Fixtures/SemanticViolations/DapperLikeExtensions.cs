using System.Data.Common;

namespace Dapper;

public static class DapperLikeExtensions
{
    #region Public Methods

    public static Task<IReadOnlyList<T>> QueryAsync<T>(this DbConnection connection, string sql) =>
        Task.FromResult<IReadOnlyList<T>>([]);

    public static Task<int> ExecuteAsync(this DbConnection connection, string commandText) =>
        Task.FromResult(0);

    public static Task<int> ExecuteReaderAsync(this DbConnection connection, string sql) =>
        Task.FromResult(0);

    public static Task<int> ExecuteScalarAsync(this DbConnection connection, string sql) =>
        Task.FromResult(0);

    #endregion
}
