namespace BeaconAr.Domain.Operations;

public sealed record CreationResult<T>(T Value, bool Created);
