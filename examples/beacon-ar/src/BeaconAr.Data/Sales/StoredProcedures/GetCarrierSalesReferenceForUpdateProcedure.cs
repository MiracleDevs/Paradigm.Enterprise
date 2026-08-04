using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
namespace BeaconAr.Data.Sales.StoredProcedures;
internal sealed class GetCarrierSalesReferenceForUpdateProcedure : ResultStoredProcedureBase<GetCarrierSalesReferenceForUpdateParameters, CarrierSalesReferenceRow>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.GetCarrierSalesReferenceForUpdate";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
