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

public sealed class CustomerViewRepository : ReadRepositoryBase<CustomerView, ReceivablesDbContext, int>, ICustomerViewRepository
{
    #region Fields

    private static readonly SearchCustomerProcedure SearchProcedure = new();

    #endregion

    #region Constructors

    public CustomerViewRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Static Constructors

    static CustomerViewRepository()
    {
        StoreProcedureMappersRegisterer.RegisterMappers();
    }

    #endregion

    #region Public Methods

    public Task<CustomerView?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        AsQueryable().SingleOrDefaultAsync(view => view.Id == id, cancellationToken);

    public async Task<PageResult<CustomerView>> SearchAsync(CustomerSearchRequest request, CancellationToken cancellationToken)
    {
        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        string sort = string.IsNullOrWhiteSpace(request.SortField) ? "id" : request.SortField.Trim().ToLowerInvariant();
        cancellationToken.ThrowIfCancellationRequested();
        var (count, rows) = await SearchProcedure.ExecuteAsync(GetDbConnection(), new CustomerSearchParameters
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

    protected override IQueryable<CustomerView> AsQueryable() => EntityContext.CustomerViews.AsNoTracking();

    protected override Func<PaginationParametersBase, Task<(PaginationInfo, List<CustomerView>)>>
        GetSearchPaginatedFunction(PaginationParametersBase parameters)
    {
        if (parameters is not MasterDataViewSearchParameters search)
            throw new ArgumentException($"{nameof(CustomerViewRepository)} requires {nameof(MasterDataViewSearchParameters)}.", nameof(parameters));

        return async _ =>
        {
            CustomerSearchRequest request = ToLegacyRequest(search);
            PageResult<CustomerView> result = await SearchAsync(request, CancellationToken.None);
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

    private static CustomerSearchRequest ToLegacyRequest(MasterDataViewSearchParameters parameters) => new()
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
