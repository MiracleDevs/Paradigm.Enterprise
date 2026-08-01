namespace BeaconAr.Domain.MasterData.Contracts;

public sealed class AddressSearchRequest : MasterDataSearchRequest
{
    #region Properties

    public int? CustomerId { get; init; }

    public string? Type { get; init; }

    public AddressUsage? Usage { get; init; }

    #endregion
}
