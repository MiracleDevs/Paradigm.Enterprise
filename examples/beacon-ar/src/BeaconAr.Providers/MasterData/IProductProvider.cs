using BeaconAr.Domain.MasterData.Contracts;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.MasterData;

public interface IProductProvider : IProvider
{
    Task<PageResult<ProductDto>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken);

    Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<ProductDto> CreateAsync(ProductCreateRequest request, CancellationToken cancellationToken);

    Task<ProductDto> UpdateAsync(int id, ProductUpdateRequest request, string expectedVersion, CancellationToken cancellationToken);

    Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken);
}
