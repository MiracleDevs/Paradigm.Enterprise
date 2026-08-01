namespace BeaconAr.Domain.Sales.Repositories;

public sealed record CustomerSalesReference(
    int Id,
    string AccountNumber,
    string Name,
    string Email,
    string? Phone,
    bool IsActive);
