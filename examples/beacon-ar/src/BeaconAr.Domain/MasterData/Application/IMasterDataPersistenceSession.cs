namespace BeaconAr.Domain.MasterData.Application;

public interface IMasterDataPersistenceSession
{
    void DiscardTrackedChanges();
}
