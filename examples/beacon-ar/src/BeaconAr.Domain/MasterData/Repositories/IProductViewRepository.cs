using BeaconAr.Domain.MasterData.Contracts;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface IProductViewRepository : IRepository
{
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PageResult<ProductDto>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken);
}
