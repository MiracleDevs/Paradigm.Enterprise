using BeaconAr.Data.Receivables.Context;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Receivables.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.MasterData;

public sealed class CarrierRepository : RepositoryBase<ReceivablesDbContext, int>, ICarrierRepository
{
    #region Constructors

    public CarrierRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public Task<Carrier?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.Carriers.SingleOrDefaultAsync(carrier => carrier.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, int? excludedId, CancellationToken cancellationToken) =>
        EntityContext.Carriers.AsNoTracking().AnyAsync(
            carrier => carrier.Code == code && (!excludedId.HasValue || carrier.Id != excludedId.Value), cancellationToken);

    public Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.SalesOrders.AsNoTracking().AnyAsync(order => order.CarrierId == id, cancellationToken);

    public void Add(Carrier carrier) => EntityContext.Carriers.Add(carrier);

    public void Delete(Carrier carrier) => EntityContext.Carriers.Remove(carrier);

    #endregion
}
