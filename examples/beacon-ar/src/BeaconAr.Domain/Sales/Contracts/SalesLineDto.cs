namespace BeaconAr.Domain.Sales.Contracts;

public sealed record SalesLineDto(
    int Id,
    int ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal LineSubtotal,
    decimal DiscountAmount,
    decimal LineTotal);
