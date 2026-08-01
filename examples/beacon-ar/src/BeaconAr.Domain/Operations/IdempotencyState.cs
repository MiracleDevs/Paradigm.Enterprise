namespace BeaconAr.Domain.Operations;

public enum IdempotencyState
{
    InProgress = 1,
    Completed = 2,
    Failed = 3,
}
