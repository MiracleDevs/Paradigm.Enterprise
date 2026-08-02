using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Receivables.Entities;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;
using Paradigm.Enterprise.Domain.Uow;

namespace BeaconAr.Providers.Sales;

public sealed class QuoteConversionProvider : SalesProviderBase, IQuoteConversionProvider
{
    #region Fields

    private readonly IQuoteRepository _quotes;
    private readonly ISalesOrderRepository _orders;
    private readonly ISalesOrderViewRepository _views;

    #endregion

    #region Constructors

    public QuoteConversionProvider(
        IQuoteRepository quotes,
        ISalesOrderRepository orders,
        ISalesOrderViewRepository views,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        IApplicationOperationContext operationContext,
        TimeProvider timeProvider,
        ISalesPersistenceErrorClassifier errorClassifier,
        IPersistenceSession persistenceSession)
        : base(unitOfWork, auditLogs, operationContext, timeProvider, errorClassifier, persistenceSession)
    {
        _quotes = quotes;
        _orders = orders;
        _views = views;
    }

    #endregion

    #region Public Methods

    public async Task<QuoteConversionResult> ConvertAsync(
        int quoteId,
        CancellationToken cancellationToken)
    {
        if (quoteId <= 0)
            throw InvalidReference("quoteId", "Quote ID must be positive.");

        using ITransaction transaction = UnitOfWork.CreateTransaction();
        try
        {
            Quote quote = await _quotes.LockForConversionAsync(quoteId, cancellationToken) ?? throw NotFound("quote");
            SalesOrder? existing = await _orders.FindBySourceQuoteAsync(quoteId, cancellationToken);
            if (existing is not null)
            {
                transaction.Commit();
                return new QuoteConversionResult(await GetOrderAsync(existing.Id, cancellationToken), false);
            }

            quote.ValidateForConversion();
            DateTimeOffset now = TimeProvider.GetUtcNow();
            string number = await _orders.AllocateNumberAsync(cancellationToken);
            SalesOrder order = SalesOrder.CreateFromQuote(number, quote, OperationContext.UserId, now);
            _orders.Add(order);
            _orders.AddHistory(SalesOrderStatusHistory.Create(order, OperationContext.UserId, now));
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            cancellationToken.ThrowIfCancellationRequested();
            AddAudit("salesOrder", order.Id, "created", now, metadataJson: $"{{\"sourceQuoteId\":{quote.Id}}}");
            AddAudit("quote", quote.Id, "converted", now, metadataJson: $"{{\"salesOrderId\":{order.Id}}}");
            await UnitOfWork.CommitChangesAsync();
            transaction.Commit();
            return new QuoteConversionResult(await GetOrderAsync(order.Id, cancellationToken), true);
        }
        catch (Exception exception)
        {
            if (transaction.IsActive)
                transaction.Rollback();
            PersistenceSession.DiscardTrackedChanges();

            if (ErrorClassifier.IsSourceQuoteSingletonConflict(exception))
            {
                SalesOrder winner = await _orders.FindBySourceQuoteAsync(quoteId, cancellationToken)
                    ?? throw new SalesException("duplicate_key", "A quote conversion conflict occurred.", exception);
                return new QuoteConversionResult(await GetOrderAsync(winner.Id, cancellationToken), false);
            }
            if (exception is OperationCanceledException or SalesException)
                throw;
            PersistenceConflictKind conflict = ErrorClassifier.Classify(exception);
            if (conflict == PersistenceConflictKind.None)
                throw;
            throw conflict switch
            {
                PersistenceConflictKind.Concurrency => new SalesException("concurrency_conflict", "The quote was changed by another user.", exception),
                PersistenceConflictKind.Duplicate => new SalesException("duplicate_key", "A conflicting sales record already exists.", exception),
                PersistenceConflictKind.Referenced => new SalesException("reference_conflict", "A referenced record changed during conversion.", exception),
                _ => new InvalidOperationException("Unsupported persistence conflict classification.", exception),
            };
        }
    }

    #endregion

    #region Private Methods

    private async Task<SalesOrderDto> GetOrderAsync(int id, CancellationToken cancellationToken) =>
        await _views.GetByIdIncludingDeletedAsync(id, cancellationToken) ?? throw NotFound("sales order");

    #endregion
}
