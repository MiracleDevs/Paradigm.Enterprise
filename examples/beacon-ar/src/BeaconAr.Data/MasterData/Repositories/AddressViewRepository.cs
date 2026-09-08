using BeaconAr.Data.MasterData.StoredProcedures;
using BeaconAr.Data.MasterData.Context;
using BeaconAr.Domain.MasterData;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;
using Paradigm.Enterprise.Domain.Dtos;

namespace BeaconAr.Data.MasterData.Repositories;

public sealed class AddressViewRepository : ReadRepositoryBase<CustomerAddressView, MasterDataDbContext, int>, IAddressViewRepository
{
    #region Fields

    private static readonly SearchAddressProcedure SearchProcedure = new();

    #endregion

    #region Constructors

    public AddressViewRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

#region Public Methods

    public Task<CustomerAddressView?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        AsQueryable().SingleOrDefaultAsync(view => view.Id == id, cancellationToken);

    public async Task<PageResult<CustomerAddressView>> SearchAsync(AddressSearchRequest request, CancellationToken cancellationToken)
    {
        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        string sort = string.IsNullOrWhiteSpace(request.SortField) ? "id" : request.SortField.Trim().ToLowerInvariant();
        cancellationToken.ThrowIfCancellationRequested();
        var (count, rows) = await SearchProcedure.ExecuteAsync(GetDbConnection(), new AddressSearchParameters
        {
            Search = search,
            CustomerId = request.CustomerId,
            Type = string.IsNullOrWhiteSpace(request.Type) ? null : request.Type.Trim().ToLowerInvariant(),
            Usage = request.Usage switch
            {
                AddressUsage.Billing => "billing",
                AddressUsage.Shipping => "shipping",
                _ => null,
            },
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

    protected override IQueryable<CustomerAddressView> AsQueryable() => EntityContext.CustomerAddressViews.AsNoTracking();

    protected override Func<PaginationParametersBase, Task<(PaginationInfo, List<CustomerAddressView>)>>
        GetSearchPaginatedFunction(PaginationParametersBase parameters)
    {
        if (parameters is not MasterDataViewSearchParameters search)
            throw new ArgumentException($"{nameof(AddressViewRepository)} requires {nameof(MasterDataViewSearchParameters)}.", nameof(parameters));

        return async _ =>
        {
            AddressSearchRequest request = ToLegacyRequest(search);
            PageResult<CustomerAddressView> result = await SearchAsync(request, CancellationToken.None);
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

    private static AddressSearchRequest ToLegacyRequest(MasterDataViewSearchParameters parameters) => new()
    {
        Search = parameters.Search,
        CustomerId = parameters.CustomerId,
        Type = parameters.Type,
        Usage = parameters.Usage,
        PageNumber = parameters.PageNumber ?? 1,
        PageSize = parameters.PageSize ?? 10,
        SortField = parameters.SortBy,
        SortDirection = string.Equals(parameters.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? SortDirection.Desc
            : SortDirection.Asc,
    };

    #endregion
}
