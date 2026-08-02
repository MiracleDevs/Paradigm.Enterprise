namespace BeaconAr.Domain.Access.Contracts;

public sealed record CurrentUserDto(
    int Id,
    string DisplayName,
    string? Email,
    IReadOnlyList<string> Policies);
