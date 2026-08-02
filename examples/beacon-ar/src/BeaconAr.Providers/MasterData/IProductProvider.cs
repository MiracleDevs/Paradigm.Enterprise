using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Receivables.Entities;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.MasterData;

public interface IProductProvider : IEditProvider<ProductView, int>
{
    Task<PageResult<ProductDto>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken);

    Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<ProductDto> CreateAsync(ProductCreateRequest request, CancellationToken cancellationToken);

    Task<ProductDto> UpdateAsync(int id, ProductUpdateRequest request, string expectedVersion, CancellationToken cancellationToken);

    Task<PageResult<ProductView>> SearchForApiAsync(ProductSearchRequest request, CancellationToken cancellationToken);

    Task<ProductView> GetForApiAsync(int id, CancellationToken cancellationToken);

    Task<ProductView> CreateForApiAsync(ProductCreateRequest request, CancellationToken cancellationToken);

    Task<ProductView> UpdateForApiAsync(int id, ProductUpdateRequest request, string expectedVersion, CancellationToken cancellationToken);

    Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken);
}
