namespace SemanticViolations;

public sealed class OracleOrderSearchProcedure
    : Paradigm.Enterprise.Data.Oracle.StoredProcedures.ResultStoredProcedureBase<OrderSearchParameters, List<Order>>
{
    #region Overrides

    protected override string StoredProcedureName => "SearchOrders";

    #endregion
}