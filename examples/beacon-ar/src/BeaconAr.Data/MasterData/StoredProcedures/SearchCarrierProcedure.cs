using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

namespace BeaconAr.Data.MasterData.StoredProcedures;

internal sealed class SearchCarrierProcedure : ResultStoredProcedureBase<CarrierSearchParameters, int, List<CarrierSearchRow>>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.SearchCarrier";

    protected override int? ExecutionTimeout => 30;

    #endregion
}
