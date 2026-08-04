using BeaconAr.Domain.MasterData.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface ICarrierRepository : IEditRepository<Carrier, int>
{
    Task<Carrier?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, int? excludedId, CancellationToken cancellationToken);

    Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken);

}
