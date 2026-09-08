namespace BeaconAr.Domain.MasterData.Contracts;

public sealed record CustomerCreateRequest(
    string? AccountNumber,
    string? Name,
    string? Email,
    string? Phone,
    decimal CreditLimit,
    short PaymentTermsDays,
    bool IsActive = true);
