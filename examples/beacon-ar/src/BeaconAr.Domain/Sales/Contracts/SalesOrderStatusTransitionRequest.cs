namespace BeaconAr.Domain.Sales.Contracts;

public sealed record SalesOrderStatusTransitionRequest(SalesOrderStatus Status, int? CarrierId = null, string? TrackingNumber = null);
