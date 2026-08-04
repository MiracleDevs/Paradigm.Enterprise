using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
namespace BeaconAr.Data.Sales.StoredProcedures;
internal sealed class GetCustomerSalesReferenceForUpdateProcedure : ResultStoredProcedureBase<GetCustomerSalesReferenceForUpdateParameters, CustomerSalesReferenceRow>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.GetCustomerSalesReferenceForUpdate";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
