using BeaconAr.Domain.MasterData.Entities;
using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

namespace BeaconAr.Data.MasterData.StoredProcedures;

internal sealed class SearchAddressProcedure : ResultStoredProcedureBase<AddressSearchParameters, int, List<CustomerAddressView>>
{
    #region Properties

    protected override string StoredProcedureName => "dbo.SearchAddress";

    protected override int? ExecutionTimeout => 30;

    #endregion
}
