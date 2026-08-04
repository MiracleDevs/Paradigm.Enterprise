using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
namespace BeaconAr.Data.MasterData.StoredProcedures;
internal sealed class HasAddressReferencesProcedure : ResultStoredProcedureBase<HasAddressReferencesParameters, bool>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.HasAddressReferences";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
