namespace BeaconAr.Domain.MasterData.Contracts;

public sealed class CustomerSearchRequest : MasterDataSearchRequest
{
    #region Properties

    public bool? Active { get; init; }

    #endregion
}
