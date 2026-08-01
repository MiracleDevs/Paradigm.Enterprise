using BeaconAr.Data.Receivables;
using BeaconAr.Domain.Operations;

namespace BeaconAr.Data.Operations;

public sealed class PersistenceSession : IPersistenceSession
{
    #region Fields

    private readonly ReceivablesDbContext _context;

    #endregion

    #region Constructors

    public PersistenceSession(ReceivablesDbContext context)
    {
        _context = context;
    }

    #endregion

    #region Public Methods

    public void DiscardTrackedChanges() => _context.ChangeTracker.Clear();

    #endregion
}
