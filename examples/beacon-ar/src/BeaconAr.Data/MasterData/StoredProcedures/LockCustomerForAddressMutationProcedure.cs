using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

namespace BeaconAr.Data.MasterData.StoredProcedures;

internal sealed class LockCustomerForAddressMutationProcedure : ResultStoredProcedureBase<LockCustomerForAddressMutationParameters, int>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.LockCustomerForAddressMutation";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
