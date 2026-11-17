namespace SemanticViolations;

public sealed class OracleRefreshProcedure
    : Paradigm.Enterprise.Data.Oracle.StoredProcedures.StoredProcedureBase
{
    #region Overrides

    protected override string StoredProcedureName => "RefreshOrders";

    #endregion
}