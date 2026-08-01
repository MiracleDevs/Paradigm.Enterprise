namespace BeaconAr.Domain.Sales.Contracts;

public sealed record SalesOrderCreateRequest(
    int CustomerId,
    int ShippingAddressId,
    DateOnly? RequestedShipDate,
    int? CarrierId,
    string? TrackingNumber,
    IReadOnlyList<SalesLineRequest> Lines);
