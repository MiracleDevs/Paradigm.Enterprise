using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Receivables.Generated;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;
using BeaconAr.Domain.Sales.Validation;
using Paradigm.Enterprise.Domain.Uow;
using QuoteState = BeaconAr.Domain.Sales.QuoteStatus;

namespace BeaconAr.Providers.Sales;

public sealed class QuoteProvider : SalesProviderBase, IQuoteProvider
{
    #region Fields

    private readonly IQuoteRepository _quotes;
    private readonly IQuoteViewRepository _views;
    private readonly ISalesReferenceRepository _references;

    #endregion

    #region Constructors

    public QuoteProvider(
        IQuoteRepository quotes,
        IQuoteViewRepository views,
        ISalesReferenceRepository references,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        IApplicationOperationContext operationContext,
        TimeProvider timeProvider,
        ISalesPersistenceErrorClassifier errorClassifier,
        IPersistenceSession persistenceSession)
        : base(unitOfWork, auditLogs, operationContext, timeProvider, errorClassifier, persistenceSession)
    {
        _quotes = quotes;
        _views = views;
        _references = references;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<QuoteSummaryDto>> SearchAsync(QuoteSearchRequest request, CancellationToken cancellationToken)
    {
        SalesRequestValidator.Validate(request);
        return _views.SearchAsync(request, cancellationToken);
    }

    public async Task<QuoteDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await _views.GetByIdAsync(id, cancellationToken) ?? throw NotFound("quote");

    public async Task<QuoteDto> CreateAsync(QuoteCreateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        SalesRequestValidator.Validate(request);
        int id = await ExecuteMutationAsync(async () =>
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            var (customer, address, products) = await ResolveReferencesAsync(
                request.CustomerId, request.ShippingAddressId, request.Lines, cancellationToken);
            string number = await _quotes.AllocateNumberAsync(cancellationToken);
            Quote quote = Quote.CreateDraft(number, request, customer, address, products,
                OperationContext.UserId, now);
            _quotes.Add(quote);
            _quotes.AddHistory(new QuoteStatusHistory
            {
                Quote = quote,
                StatusId = (int)QuoteState.Draft,
                CreatedByUserId = OperationContext.UserId,
                CreationDate = now,
            });
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            cancellationToken.ThrowIfCancellationRequested();
            AddAudit("quote", quote.Id, "created", now);
            await UnitOfWork.CommitChangesAsync();
            return quote.Id;
        });
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<QuoteDto> UpdateAsync(
        int id,
        QuoteUpdateRequest request,
        string expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        SalesRequestValidator.Validate(request);
        SalesRequestValidator.ValidateVersion(expectedVersion);
        await ExecuteMutationAsync(async () =>
        {
            Quote quote = await _quotes.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound("quote");
            EnsureVersion(quote.RowVersion, expectedVersion);
            var (customer, address, products) = await ResolveReferencesAsync(
                request.CustomerId, request.ShippingAddressId, request.Lines, cancellationToken);
            IReadOnlyList<QuoteLine> lines = quote.PrepareReplacement(request, customer, address, products);
            DateTimeOffset now = TimeProvider.GetUtcNow();
            quote.ApplyReplacement(request, customer, address, OperationContext.UserId, now);
            _quotes.ReplaceLines(quote, lines);
            AddAudit("quote", quote.Id, "updated", now, metadataJson: "{\"changedFields\":[\"header\",\"lines\"]}");
            await UnitOfWork.CommitChangesAsync();
            return quote.Id;
        });
        return await GetByIdAsync(id, cancellationToken);
    }

    public Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        SalesRequestValidator.ValidateVersion(expectedVersion);
        return DeleteCoreAsync(id, expectedVersion, cancellationToken);
    }

    public async Task<QuoteDto> TransitionAsync(
        int id,
        QuoteStatusTransitionRequest request,
        string expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        SalesRequestValidator.ValidateVersion(expectedVersion);
        if (!Enum.IsDefined(request.Status))
            throw InvalidReference("status", "Status is invalid.");
        await ExecuteMutationAsync(async () =>
        {
            Quote quote = await _quotes.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound("quote");
            EnsureVersion(quote.RowVersion, expectedVersion);
            DateTimeOffset now = TimeProvider.GetUtcNow();
            QuoteState previous = quote.TransitionTo(request.Status, OperationContext.UserId, now);
            _quotes.AddHistory(new QuoteStatusHistory
            {
                QuoteId = quote.Id,
                StatusId = (int)request.Status,
                CreatedByUserId = OperationContext.UserId,
                CreationDate = now,
            });
            AddAudit("quote", quote.Id, "statusTransition", now, Code(previous), Code(request.Status));
            await UnitOfWork.CommitChangesAsync();
            return quote.Id;
        });
        return await GetByIdAsync(id, cancellationToken);
    }

    #endregion

    #region Private Methods

    private async Task DeleteCoreAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        await ExecuteMutationAsync(async () =>
        {
            Quote quote = await _quotes.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound("quote");
            EnsureVersion(quote.RowVersion, expectedVersion);
            DateTimeOffset now = TimeProvider.GetUtcNow();
            quote.Tombstone(OperationContext.UserId, now);
            AddAudit("quote", quote.Id, "deleted", now);
            await UnitOfWork.CommitChangesAsync();
            return quote.Id;
        });
    }

    private async Task<(CustomerSalesReference Customer, AddressSalesReference Address,
        IReadOnlyDictionary<int, ProductSalesReference> Products)> ResolveReferencesAsync(
        int customerId,
        int addressId,
        IReadOnlyList<SalesLineRequest>? lines,
        CancellationToken cancellationToken)
    {
        if (customerId <= 0)
            throw InvalidReference("customerId", "Customer ID must be positive.");
        if (addressId <= 0)
            throw InvalidReference("shippingAddressId", "Shipping address ID must be positive.");
        CustomerSalesReference customer = await _references.GetCustomerAsync(customerId, cancellationToken)
            ?? throw InvalidReference("customerId", "The selected customer was not found.");
        AddressSalesReference address = await _references.GetAddressAsync(addressId, cancellationToken)
            ?? throw InvalidReference("shippingAddressId", "The selected shipping address was not found.");
        IReadOnlyDictionary<int, ProductSalesReference> products = await _references.GetProductsAsync(
            lines?.Where(line => line.ProductId > 0).Select(line => line.ProductId).Distinct().ToArray() ?? [],
            cancellationToken);
        return (customer, address, products);
    }

    private static string Code(QuoteState status) => status.ToString().ToLowerInvariant();

    #endregion
}
