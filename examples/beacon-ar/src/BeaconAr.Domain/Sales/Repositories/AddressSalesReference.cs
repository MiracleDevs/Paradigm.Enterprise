namespace BeaconAr.Domain.Sales.Repositories;

public sealed record AddressSalesReference(
    int Id,
    int CustomerId,
    string AddressTypeCode,
    string Label,
    string Line1,
    string? Line2,
    string City,
    string? State,
    string PostalCode,
    string Country);
