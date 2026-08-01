namespace BeaconAr.Domain.MasterData.Contracts;

public sealed record CarrierCreateRequest(
    string? Code,
    string? Name,
    string? ServiceLevel,
    string? TrackingUrlTemplate,
    bool IsActive = true);
