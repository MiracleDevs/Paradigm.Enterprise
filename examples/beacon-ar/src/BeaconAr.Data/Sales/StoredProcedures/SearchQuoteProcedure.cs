using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
using BeaconAr.Domain.Sales.Entities;

namespace BeaconAr.Data.Sales.StoredProcedures;

internal sealed class SearchQuoteProcedure : ResultStoredProcedureBase<QuoteSearchParameters, int, List<QuoteView>>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.SearchQuote";

    protected override int? ExecutionTimeout => 30;

    #endregion
}
