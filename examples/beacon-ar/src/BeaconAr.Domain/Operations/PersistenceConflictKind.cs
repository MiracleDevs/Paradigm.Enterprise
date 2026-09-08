namespace BeaconAr.Domain.Operations;

public enum PersistenceConflictKind
{
    None,
    Duplicate,
    Referenced,
    Concurrency,
}
