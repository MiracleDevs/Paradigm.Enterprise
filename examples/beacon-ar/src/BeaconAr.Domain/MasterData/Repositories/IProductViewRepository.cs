using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface IProductViewRepository : IReadRepository<ProductView, int>
{
    Task<ProductView?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PageResult<ProductView>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken);
}
