using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

namespace BeaconAr.Data.Operations.StoredProcedures;

internal sealed class LockIdempotencyRequestProcedure : ResultStoredProcedureBase<LockIdempotencyRequestParameters, long>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.LockIdempotencyRequest";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
