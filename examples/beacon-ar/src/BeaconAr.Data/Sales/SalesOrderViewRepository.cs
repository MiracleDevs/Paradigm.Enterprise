using BeaconAr.Data.Mappers;
using BeaconAr.Data.MasterData;
using BeaconAr.Data.Receivables;
using BeaconAr.Data.Sales.StoredProcedures;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;
using BeaconAr.Domain.Sales.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Paradigm.Enterprise.Data.Repositories;
using System.Data;

namespace BeaconAr.Data.Sales;

public sealed class SalesOrderViewRepository : RepositoryBase<ReceivablesDbContext, int>, ISalesOrderViewRepository
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

    #region Static Constructors

    static SalesOrderViewRepository() => StoreProcedureMappersRegisterer.RegisterMappers();

    #endregion

    #region Public Methods

    public Task<SalesOrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        GetByIdAsync(id, false, cancellationToken);

    public Task<SalesOrderDto?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken) =>
        GetByIdAsync(id, true, cancellationToken);

    public async Task<PageResult<SalesOrderSummaryDto>> SearchAsync(SalesOrderSearchRequest request, CancellationToken cancellationToken)
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
        SalesOrderSummaryDto[] items = (rows ?? []).Select(row => new SalesOrderSummaryDto(
            row.Id, row.OrderNumber, row.SourceQuoteId, row.CustomerId, row.CustomerAccountNumberSnapshot,
            row.CustomerNameSnapshot, (Domain.Sales.SalesOrderStatus)row.StatusId,
            row.RequestedShipDate.HasValue ? DateOnly.FromDateTime(row.RequestedShipDate.Value) : null,
            row.CarrierId, row.CarrierName, row.TrackingNumber, row.Subtotal, row.DiscountTotal, row.GrandTotal,
            row.CreatedByUserId, row.CreationDate, row.ModifiedByUserId, row.ModificationDate,
            SalesRequestValidator.EncodeVersion(row.RowVersion))).ToArray();
        return PageResultFactory.Create(items, request.PageNumber, request.PageSize, count);
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
            var header = await (
                from order in EntityContext.SalesOrders.AsNoTracking()
                join pricing in EntityContext.SalesOrderPricings on order.Id equals pricing.SalesOrderId
                join carrier in EntityContext.Carriers on order.CarrierId equals carrier.Id into carriers
                from carrier in carriers.DefaultIfEmpty()
                where order.Id == id && (includeDeleted || order.DeletionDate == null)
                select new
                {
                    Order = order,
                    CarrierName = carrier == null ? null : carrier.Name,
                    pricing.Subtotal,
                    pricing.DiscountTotal,
                    pricing.GrandTotal
                })
                .SingleOrDefaultAsync(cancellationToken);
            if (header is null)
            {
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
                return null;
            }

            SalesLineDto[] lines = await EntityContext.SalesOrderLines.AsNoTracking()
                .Where(line => line.SalesOrderId == id)
                .OrderBy(line => line.Id)
                .Select(line => new SalesLineDto(line.Id, line.ProductId, line.SkuSnapshot, line.ProductNameSnapshot,
                    line.Quantity, line.UnitPrice, line.DiscountPercent, line.LineSubtotal!.Value,
                    line.DiscountAmount!.Value, line.LineTotal!.Value))
                .ToArrayAsync(cancellationToken);
            var entity = header.Order;
            SalesOrderDto result = new(entity.Id, entity.OrderNumber, entity.SourceQuoteId, entity.CustomerId,
                entity.ShippingAddressId, (Domain.Sales.SalesOrderStatus)entity.StatusId, entity.RequestedShipDate,
                entity.CarrierId, header.CarrierName, entity.TrackingNumber,
                entity.CustomerAccountNumberSnapshot!, entity.CustomerNameSnapshot!, entity.CustomerEmailSnapshot!,
                entity.CustomerPhoneSnapshot, entity.ShippingLabelSnapshot!, entity.ShippingLine1Snapshot!,
                entity.ShippingLine2Snapshot, entity.ShippingCitySnapshot!, entity.ShippingStateSnapshot,
                entity.ShippingPostalCodeSnapshot!, entity.ShippingCountrySnapshot!, entity.ShippingAddressTypeCodeSnapshot!,
                lines, header.Subtotal ?? 0m, header.DiscountTotal ?? 0m, header.GrandTotal ?? 0m, entity.CreatedByUserId,
                entity.CreationDate, entity.ModifiedByUserId, entity.ModificationDate,
                SalesRequestValidator.EncodeVersion(entity.RowVersion));
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
