using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Entities;
using Paradigm.Enterprise.Domain.Uow;
using VersionTokenCodec = BeaconAr.Domain.Operations.VersionTokenCodec;

namespace BeaconAr.Providers.MasterData;

public sealed class ProductProvider : IProductProvider
{
    #region Fields

    private readonly MasterDataMutationCoordinator _mutations;
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductViewRepository _views;

    #endregion

    #region Constructors

    public ProductProvider(
        IProductRepository repository,
        IProductViewRepository views,
        IUnitOfWork unitOfWork,
        MasterDataMutationCoordinator mutations)
    {
        _repository = repository;
        _views = views;
        _unitOfWork = unitOfWork;
        _mutations = mutations;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<ProductView>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken)
    {
        request.Validate("id", "sku", "name");
        return _views.SearchAsync(request, cancellationToken);
    }

    public async Task<ProductView> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await _views.GetByIdAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("product");

    public async Task<ProductView> CreateAsync(ProductCreateRequest request, CancellationToken cancellationToken)
    {
        DateTimeOffset now = _mutations.UtcNow;
        Product product = Product.Create(request, _mutations.UserId, now);
        int id = await _mutations.ExecuteAsync(async () =>
        {
            if (await _repository.SkuExistsAsync(product.Sku, null, cancellationToken))
                throw MasterDataMutationCoordinator.Duplicate("SKU");

            await _repository.AddAsync(product);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            _mutations.AddAudit("product", product.Id, "created", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            return product.Id;
        }, "referenced_record");
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ProductView> UpdateAsync(int id, ProductUpdateRequest request, string expectedVersion, CancellationToken cancellationToken)
    {
        VersionTokenCodec.Decode(expectedVersion);
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            Product product = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("product");
            MasterDataMutationCoordinator.EnsureVersion(product.RowVersion, expectedVersion);
            if (await _repository.SkuExistsAsync(request.Sku?.Trim() ?? string.Empty, id, cancellationToken))
                throw MasterDataMutationCoordinator.Duplicate("SKU");

            bool wasActive = product.IsActive;
            product.Replace(request, _mutations.UserId, now);
            await _repository.UpdateAsync(product);
            _mutations.AddAudit("product", product.Id, MasterDataMutationCoordinator.GetUpdateAction(wasActive, product.IsActive), "{\"changedFields\":[\"masterData\"]}", now);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            return product.Id;
        }, "referenced_record");
        return await GetByIdAsync(id, cancellationToken);
    }

    public Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        VersionTokenCodec.Decode(expectedVersion);
        return ExecuteDeleteAsync(id, expectedVersion, cancellationToken);
    }

    #endregion

    #region Private Methods

    private async Task ExecuteDeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            Product product = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("product");
            MasterDataMutationCoordinator.EnsureVersion(product.RowVersion, expectedVersion);
            if (await _repository.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_record", "The product is referenced and cannot be deleted; deactivate it instead.");

            await _repository.DeleteAsync(product);
            _mutations.AddAudit("product", id, "deleted", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            return id;
        }, "referenced_record");
    }

    #endregion
}
