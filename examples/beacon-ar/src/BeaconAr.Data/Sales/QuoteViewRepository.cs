using BeaconAr.Data.Mappers;
using BeaconAr.Data.MasterData;
using BeaconAr.Data.Receivables.Context;
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

public sealed class QuoteViewRepository : RepositoryBase<ReceivablesDbContext, int>, IQuoteViewRepository
{
    #region Fields

    private static readonly SearchQuoteProcedure SearchProcedure = new();

    #endregion

    #region Constructors

    public QuoteViewRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Static Constructors

    static QuoteViewRepository() => StoreProcedureMappersRegisterer.RegisterMappers();

    #endregion

    #region Public Methods

    public async Task<QuoteDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = EntityContext.Database.CurrentTransaction is null
            ? await EntityContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            var header = await (
                from quote in EntityContext.Quotes.AsNoTracking()
                join pricing in EntityContext.QuotePricingViews on quote.Id equals pricing.QuoteId
                where quote.Id == id && quote.DeletionDate == null
                select new
                {
                    Quote = quote,
                    pricing.Subtotal,
                    pricing.DiscountTotal,
                    pricing.GrandTotal,
                    SalesOrderId = EntityContext.SalesOrders.Where(order => order.SourceQuoteId == quote.Id)
                        .Select(order => (int?)order.Id).SingleOrDefault(),
                }).SingleOrDefaultAsync(cancellationToken);
            if (header is null)
            {
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
                return null;
            }

            SalesLineDto[] lines = await EntityContext.QuoteLines.AsNoTracking()
                .Where(line => line.QuoteId == id)
                .OrderBy(line => line.Id)
                .Select(line => new SalesLineDto(line.Id, line.ProductId, line.SkuSnapshot, line.ProductNameSnapshot,
                    line.Quantity, line.UnitPrice, line.DiscountPercent, line.LineSubtotal!.Value,
                    line.DiscountAmount!.Value, line.LineTotal!.Value))
                .ToArrayAsync(cancellationToken);
            var entity = header.Quote;
            QuoteDto result = new(entity.Id, entity.QuoteNumber, entity.CustomerId, entity.ShippingAddressId,
                entity.QuoteDate, entity.ValidUntil, (Domain.Sales.QuoteStatus)entity.StatusId, entity.Notes,
                entity.CustomerAccountNumberSnapshot, entity.CustomerNameSnapshot, entity.CustomerEmailSnapshot,
                entity.CustomerPhoneSnapshot, entity.ShippingLabelSnapshot, entity.ShippingLine1Snapshot,
                entity.ShippingLine2Snapshot, entity.ShippingCitySnapshot, entity.ShippingStateSnapshot,
                entity.ShippingPostalCodeSnapshot, entity.ShippingCountrySnapshot, entity.ShippingAddressTypeCodeSnapshot,
                lines, header.Subtotal ?? 0m, header.DiscountTotal ?? 0m, header.GrandTotal ?? 0m, header.SalesOrderId,
                entity.CreatedByUserId, entity.CreationDate, entity.ModifiedByUserId, entity.ModificationDate,
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

    public async Task<PageResult<QuoteSummaryDto>> SearchAsync(QuoteSearchRequest request, CancellationToken cancellationToken)
    {
        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        string sort = string.IsNullOrWhiteSpace(request.SortField) ? "quoteNumber" : request.SortField.Trim();
        cancellationToken.ThrowIfCancellationRequested();
        var (count, rows) = await SearchProcedure.ExecuteAsync(GetDbConnection(), new QuoteSearchParameters
        {
            Search = search,
            StatusId = request.Status.HasValue ? (int)request.Status.Value : null,
            CustomerId = request.CustomerId,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortField = sort,
            SortDirection = request.SortDirection == SortDirection.Desc ? "desc" : "asc",
        }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        QuoteSummaryDto[] items = (rows ?? []).Select(row => new QuoteSummaryDto(
            row.Id, row.QuoteNumber, row.CustomerId, row.CustomerAccountNumberSnapshot,
            row.CustomerNameSnapshot, DateOnly.FromDateTime(row.QuoteDate), DateOnly.FromDateTime(row.ValidUntil),
            (Domain.Sales.QuoteStatus)row.StatusId, row.Subtotal, row.DiscountTotal, row.GrandTotal,
            row.SalesOrderId, row.CreatedByUserId, row.CreationDate, row.ModifiedByUserId,
            row.ModificationDate, SalesRequestValidator.EncodeVersion(row.RowVersion))).ToArray();
        return PageResultFactory.Create(items, request.PageNumber, request.PageSize, count);
    }

    #endregion
}
