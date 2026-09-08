using BeaconAr.Data.Access.Context;
using BeaconAr.Data.MasterData.Context;
using BeaconAr.Data.Operations.Context;
using BeaconAr.Data.Sales.Context;
using BeaconAr.Domain.Operations;

namespace BeaconAr.Data.Operations;

public sealed class PersistenceSession : IPersistenceSession
{
    #region Fields

    private readonly AccessDbContext _accessContext;
    private readonly MasterDataDbContext _masterDataContext;
    private readonly OperationsDbContext _operationsContext;
    private readonly SalesDbContext _salesContext;

    #endregion

    #region Constructors

    public PersistenceSession(
        AccessDbContext accessContext,
        MasterDataDbContext masterDataContext,
        OperationsDbContext operationsContext,
        SalesDbContext salesContext)
    {
        _accessContext = accessContext;
        _masterDataContext = masterDataContext;
        _operationsContext = operationsContext;
        _salesContext = salesContext;
    }

    #endregion

    #region Public Methods

    public void DiscardTrackedChanges()
    {
        _accessContext.ChangeTracker.Clear();
        _masterDataContext.ChangeTracker.Clear();
        _operationsContext.ChangeTracker.Clear();
        _salesContext.ChangeTracker.Clear();
    }

    #endregion
}
