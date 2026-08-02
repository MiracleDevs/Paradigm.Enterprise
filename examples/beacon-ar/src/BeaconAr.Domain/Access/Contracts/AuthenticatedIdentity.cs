namespace BeaconAr.Domain.Access.Contracts;

public sealed record AuthenticatedIdentity(
    string Issuer,
    string Subject,
    string DisplayName,
    string? Email,
    IReadOnlyList<string> Policies);
