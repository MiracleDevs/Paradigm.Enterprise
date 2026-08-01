namespace BeaconAr.Domain.MasterData.Contracts;

public sealed record CarrierUpdateRequest(
    string? Code,
    string? Name,
    string? ServiceLevel,
    string? TrackingUrlTemplate,
    bool IsActive);
