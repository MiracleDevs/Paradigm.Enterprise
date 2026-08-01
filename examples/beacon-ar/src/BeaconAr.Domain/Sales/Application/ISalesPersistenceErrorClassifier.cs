using BeaconAr.Domain.Operations;

namespace BeaconAr.Domain.Sales.Application;

public interface ISalesPersistenceErrorClassifier
{
    PersistenceConflictKind Classify(Exception exception);

    bool IsSourceQuoteSingletonConflict(Exception exception);
}
