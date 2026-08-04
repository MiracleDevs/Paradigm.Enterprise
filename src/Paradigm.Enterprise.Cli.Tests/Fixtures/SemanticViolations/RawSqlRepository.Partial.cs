namespace SemanticViolations;

public sealed partial class RawSqlRepository
{
    #region Private Methods

    private string PartialSql() => "SELECT Id FROM dbo.PartialOrders";

    #endregion
}
