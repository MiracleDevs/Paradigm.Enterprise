namespace BeaconAr.Domain.Sales.Contracts;

public sealed record SalesLineRequest(int ProductId, int Quantity, decimal UnitPrice, decimal DiscountPercent);
