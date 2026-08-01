namespace BeaconAr.Domain.Sales.Contracts;

public sealed record QuoteCreateRequest(
    int CustomerId,
    int ShippingAddressId,
    DateOnly QuoteDate,
    DateOnly ValidUntil,
    string? Notes,
    IReadOnlyList<SalesLineRequest> Lines);
