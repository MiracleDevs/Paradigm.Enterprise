using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
namespace BeaconAr.Data.MasterData.StoredProcedures;
internal sealed class HasProductReferencesProcedure : ResultStoredProcedureBase<HasProductReferencesParameters, bool>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.HasProductReferences";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
