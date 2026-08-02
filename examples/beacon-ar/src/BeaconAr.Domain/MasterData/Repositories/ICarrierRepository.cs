using BeaconAr.Domain.Receivables.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface ICarrierRepository : IRepository
{
    Task<Carrier?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, int? excludedId, CancellationToken cancellationToken);

    Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken);

    void Add(Carrier carrier);

    void Delete(Carrier carrier);
}
