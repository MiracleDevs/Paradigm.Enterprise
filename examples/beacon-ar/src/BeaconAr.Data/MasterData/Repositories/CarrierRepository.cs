using BeaconAr.Data.MasterData.Context;
using BeaconAr.Data.MasterData.StoredProcedures;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.MasterData.Repositories;

public sealed class CarrierRepository : EditRepositoryBase<Carrier, MasterDataDbContext, int>, ICarrierRepository
{
    #region Fields

    private static readonly HasCarrierReferencesProcedure HasReferencesProcedure = new();

    #endregion

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

    public async Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool result = await HasReferencesProcedure.ExecuteAsync(GetDbConnection(), new HasCarrierReferencesParameters { Id = id }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    #endregion
}
