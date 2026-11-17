namespace SemanticViolations;

public sealed class MySqlRefreshProcedure
    : Paradigm.Enterprise.Data.MySql.StoredProcedures.StoredProcedureBase
{
    #region Overrides

    protected override string StoredProcedureName => "RefreshOrders";

    #endregion
}