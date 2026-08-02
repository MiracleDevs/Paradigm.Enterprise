using BeaconAr.Domain.Receivables.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface IProductRepository : IRepository
{
    Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<bool> SkuExistsAsync(string sku, int? excludedId, CancellationToken cancellationToken);

    Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken);

    void Add(Product product);

    void Delete(Product product);
}
