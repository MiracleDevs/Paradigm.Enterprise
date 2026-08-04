using BeaconAr.Domain.MasterData.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface IProductRepository : IEditRepository<Product, int>
{
    Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<bool> SkuExistsAsync(string sku, int? excludedId, CancellationToken cancellationToken);

    Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken);

}
