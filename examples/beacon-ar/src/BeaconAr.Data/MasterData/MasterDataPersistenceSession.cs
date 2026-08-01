using BeaconAr.Data.Receivables;
using BeaconAr.Domain.MasterData.Application;

namespace BeaconAr.Data.MasterData;

public sealed class MasterDataPersistenceSession : IMasterDataPersistenceSession
{
    #region Fields

    private readonly ReceivablesDbContext _context;

    #endregion

    #region Constructors

    public MasterDataPersistenceSession(ReceivablesDbContext context)
    {
        _context = context;
    }

    #endregion

    #region Public Methods

    public void DiscardTrackedChanges() => _context.ChangeTracker.Clear();

    #endregion
}
