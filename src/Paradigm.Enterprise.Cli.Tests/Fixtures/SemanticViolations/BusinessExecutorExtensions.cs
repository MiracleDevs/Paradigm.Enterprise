namespace SemanticViolations;

public static class BusinessExecutorExtensions
{
    #region Public Methods

    public static Task<int> QueryRowsAsync(
        this BusinessExecutor executor,
        string sql) => Task.FromResult(sql.Length);

    #endregion
}
