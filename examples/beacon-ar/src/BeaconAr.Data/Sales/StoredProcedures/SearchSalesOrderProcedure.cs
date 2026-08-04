using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
using BeaconAr.Domain.Sales.Entities;

namespace BeaconAr.Data.Sales.StoredProcedures;

internal sealed class SearchSalesOrderProcedure : ResultStoredProcedureBase<SalesOrderSearchParameters, int, List<SalesOrderView>>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.SearchSalesOrder";

    protected override int? ExecutionTimeout => 30;

    #endregion
}
