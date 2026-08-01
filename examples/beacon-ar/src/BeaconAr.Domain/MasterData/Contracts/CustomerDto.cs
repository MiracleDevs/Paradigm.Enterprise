namespace BeaconAr.Domain.MasterData.Contracts;

public sealed record CustomerDto(
    int Id,
    string AccountNumber,
    string Name,
    string Email,
    string? Phone,
    decimal CreditLimit,
    short PaymentTermsDays,
    bool IsActive,
    int? CreatedByUserId,
    DateTimeOffset CreationDate,
    int? ModifiedByUserId,
    DateTimeOffset? ModificationDate,
    string Version);
