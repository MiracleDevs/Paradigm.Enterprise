using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
namespace BeaconAr.Data.MasterData.StoredProcedures;
internal sealed class HasCustomerReferencesProcedure : ResultStoredProcedureBase<HasCustomerReferencesParameters, bool>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.HasCustomerReferences";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
