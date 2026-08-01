using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

namespace BeaconAr.Data.Sales.StoredProcedures;

internal sealed class SearchQuoteProcedure : ResultStoredProcedureBase<QuoteSearchParameters, int, List<QuoteSearchRow>>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.SearchQuote";

    protected override int? ExecutionTimeout => 30;

    #endregion
}
