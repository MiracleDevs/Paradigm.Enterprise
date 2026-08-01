namespace BeaconAr.Domain.MasterData.Contracts;

public sealed record CarrierDto(
    int Id,
    string Code,
    string Name,
    string ServiceLevel,
    string? TrackingUrlTemplate,
    bool IsActive,
    int? CreatedByUserId,
    DateTimeOffset CreationDate,
    int? ModifiedByUserId,
    DateTimeOffset? ModificationDate,
    string Version);
