namespace BeaconAr.Domain.MasterData.Contracts;

public sealed record AddressDto(
    int Id,
    int CustomerId,
    string Type,
    string Label,
    string Line1,
    string? Line2,
    string City,
    string? State,
    string PostalCode,
    string Country,
    bool DefaultBilling,
    bool DefaultShipping,
    int? CreatedByUserId,
    DateTimeOffset CreationDate,
    int? ModifiedByUserId,
    DateTimeOffset? ModificationDate,
    string Version);
