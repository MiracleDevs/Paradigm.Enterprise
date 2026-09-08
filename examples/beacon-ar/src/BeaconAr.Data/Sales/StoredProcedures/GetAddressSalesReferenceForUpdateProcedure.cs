using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
namespace BeaconAr.Data.Sales.StoredProcedures;
internal sealed class GetAddressSalesReferenceForUpdateProcedure : ResultStoredProcedureBase<GetAddressSalesReferenceForUpdateParameters, AddressSalesReferenceRow>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.GetAddressSalesReferenceForUpdate";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
