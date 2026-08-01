using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

namespace BeaconAr.Data.MasterData.StoredProcedures;

internal sealed class SearchProductProcedure : ResultStoredProcedureBase<ProductSearchParameters, int, List<ProductSearchRow>>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.SearchProduct";

    protected override int? ExecutionTimeout => 30;

    #endregion
}
