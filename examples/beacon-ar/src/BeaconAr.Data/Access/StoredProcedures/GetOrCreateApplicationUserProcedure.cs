using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

namespace BeaconAr.Data.Access.StoredProcedures;

internal sealed class GetOrCreateApplicationUserProcedure : ResultStoredProcedureBase<GetOrCreateApplicationUserParameters, int>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.GetOrCreateApplicationUser";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
