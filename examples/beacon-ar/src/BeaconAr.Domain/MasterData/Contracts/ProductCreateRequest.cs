namespace BeaconAr.Domain.MasterData.Contracts;

public sealed record ProductCreateRequest(
    string? Sku,
    string? Name,
    string? Category,
    decimal UnitPrice,
    int StockQuantity,
    string? ThumbnailUrl,
    bool IsActive = true);
