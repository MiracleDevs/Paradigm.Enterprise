namespace BeaconAr.Domain.MasterData.Contracts;

public sealed class ProductSearchRequest : MasterDataSearchRequest
{
    #region Properties

    public bool? Active { get; init; }

    #endregion
}
