using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

namespace BeaconAr.Data.MasterData.StoredProcedures;

internal sealed class SearchCustomerProcedure : ResultStoredProcedureBase<CustomerSearchParameters, int, List<CustomerSearchRow>>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.SearchCustomer";

    protected override int? ExecutionTimeout => 30;

    #endregion
}
