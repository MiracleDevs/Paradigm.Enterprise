namespace BeaconAr.Domain.MasterData.Application;

public interface IMasterDataPersistenceErrorClassifier
{
    PersistenceConflictKind Classify(Exception exception);
}
