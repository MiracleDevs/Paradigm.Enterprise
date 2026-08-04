using BeaconAr.Data.MasterData;
using BeaconAr.Data.Sales.Context;
using BeaconAr.Data.Sales.StoredProcedures;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Entities;
using BeaconAr.Domain.Sales.Repositories;
using BeaconAr.Domain.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Paradigm.Enterprise.Data.Repositories;
using System.Data;

namespace BeaconAr.Data.Sales.Repositories;

public sealed class SalesOrderViewRepository : RepositoryBase<SalesDbContext, int>, ISalesOrderViewRepository
{
    #region Fields

    private static readonly SearchSalesOrderProcedure SearchProcedure = new();

    #endregion

    #region Constructors

    public SalesOrderViewRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

#region Public Methods

    public Task<SalesOrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        GetByIdAsync(id, false, cancellationToken);

    public Task<SalesOrderDto?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken) =>
        GetByIdAsync(id, true, cancellationToken);

    public async Task<PageResult<SalesOrderView>> SearchAsync(SalesOrderSearchRequest request, CancellationToken cancellationToken)
    {
        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        string sort = string.IsNullOrWhiteSpace(request.SortField) ? "orderNumber" : request.SortField.Trim();
        cancellationToken.ThrowIfCancellationRequested();
        var (count, rows) = await SearchProcedure.ExecuteAsync(GetDbConnection(), new SalesOrderSearchParameters
        {
            Search = search,
            StatusId = request.Status.HasValue ? (int)request.Status.Value : null,
            CustomerId = request.CustomerId,
            SourceQuoteId = request.SourceQuoteId,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortField = sort,
            SortDirection = request.SortDirection == SortDirection.Desc ? "desc" : "asc",
        }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return PageResultFactory.Create(rows ?? [], request.PageNumber, request.PageSize, count);
    }

    #endregion

    #region Private Methods

    private async Task<SalesOrderDto?> GetByIdAsync(int id, bool includeDeleted, CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = EntityContext.Database.CurrentTransaction is null
            ? await EntityContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            SalesOrderView? header = await EntityContext.SalesOrderViews.AsNoTracking()
                .SingleOrDefaultAsync(order => order.Id == id && (includeDeleted || order.DeletionDate == null),
                    cancellationToken);
            if (header is null)
            {
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
                return null;
            }

            SalesLineDto[] lines = await EntityContext.SalesOrderLineViews.AsNoTracking()
                .Where(line => line.SalesOrderId == id)
                .OrderBy(line => line.Id)
                .Select(line => new SalesLineDto(line.Id, line.ProductId, line.SkuSnapshot, line.ProductNameSnapshot,
                    line.Quantity, line.UnitPrice, line.DiscountPercent, line.LineSubtotal!.Value,
                    line.DiscountAmount!.Value, line.LineTotal!.Value))
                .ToArrayAsync(cancellationToken);
            SalesOrderDto result = new(header.Id, header.OrderNumber, header.SourceQuoteId, header.CustomerId,
                header.ShippingAddressId, (BeaconAr.Interfaces.Sales.Enums.SalesOrderStatus)header.StatusId, header.RequestedShipDate,
                header.CarrierId, header.CarrierName, header.TrackingNumber,
                header.CustomerAccountNumberSnapshot!, header.CustomerNameSnapshot!, header.CustomerEmailSnapshot!,
                header.CustomerPhoneSnapshot, header.ShippingLabelSnapshot!, header.ShippingLine1Snapshot!,
                header.ShippingLine2Snapshot, header.ShippingCitySnapshot!, header.ShippingStateSnapshot,
                header.ShippingPostalCodeSnapshot!, header.ShippingCountrySnapshot!, header.ShippingAddressTypeCodeSnapshot!,
                lines, header.Subtotal ?? 0m, header.DiscountTotal ?? 0m, header.GrandTotal ?? 0m, header.CreatedByUserId,
                header.CreationDate, header.ModifiedByUserId, header.ModificationDate,
                VersionTokenCodec.Encode(header.RowVersion));
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return result;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    #endregion
}
