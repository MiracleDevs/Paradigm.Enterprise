using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
namespace BeaconAr.Data.MasterData.StoredProcedures;
internal sealed class HasCarrierReferencesProcedure : ResultStoredProcedureBase<HasCarrierReferencesParameters, bool>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.HasCarrierReferences";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
