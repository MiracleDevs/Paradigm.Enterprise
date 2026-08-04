using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
namespace BeaconAr.Data.Sales.StoredProcedures;
internal sealed class GetProductSalesReferenceForUpdateProcedure : ResultStoredProcedureBase<GetProductSalesReferenceForUpdateParameters, ProductSalesReferenceRow>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.GetProductSalesReferenceForUpdate";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
