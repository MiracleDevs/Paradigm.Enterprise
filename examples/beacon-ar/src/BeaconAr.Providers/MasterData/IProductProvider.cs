using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Entities;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.MasterData;

public interface IProductProvider : IProvider
{
    Task<PageResult<ProductView>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken);

    Task<ProductView> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<ProductView> CreateAsync(ProductCreateRequest request, CancellationToken cancellationToken);

    Task<ProductView> UpdateAsync(int id, ProductUpdateRequest request, string expectedVersion, CancellationToken cancellationToken);

    Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken);
}
