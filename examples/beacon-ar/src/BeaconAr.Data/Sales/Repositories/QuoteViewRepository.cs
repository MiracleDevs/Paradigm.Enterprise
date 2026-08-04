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

public sealed class QuoteViewRepository : RepositoryBase<SalesDbContext, int>, IQuoteViewRepository
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

#region Public Methods

    public async Task<QuoteDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = EntityContext.Database.CurrentTransaction is null
            ? await EntityContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            QuoteView? header = await EntityContext.QuoteViews.AsNoTracking()
                .SingleOrDefaultAsync(quote => quote.Id == id && quote.DeletionDate == null, cancellationToken);
            if (header is null)
            {
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
                return null;
            }

            SalesLineDto[] lines = await EntityContext.QuoteLineViews.AsNoTracking()
                .Where(line => line.QuoteId == id)
                .OrderBy(line => line.Id)
                .Select(line => new SalesLineDto(line.Id, line.ProductId, line.SkuSnapshot, line.ProductNameSnapshot,
                    line.Quantity, line.UnitPrice, line.DiscountPercent, line.LineSubtotal!.Value,
                    line.DiscountAmount!.Value, line.LineTotal!.Value))
                .ToArrayAsync(cancellationToken);
            QuoteDto result = new(header.Id, header.QuoteNumber, header.CustomerId, header.ShippingAddressId,
                header.QuoteDate, header.ValidUntil, (BeaconAr.Interfaces.Sales.Enums.QuoteStatus)header.StatusId, header.Notes,
                header.CustomerAccountNumberSnapshot, header.CustomerNameSnapshot, header.CustomerEmailSnapshot,
                header.CustomerPhoneSnapshot, header.ShippingLabelSnapshot, header.ShippingLine1Snapshot,
                header.ShippingLine2Snapshot, header.ShippingCitySnapshot, header.ShippingStateSnapshot,
                header.ShippingPostalCodeSnapshot, header.ShippingCountrySnapshot, header.ShippingAddressTypeCodeSnapshot,
                lines, header.Subtotal ?? 0m, header.DiscountTotal ?? 0m, header.GrandTotal ?? 0m, header.SalesOrderId,
                header.CreatedByUserId, header.CreationDate, header.ModifiedByUserId, header.ModificationDate,
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

    public async Task<PageResult<QuoteView>> SearchAsync(QuoteSearchRequest request, CancellationToken cancellationToken)
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
        return PageResultFactory.Create(rows ?? [], request.PageNumber, request.PageSize, count);
    }

    #endregion
}
