using BeaconAr.Data.Mappers;
using BeaconAr.Data.MasterData.StoredProcedures;
using BeaconAr.Data.Receivables;
using BeaconAr.Domain.MasterData;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Receivables.Generated;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.MasterData;

public sealed class AddressViewRepository : RepositoryBase<ReceivablesDbContext, int>, IAddressViewRepository
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

    #region Static Constructors

    static AddressViewRepository() => StoreProcedureMappersRegisterer.RegisterMappers();

    #endregion

    #region Public Methods

    public async Task<AddressDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        CustomerAddress? address = await EntityContext.CustomerAddresses.AsNoTracking()
            .Include(item => item.AddressType)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return address is null ? null : Map(address);
    }

    public async Task<PageResult<AddressDto>> SearchAsync(AddressSearchRequest request, CancellationToken cancellationToken)
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
        return PageResultFactory.Create((rows ?? []).Select(Map).ToArray(), request.PageNumber, request.PageSize, count);
    }

    #endregion

    #region Private Methods

    private static AddressDto Map(CustomerAddress address) => new(
        address.Id, address.CustomerId, address.AddressType.Code, address.Label, address.Line1, address.Line2,
        address.City, address.State, address.PostalCode, address.Country.Trim(), address.IsDefaultBilling,
        address.IsDefaultShipping, address.CreatedByUserId, address.CreationDate, address.ModifiedByUserId,
        address.ModificationDate, VersionTokenCodec.Encode(address.RowVersion));

    private static AddressDto Map(AddressSearchRow address) => new(
        address.Id, address.CustomerId, address.Type, address.Label, address.Line1, address.Line2,
        address.City, address.State, address.PostalCode, address.Country, address.DefaultBilling,
        address.DefaultShipping, address.CreatedByUserId, address.CreationDate, address.ModifiedByUserId,
        address.ModificationDate, VersionTokenCodec.Encode(address.RowVersion));

    #endregion
}
