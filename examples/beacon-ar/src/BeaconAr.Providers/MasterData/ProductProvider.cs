using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Validation;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Receivables.Entities;
using Paradigm.Enterprise.Domain.Uow;
using VersionTokenCodec = BeaconAr.Domain.MasterData.Application.VersionTokenCodec;

namespace BeaconAr.Providers.MasterData;

public sealed class ProductProvider : MasterDataProviderBase, IProductProvider
{
    #region Fields

    private readonly IProductRepository _products;
    private readonly IProductViewRepository _views;

    #endregion

    #region Constructors

    public ProductProvider(
        IProductRepository products,
        IProductViewRepository views,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        IApplicationOperationContext operationContext,
        TimeProvider timeProvider,
        IMasterDataPersistenceErrorClassifier errorClassifier,
        IPersistenceSession persistenceSession)
        : base(unitOfWork, auditLogs, operationContext, timeProvider, errorClassifier, persistenceSession)
    {
        _products = products;
        _views = views;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<ProductDto>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken)
    {
        MasterDataRequestValidator.ValidateSearch(request, "id", "sku", "name");
        return _views.SearchAsync(request, cancellationToken);
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await _views.GetByIdAsync(id, cancellationToken) ?? throw NotFound("product");

    public async Task<ProductDto> CreateAsync(ProductCreateRequest request, CancellationToken cancellationToken)
    {
        ProductCreateRequest value = MasterDataRequestValidator.Normalize(request);
        int id = await ExecuteMutationAsync(async () =>
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            if (await _products.SkuExistsAsync(value.Sku!, null, cancellationToken))
                throw Duplicate("SKU");

            Product product = Product.Create(value, OperationContext.UserId, now);
            _products.Add(product);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            AddAudit("product", product.Id, "created", recordedAt: now);
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
        await ExecuteMutationAsync(async () =>
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            Product product = await _products.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound("product");
            EnsureVersion(product.RowVersion, expectedVersion);
            if (await _products.SkuExistsAsync(value.Sku!, id, cancellationToken))
                throw Duplicate("SKU");

            bool wasActive = product.IsActive;
            product.Replace(value, OperationContext.UserId, now);
            AddAudit("product", product.Id, GetUpdateAction(wasActive, product.IsActive), "{\"changedFields\":[\"masterData\"]}", now);
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

    #region Private Methods

    private async Task ExecuteDeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        await ExecuteMutationAsync(async () =>
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            Product product = await _products.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound("product");
            EnsureVersion(product.RowVersion, expectedVersion);
            if (await _products.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_record", "The product is referenced and cannot be deleted; deactivate it instead.");

            _products.Delete(product);
            AddAudit("product", id, "deleted", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return id;
        }, "referenced_record");
    }

    #endregion
}
