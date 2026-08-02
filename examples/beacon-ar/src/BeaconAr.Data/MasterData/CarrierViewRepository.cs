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

public sealed class CarrierViewRepository : ReadRepositoryBase<CarrierView, ReceivablesDbContext, int>, ICarrierViewRepository
{
    #region Fields

    private static readonly SearchCarrierProcedure SearchProcedure = new();

    #endregion

    #region Constructors

    public CarrierViewRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Static Constructors

    static CarrierViewRepository()
    {
        StoreProcedureMappersRegisterer.RegisterMappers();
    }

    #endregion

    #region Public Methods

    public Task<CarrierView?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        AsQueryable().SingleOrDefaultAsync(view => view.Id == id, cancellationToken);

    public async Task<PageResult<CarrierView>> SearchAsync(CarrierSearchRequest request, CancellationToken cancellationToken)
    {
        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        string sort = string.IsNullOrWhiteSpace(request.SortField) ? "id" : request.SortField.Trim().ToLowerInvariant();
        cancellationToken.ThrowIfCancellationRequested();
        var (count, rows) = await SearchProcedure.ExecuteAsync(GetDbConnection(), new CarrierSearchParameters
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

    protected override IQueryable<CarrierView> AsQueryable() => EntityContext.CarrierViews.AsNoTracking();

    protected override Func<PaginationParametersBase, Task<(PaginationInfo, List<CarrierView>)>>
        GetSearchPaginatedFunction(PaginationParametersBase parameters)
    {
        if (parameters is not MasterDataViewSearchParameters search)
            throw new ArgumentException($"{nameof(CarrierViewRepository)} requires {nameof(MasterDataViewSearchParameters)}.", nameof(parameters));

        return async _ =>
        {
            CarrierSearchRequest request = ToLegacyRequest(search);
            PageResult<CarrierView> result = await SearchAsync(request, CancellationToken.None);
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

    private static CarrierSearchRequest ToLegacyRequest(MasterDataViewSearchParameters parameters) => new()
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
