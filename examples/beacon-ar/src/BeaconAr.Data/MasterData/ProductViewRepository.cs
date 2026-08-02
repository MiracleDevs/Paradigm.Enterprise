using BeaconAr.Data.Mappers;
using BeaconAr.Data.MasterData.StoredProcedures;
using BeaconAr.Data.Receivables.Context;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Receivables.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;
using Paradigm.Enterprise.Domain.Dtos;

namespace BeaconAr.Data.MasterData;

public sealed class ProductViewRepository : ReadRepositoryBase<ProductView, ReceivablesDbContext, int>, IProductViewRepository
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

    static ProductViewRepository()
    {
        StoreProcedureMappersRegisterer.RegisterMappers();
    }

    #endregion

    #region Public Methods

    public Task<ProductView?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        AsQueryable().SingleOrDefaultAsync(view => view.Id == id, cancellationToken);

    public async Task<PageResult<ProductView>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken)
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
        return PageResultFactory.Create(rows ?? [], request.PageNumber, request.PageSize, count);
    }

    #endregion

    #region Overrides

    protected override IQueryable<ProductView> AsQueryable() => EntityContext.ProductViews.AsNoTracking();

    protected override Func<PaginationParametersBase, Task<(PaginationInfo, List<ProductView>)>>
        GetSearchPaginatedFunction(PaginationParametersBase parameters)
    {
        if (parameters is not MasterDataViewSearchParameters search)
            throw new ArgumentException($"{nameof(ProductViewRepository)} requires {nameof(MasterDataViewSearchParameters)}.", nameof(parameters));

        return async _ =>
        {
            ProductSearchRequest request = ToLegacyRequest(search);
            PageResult<ProductView> result = await SearchAsync(request, CancellationToken.None);
            return (new PaginationInfo
            {
                ItemsCount = result.ItemsCount,
                PageNumber = result.PageNumber,
                TotalPages = result.TotalPages,
            }, result.Items.ToList());
        };
    }

    #endregion

    #region Private Methods

    private static ProductSearchRequest ToLegacyRequest(MasterDataViewSearchParameters parameters) => new()
    {
        Search = parameters.Search,
        Active = parameters.Active,
        PageNumber = parameters.PageNumber ?? 1,
        PageSize = parameters.PageSize ?? 10,
        SortField = parameters.SortBy,
        SortDirection = string.Equals(parameters.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? SortDirection.Desc
            : SortDirection.Asc,
    };

    #endregion
}
