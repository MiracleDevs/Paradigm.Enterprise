using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

namespace BeaconAr.Data.Reporting.StoredProcedures;

internal sealed class GetDashboardSummaryProcedure : ResultStoredProcedureBase<DashboardParameters, DashboardRow>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.GetDashboardSummary";

    protected override int? ExecutionTimeout => 30;

    #endregion
}
