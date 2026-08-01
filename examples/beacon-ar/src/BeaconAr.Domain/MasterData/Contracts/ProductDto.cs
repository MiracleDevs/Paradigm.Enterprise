namespace BeaconAr.Domain.MasterData.Contracts;

public sealed record ProductDto(
    int Id,
    string Sku,
    string Name,
    string Category,
    decimal UnitPrice,
    int StockQuantity,
    string? ThumbnailUrl,
    bool IsActive,
    int? CreatedByUserId,
    DateTimeOffset CreationDate,
    int? ModifiedByUserId,
    DateTimeOffset? ModificationDate,
    string Version);
