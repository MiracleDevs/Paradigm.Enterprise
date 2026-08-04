using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
namespace BeaconAr.Data.Sales.StoredProcedures;
internal sealed class AllocateSalesOrderNumberProcedure : ResultStoredProcedureBase<AllocateSalesOrderNumberParameters, long>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.AllocateSalesOrderNumber";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
