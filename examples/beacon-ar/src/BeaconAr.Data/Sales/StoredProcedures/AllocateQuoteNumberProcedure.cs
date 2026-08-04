using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
namespace BeaconAr.Data.Sales.StoredProcedures;
internal sealed class AllocateQuoteNumberProcedure : ResultStoredProcedureBase<AllocateQuoteNumberParameters, long>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.AllocateQuoteNumber";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
