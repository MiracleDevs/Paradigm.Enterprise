namespace BeaconAr.Domain.Operations;

public sealed record IdempotencyDescriptor(
    string Operation,
    byte[] KeyHash,
    byte[] RequestHash,
    DateTimeOffset ExpirationDate);
