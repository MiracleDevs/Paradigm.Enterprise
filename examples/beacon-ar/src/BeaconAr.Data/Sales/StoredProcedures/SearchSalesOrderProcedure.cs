using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

namespace BeaconAr.Data.Sales.StoredProcedures;

internal sealed class SearchSalesOrderProcedure : ResultStoredProcedureBase<SalesOrderSearchParameters, int, List<SalesOrderSearchRow>>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.SearchSalesOrder";

    protected override int? ExecutionTimeout => 30;

    #endregion
}
