namespace BeaconAr.Domain.MasterData.Contracts;

public sealed record CustomerUpdateRequest(
    string? AccountNumber,
    string? Name,
    string? Email,
    string? Phone,
    decimal CreditLimit,
    short PaymentTermsDays,
    bool IsActive);
