namespace BeaconAr.Domain.Operations;

public interface IPersistenceSession
{
    void DiscardTrackedChanges();
}
