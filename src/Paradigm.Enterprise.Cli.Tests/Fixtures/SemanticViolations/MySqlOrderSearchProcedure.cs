namespace SemanticViolations;

public sealed class MySqlOrderSearchProcedure
    : Paradigm.Enterprise.Data.MySql.StoredProcedures.ResultStoredProcedureBase<OrderSearchParameters, List<Order>>
{
    #region Overrides

    protected override string StoredProcedureName => "SearchOrders";

    #endregion
}