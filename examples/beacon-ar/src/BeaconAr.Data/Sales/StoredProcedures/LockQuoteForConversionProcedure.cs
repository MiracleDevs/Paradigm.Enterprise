using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
namespace BeaconAr.Data.Sales.StoredProcedures;
internal sealed class LockQuoteForConversionProcedure : ResultStoredProcedureBase<LockQuoteForConversionParameters, int>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.LockQuoteForConversion";
    protected override int? ExecutionTimeout => 30;

    #endregion
}
