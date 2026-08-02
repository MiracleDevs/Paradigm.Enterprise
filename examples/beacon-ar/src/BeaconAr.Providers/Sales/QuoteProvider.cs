using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Receivables.Entities;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;
using BeaconAr.Domain.Sales.Validation;
using Paradigm.Enterprise.Domain.Uow;
using QuoteState = BeaconAr.Domain.Sales.QuoteStatus;

namespace BeaconAr.Providers.Sales;

public sealed class QuoteProvider : IQuoteProvider
{
    #region Fields

    private readonly IQuoteRepository _quotes;
    private readonly IQuoteViewRepository _views;
    private readonly ISalesReferenceRepository _references;
    private readonly SalesWorkflowCoordinator _workflow;

    #endregion

    #region Constructors

    public QuoteProvider(
        IQuoteRepository quotes,
        IQuoteViewRepository views,
        ISalesReferenceRepository references,
        SalesWorkflowCoordinator workflow)
    {
        _quotes = quotes;
        _views = views;
        _references = references;
        _workflow = workflow;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<QuoteSummaryDto>> SearchAsync(QuoteSearchRequest request, CancellationToken cancellationToken)
    {
        SalesRequestValidator.Validate(request);
        return _views.SearchAsync(request, cancellationToken);
    }

    public async Task<QuoteDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await _views.GetByIdAsync(id, cancellationToken) ?? throw SalesWorkflowCoordinator.NotFound("quote");

    public async Task<QuoteDto> CreateAsync(QuoteCreateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        SalesRequestValidator.Validate(request);
        int id = await _workflow.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _workflow.UtcNow;
            var (customer, address, products) = await ResolveReferencesAsync(
                request.CustomerId, request.ShippingAddressId, request.Lines, cancellationToken);
            string number = await _quotes.AllocateNumberAsync(cancellationToken);
            Quote quote = Quote.CreateDraft(number, request, customer, address, products,
                _workflow.UserId, now);
            _quotes.Add(quote);
            _quotes.AddHistory(QuoteStatusHistory.Create(quote, _workflow.UserId, now));
            cancellationToken.ThrowIfCancellationRequested();
            await _workflow.UnitOfWork.CommitChangesAsync();
            cancellationToken.ThrowIfCancellationRequested();
            _workflow.AddAudit("quote", quote.Id, "created", now);
            await _workflow.UnitOfWork.CommitChangesAsync();
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
        await _workflow.ExecuteAsync(async () =>
        {
            Quote quote = await _quotes.GetForUpdateAsync(id, cancellationToken) ?? throw SalesWorkflowCoordinator.NotFound("quote");
            SalesWorkflowCoordinator.EnsureVersion(quote.RowVersion, expectedVersion);
            var (customer, address, products) = await ResolveReferencesAsync(
                request.CustomerId, request.ShippingAddressId, request.Lines, cancellationToken);
            IReadOnlyList<QuoteLine> lines = quote.PrepareReplacement(request, customer, address, products);
            DateTimeOffset now = _workflow.UtcNow;
            quote.ApplyReplacement(request, customer, address, _workflow.UserId, now);
            _quotes.ReplaceLines(quote, lines);
            _workflow.AddAudit("quote", quote.Id, "updated", now, metadataJson: "{\"changedFields\":[\"header\",\"lines\"]}");
            await _workflow.UnitOfWork.CommitChangesAsync();
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
            throw SalesWorkflowCoordinator.InvalidReference("status", "Status is invalid.");
        await _workflow.ExecuteAsync(async () =>
        {
            Quote quote = await _quotes.GetForUpdateAsync(id, cancellationToken) ?? throw SalesWorkflowCoordinator.NotFound("quote");
            SalesWorkflowCoordinator.EnsureVersion(quote.RowVersion, expectedVersion);
            DateTimeOffset now = _workflow.UtcNow;
            QuoteState previous = quote.TransitionTo(request.Status, _workflow.UserId, now);
            _quotes.AddHistory(QuoteStatusHistory.Create(quote, _workflow.UserId, now));
            _workflow.AddAudit("quote", quote.Id, "statusTransition", now, Code(previous), Code(request.Status));
            await _workflow.UnitOfWork.CommitChangesAsync();
            return quote.Id;
        });
        return await GetByIdAsync(id, cancellationToken);
    }

    #endregion

    #region Private Methods

    private async Task DeleteCoreAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        await _workflow.ExecuteAsync(async () =>
        {
            Quote quote = await _quotes.GetForUpdateAsync(id, cancellationToken) ?? throw SalesWorkflowCoordinator.NotFound("quote");
            SalesWorkflowCoordinator.EnsureVersion(quote.RowVersion, expectedVersion);
            DateTimeOffset now = _workflow.UtcNow;
            quote.Tombstone(_workflow.UserId, now);
            _workflow.AddAudit("quote", quote.Id, "deleted", now);
            await _workflow.UnitOfWork.CommitChangesAsync();
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
            throw SalesWorkflowCoordinator.InvalidReference("customerId", "Customer ID must be positive.");
        if (addressId <= 0)
            throw SalesWorkflowCoordinator.InvalidReference("shippingAddressId", "Shipping address ID must be positive.");
        CustomerSalesReference customer = await _references.GetCustomerAsync(customerId, cancellationToken)
            ?? throw SalesWorkflowCoordinator.InvalidReference("customerId", "The selected customer was not found.");
        AddressSalesReference address = await _references.GetAddressAsync(addressId, cancellationToken)
            ?? throw SalesWorkflowCoordinator.InvalidReference("shippingAddressId", "The selected shipping address was not found.");
        IReadOnlyDictionary<int, ProductSalesReference> products = await _references.GetProductsAsync(
            lines?.Where(line => line.ProductId > 0).Select(line => line.ProductId).Distinct().ToArray() ?? [],
            cancellationToken);
        return (customer, address, products);
    }

    private static string Code(QuoteState status) => status.ToString().ToLowerInvariant();

    #endregion
}
