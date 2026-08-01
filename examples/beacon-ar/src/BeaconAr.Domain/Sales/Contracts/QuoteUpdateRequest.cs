namespace BeaconAr.Domain.Sales.Contracts;

public sealed record QuoteUpdateRequest(
    int CustomerId,
    int ShippingAddressId,
    DateOnly QuoteDate,
    DateOnly ValidUntil,
    string? Notes,
    IReadOnlyList<SalesLineRequest> Lines);
