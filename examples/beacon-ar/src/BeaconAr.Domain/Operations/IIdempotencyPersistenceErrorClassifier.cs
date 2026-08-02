namespace BeaconAr.Domain.Operations;

public interface IIdempotencyPersistenceErrorClassifier
{
    bool IsKeyConflict(Exception exception);
}
