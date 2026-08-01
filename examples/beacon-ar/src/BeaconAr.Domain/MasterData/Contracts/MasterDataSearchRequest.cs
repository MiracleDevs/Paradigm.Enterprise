namespace BeaconAr.Domain.MasterData.Contracts;

public abstract class MasterDataSearchRequest
{
    #region Properties

    public string? Search { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? SortField { get; init; }

    public SortDirection SortDirection { get; init; } = SortDirection.Asc;

    #endregion
}
