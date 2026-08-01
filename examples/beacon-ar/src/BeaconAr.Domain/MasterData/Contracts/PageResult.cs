namespace BeaconAr.Domain.MasterData.Contracts;

public sealed record PageResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalPages,
    int ItemsCount);
