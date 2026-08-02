using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Validation;
using BeaconAr.Domain.Receivables.Entities;
using BeaconAr.Interfaces.Receivables.Entities;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Providers;
using VersionTokenCodec = BeaconAr.Domain.MasterData.Application.VersionTokenCodec;

namespace BeaconAr.Providers.MasterData;

public sealed class ProductProvider
    : EditProviderBase<IProduct, Product, ProductView, IProductRepository, IProductViewRepository, int>, IProductProvider
{
    #region Fields

    private readonly MasterDataMutationCoordinator _mutations;

    #endregion

    #region Constructors

    public ProductProvider(
        IServiceProvider serviceProvider,
        MasterDataMutationCoordinator mutations)
        : base(serviceProvider)
    {
        _mutations = mutations;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<ProductDto>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken)
    {
        MasterDataRequestValidator.ValidateSearch(request, "id", "sku", "name");
        return SearchLegacyAsync(request, cancellationToken);
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        ToDto(await ViewRepository.GetByIdAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("product"));

    public async Task<ProductDto> CreateAsync(ProductCreateRequest request, CancellationToken cancellationToken)
    {
        ProductCreateRequest value = MasterDataRequestValidator.Normalize(request);
        int id = await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            if (await Repository.SkuExistsAsync(value.Sku!, null, cancellationToken))
                throw MasterDataMutationCoordinator.Duplicate("SKU");

            Product product = Product.Create(value, _mutations.UserId, now);
            await Repository.AddAsync(product);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            _mutations.AddAudit("product", product.Id, "created", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return product.Id;
        }, "referenced_record");
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ProductDto> UpdateAsync(int id, ProductUpdateRequest request, string expectedVersion, CancellationToken cancellationToken)
    {
        ProductUpdateRequest value = MasterDataRequestValidator.Normalize(request);
        VersionTokenCodec.Decode(expectedVersion);
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            Product product = await Repository.GetForUpdateAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("product");
            MasterDataMutationCoordinator.EnsureVersion(product.RowVersion, expectedVersion);
            if (await Repository.SkuExistsAsync(value.Sku!, id, cancellationToken))
                throw MasterDataMutationCoordinator.Duplicate("SKU");

            bool wasActive = product.IsActive;
            product.Replace(value, _mutations.UserId, now);
            await Repository.UpdateAsync(product);
            _mutations.AddAudit("product", product.Id, MasterDataMutationCoordinator.GetUpdateAction(wasActive, product.IsActive), "{\"changedFields\":[\"masterData\"]}", now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
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

    #region Overrides

    public override Task<PaginatedResultDto<ProductView>> SearchAsync<TParameters>(TParameters parameters)
    {
        if (parameters is not MasterDataViewSearchParameters search)
            throw new ArgumentException($"{nameof(ProductProvider)} requires {nameof(MasterDataViewSearchParameters)}.", nameof(parameters));
        MasterDataRequestValidator.ValidateSearch(search, "id", "sku", "name");
        return base.SearchAsync(parameters);
    }

    public override Task<ProductView> AddAsync(ProductView view) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<IEnumerable<ProductView>> AddAsync(List<ProductView> views) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<ProductView> UpdateAsync(ProductView view) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<IEnumerable<ProductView>> UpdateAsync(List<ProductView> views) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<ProductView> SaveAsync(ProductView view) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<IEnumerable<ProductView>> SaveAsync(IEnumerable<ProductView> views) => throw OfficialMasterDataMutationGuard.Create();

    public override Task DeleteAsync(int id) => throw OfficialMasterDataMutationGuard.Create();

    public override Task DeleteAsync(IEnumerable<int> ids) => throw OfficialMasterDataMutationGuard.Create();

    #endregion

    #region Private Methods

    private async Task ExecuteDeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            Product product = await Repository.GetForUpdateAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("product");
            MasterDataMutationCoordinator.EnsureVersion(product.RowVersion, expectedVersion);
            if (await Repository.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_record", "The product is referenced and cannot be deleted; deactivate it instead.");

            await Repository.DeleteAsync(product);
            _mutations.AddAudit("product", id, "deleted", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return id;
        }, "referenced_record");
    }

    private async Task<PageResult<ProductDto>> SearchLegacyAsync(ProductSearchRequest request, CancellationToken cancellationToken)
    {
        PageResult<ProductView> result = await ViewRepository.SearchAsync(request, cancellationToken);
        return new PageResult<ProductDto>(result.Items.Select(ToDto).ToArray(), result.PageNumber, result.PageSize,
            result.TotalPages, result.ItemsCount);
    }

    private static ProductDto ToDto(ProductView product) => new(
        product.Id, product.Sku, product.Name, product.Category, product.UnitPrice, product.StockQuantity,
        product.ThumbnailUrl, product.IsActive, product.CreatedByUserId, product.CreationDate,
        product.ModifiedByUserId, product.ModificationDate, VersionTokenCodec.Encode(product.RowVersion));

    #endregion
}
