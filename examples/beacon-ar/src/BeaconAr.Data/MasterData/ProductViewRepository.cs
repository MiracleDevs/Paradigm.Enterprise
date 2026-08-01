using BeaconAr.Data.Mappers;
using BeaconAr.Data.MasterData.StoredProcedures;
using BeaconAr.Data.Receivables;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Receivables.Generated;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.MasterData;

public sealed class ProductViewRepository : RepositoryBase<ReceivablesDbContext, int>, IProductViewRepository
{
    #region Fields

    private static readonly SearchProductProcedure SearchProcedure = new();

    #endregion

    #region Constructors

    public ProductViewRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Static Constructors

    static ProductViewRepository() => StoreProcedureMappersRegisterer.RegisterMappers();

    #endregion

    #region Public Methods

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Product? product = await EntityContext.Products.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return product is null ? null : Map(product);
    }

    public async Task<PageResult<ProductDto>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken)
    {
        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        string sort = string.IsNullOrWhiteSpace(request.SortField) ? "id" : request.SortField.Trim().ToLowerInvariant();
        cancellationToken.ThrowIfCancellationRequested();
        var (count, rows) = await SearchProcedure.ExecuteAsync(GetDbConnection(), new ProductSearchParameters
        {
            Search = search,
            Active = request.Active,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortField = sort,
            SortDirection = request.SortDirection == SortDirection.Desc ? "desc" : "asc",
        }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return PageResultFactory.Create((rows ?? []).Select(Map).ToArray(), request.PageNumber, request.PageSize, count);
    }

    #endregion

    #region Private Methods

    private static ProductDto Map(Product product) => new(
        product.Id, product.Sku, product.Name, product.Category, product.UnitPrice, product.StockQuantity,
        product.ThumbnailUrl, product.IsActive, product.CreatedByUserId, product.CreationDate,
        product.ModifiedByUserId, product.ModificationDate, VersionTokenCodec.Encode(product.RowVersion));

    private static ProductDto Map(ProductSearchRow product) => new(
        product.Id, product.Sku, product.Name, product.Category, product.UnitPrice, product.StockQuantity,
        product.ThumbnailUrl, product.IsActive, product.CreatedByUserId, product.CreationDate,
        product.ModifiedByUserId, product.ModificationDate, VersionTokenCodec.Encode(product.RowVersion));

    #endregion
}
