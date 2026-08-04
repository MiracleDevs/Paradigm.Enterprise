using Paradigm.Enterprise.Data.PostgreSql.StoredProcedures;

namespace SemanticViolations;

public sealed class PostgreSqlOrderStoredProcedure
    : ResultStoredProcedureBase<OrderSearchParameters, List<Order>>
{
    #region Overrides

    protected override string StoredProcedureName => "SearchOrders";

    #endregion
}
