namespace BeaconAr.Domain.MasterData.Application;

public enum PersistenceConflictKind
{
    None,
    Duplicate,
    Referenced,
    Concurrency,
}
