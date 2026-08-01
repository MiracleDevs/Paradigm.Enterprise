namespace BeaconAr.Domain.MasterData.Contracts;

public sealed class CarrierSearchRequest : MasterDataSearchRequest
{
    #region Properties

    public bool? Active { get; init; }

    #endregion
}
