using System.Data;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Access.Entities;
using BeaconAr.Domain.MasterData.Entities;
using BeaconAr.Domain.Operations.Entities;
using BeaconAr.Domain.Sales.Entities;
using BeaconAr.Domain.Operations.Contracts;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;
using BeaconAr.Providers.Operations;
using BeaconAr.Providers.Sales;
using Paradigm.Enterprise.Domain.Uow;
using OrderState = BeaconAr.Interfaces.Sales.Enums.SalesOrderStatus;
using QuoteState = BeaconAr.Interfaces.Sales.Enums.QuoteStatus;

namespace BeaconAr.Providers.Tests;

[TestClass]
public sealed class SalesProviderTests
{
    #region Nested Types

    private sealed class FakeQuoteRepository : IQuoteRepository
    {
        public Quote? Current { get; set; }
        public List<QuoteStatusHistory> Histories { get; } = [];
        public int AllocateCalls { get; private set; }

        public Task<string> AllocateNumberAsync(CancellationToken cancellationToken)
        {
            AllocateCalls++;
            return Task.FromResult("Q-00000001");
        }

        public Task<Quote?> GetForUpdateAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Current);
        public Task<Quote?> LockForConversionAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Current);

        public void Add(Quote quote)
        {
            quote.Id = 42;
            quote.RowVersion = Version;
            Current = quote;
        }

        public void ReplaceLines(Quote quote, IReadOnlyCollection<QuoteLine> lines)
        {
            quote.QuoteLines.Clear();
            foreach (QuoteLine line in lines)
                quote.QuoteLines.Add(line);
        }

        public void AddHistory(QuoteStatusHistory history) => Histories.Add(history);
        public void Dispose() { }
    }

    private sealed class FakeQuoteViewRepository(FakeQuoteRepository repository) : IQuoteViewRepository
    {
        public int SearchCalls { get; private set; }

        public Task<QuoteDto?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(repository.Current is null ? null : Map(repository.Current));

        public Task<PageResult<QuoteView>> SearchAsync(QuoteSearchRequest request, CancellationToken cancellationToken)
        {
            SearchCalls++;
            return Task.FromResult(new PageResult<QuoteView>([], request.PageNumber, request.PageSize, 0, 0));
        }

        public void Dispose() { }

        private static QuoteDto Map(Quote quote)
        {
            SalesLineDto[] lines = quote.QuoteLines.Select((line, index) => new SalesLineDto(index + 1,
                line.ProductId, line.SkuSnapshot, line.ProductNameSnapshot, line.Quantity, line.UnitPrice,
                line.DiscountPercent, line.Quantity * line.UnitPrice, 0m, line.Quantity * line.UnitPrice)).ToArray();
            return new QuoteDto(quote.Id, quote.QuoteNumber, quote.CustomerId, quote.ShippingAddressId,
                quote.QuoteDate, quote.ValidUntil, (QuoteState)quote.StatusId, quote.Notes,
                quote.CustomerAccountNumberSnapshot, quote.CustomerNameSnapshot, quote.CustomerEmailSnapshot,
                quote.CustomerPhoneSnapshot, quote.ShippingLabelSnapshot, quote.ShippingLine1Snapshot,
                quote.ShippingLine2Snapshot, quote.ShippingCitySnapshot, quote.ShippingStateSnapshot,
                quote.ShippingPostalCodeSnapshot, quote.ShippingCountrySnapshot, quote.ShippingAddressTypeCodeSnapshot,
                lines, lines.Sum(line => line.LineSubtotal), lines.Sum(line => line.DiscountAmount),
                lines.Sum(line => line.LineTotal), null, quote.CreatedByUserId, quote.CreationDate,
                quote.ModifiedByUserId, quote.ModificationDate, VersionTokenCodec.Encode(quote.RowVersion));
        }
    }

    private sealed class FakeReferences : ISalesReferenceRepository
    {
        public IReadOnlyCollection<int>? RequestedProductIds { get; private set; }
        public bool ProductActive { get; set; } = true;
        public int ReadCalls { get; private set; }

        public Task<CustomerSalesReference?> GetCustomerAsync(int id, CancellationToken cancellationToken)
        {
            ReadCalls++;
            return Task.FromResult<CustomerSalesReference?>(new(id, "ACCOUNT", "Customer", "customer@example.test", null, true));
        }

        public Task<AddressSalesReference?> GetAddressAsync(int id, CancellationToken cancellationToken)
        {
            ReadCalls++;
            return Task.FromResult<AddressSalesReference?>(new(id, 1, "shipping", "Dock", "Street", null, "City", null, "1000", "AR"));
        }

        public Task<IReadOnlyDictionary<int, ProductSalesReference>> GetProductsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
        {
            ReadCalls++;
            RequestedProductIds = ids;
            return Task.FromResult<IReadOnlyDictionary<int, ProductSalesReference>>(
                ids.ToDictionary(id => id, id => new ProductSalesReference(id, $"SKU-{id}", $"Product {id}", ProductActive)));
        }
        public Task<CarrierSalesReference?> GetCarrierAsync(int id, CancellationToken cancellationToken)
        {
            ReadCalls++;
            return Task.FromResult<CarrierSalesReference?>(new(id, "Carrier", true));
        }
        public void Dispose() { }
    }

    private sealed class FakeAuditRepository : IAuditLogRepository
    {
        public List<AuditLog> Entries { get; } = [];
        public void Add(AuditLog auditLog) => Entries.Add(auditLog);
        public void Dispose() { }
    }

    private sealed class FakeTransaction : ITransaction
    {
        public bool IsActive { get; private set; } = true;
        public int Commits { get; private set; }
        public int Rollbacks { get; private set; }
        public void Commit() { Commits++; IsActive = false; }
        public void Rollback() { Rollbacks++; IsActive = false; }
        public void AddCommiteable(ICommiteable commiteable) { }
        public void AddCommand(IDbCommand command) { }
        public void Dispose() { }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool HasActiveTransaction => Transactions.LastOrDefault()?.IsActive == true;
        public List<FakeTransaction> Transactions { get; } = [];
        public int SaveCalls { get; private set; }
        public int? ThrowOnSave { get; set; }
        public Task CommitChangesAsync()
        {
            SaveCalls++;
            if (ThrowOnSave == SaveCalls)
                throw new InvalidOperationException("Injected failure.");
            return Task.CompletedTask;
        }
        public ITransaction CreateTransaction() { FakeTransaction value = new(); Transactions.Add(value); return value; }
        public void RegisterCommiteable(ICommiteable commiteable) { }
        public ITransaction UseCurrentTransaction() => Transactions.Last();
        public void UseTransaction(ITransaction transaction) { }
        public void UseTransaction(IDbCommand command) { }
        public void Dispose() { }
    }

    private sealed class FakeOperationContext : IApplicationOperationContext
    {
        public int UserId => 9;
        public string CorrelationId => "sales-test";
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FakeClassifier : ISalesPersistenceErrorClassifier
    {
        public PersistenceConflictKind Classify(Exception exception) => PersistenceConflictKind.None;
        public bool IsSourceQuoteSingletonConflict(Exception exception) => false;
    }

    private sealed class FakeSession : IPersistenceSession
    {
        public int Discards { get; private set; }
        public void DiscardTrackedChanges() => Discards++;
    }

    private sealed class FakeDashboardRepository : IDashboardRepository
    {
        public int Calls { get; private set; }
        public DashboardSummaryDto Value { get; } = new(1, 2, 3, 4, 5, Now);
        public Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Value);
        }
        public void Dispose() { }
    }

    #endregion

    #region Fields

    private static readonly byte[] Version = [1, 2, 3, 4, 5, 6, 7, 8];
    private static readonly int[] ExpectedProductIds = [1, 2];
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    #endregion

    #region Public Methods

    [TestMethod]
    public async Task QuoteCreateUsesBoundedReferencesAndCommitsHistoryAndAuditAtomically()
    {
        FakeQuoteRepository quotes = new();
        FakeReferences references = new();
        FakeAuditRepository audits = new();
        FakeUnitOfWork unitOfWork = new();
        QuoteProvider provider = CreateQuoteProvider(quotes, references, audits, unitOfWork);

        QuoteDto result = await provider.CreateAsync(Request(), CancellationToken.None);

        Assert.AreEqual(42, result.Id);
        CollectionAssert.AreEqual(ExpectedProductIds, references.RequestedProductIds!.ToArray());
        Assert.HasCount(1, quotes.Histories);
        Assert.AreEqual((int)QuoteState.Draft, quotes.Histories[0].StatusId);
        Assert.HasCount(1, audits.Entries);
        Assert.AreEqual("created", audits.Entries[0].Action);
        Assert.AreEqual(2, unitOfWork.SaveCalls);
        Assert.AreEqual(1, unitOfWork.Transactions.Single().Commits);
    }

    [TestMethod]
    public async Task QuoteStaleWriterRollsBackWithoutAuditOrSave()
    {
        FakeQuoteRepository quotes = new() { Current = ExistingQuote() };
        FakeAuditRepository audits = new();
        FakeUnitOfWork unitOfWork = new();
        FakeSession session = new();
        QuoteProvider provider = CreateQuoteProvider(quotes, new FakeReferences(), audits, unitOfWork, session);

        SalesException error = await Assert.ThrowsAsync<SalesException>(() => provider.UpdateAsync(42,
            new QuoteUpdateRequest(1, 10, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2), null,
                [new SalesLineRequest(1, 1, 10m, 0)]),
            VersionTokenCodec.Encode(new byte[8]), CancellationToken.None));

        Assert.AreEqual("concurrency_conflict", error.Code);
        Assert.AreEqual(0, unitOfWork.SaveCalls);
        Assert.AreEqual(1, unitOfWork.Transactions.Single().Rollbacks);
        Assert.HasCount(0, audits.Entries);
        Assert.AreEqual(1, session.Discards);
    }

    [TestMethod]
    public async Task QuoteTransitionAddsStatusHistoryAndSafeAuditCodes()
    {
        FakeQuoteRepository quotes = new() { Current = ExistingQuote() };
        FakeAuditRepository audits = new();
        FakeUnitOfWork unitOfWork = new();
        QuoteProvider provider = CreateQuoteProvider(quotes, new FakeReferences(), audits, unitOfWork);

        QuoteDto result = await provider.TransitionAsync(42, new(QuoteState.Sent),
            VersionTokenCodec.Encode(Version), CancellationToken.None);

        Assert.AreEqual(QuoteState.Sent, result.Status);
        Assert.HasCount(1, quotes.Histories);
        Assert.AreEqual("draft", audits.Entries.Single().PreviousStatusCode);
        Assert.AreEqual("sent", audits.Entries.Single().NewStatusCode);
        Assert.AreEqual("statusTransition", audits.Entries.Single().Action);
    }

    [TestMethod]
    public async Task PersistenceFailureRollsBackAndRemainsUnexpected()
    {
        FakeUnitOfWork unitOfWork = new() { ThrowOnSave = 1 };
        FakeSession session = new();
        QuoteProvider provider = CreateQuoteProvider(new FakeQuoteRepository(), new FakeReferences(),
            new FakeAuditRepository(), unitOfWork, session);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateAsync(Request(), CancellationToken.None));

        Assert.AreEqual(1, unitOfWork.Transactions.Single().Rollbacks);
        Assert.AreEqual(1, session.Discards);
    }

    [TestMethod]
    public async Task InvalidQuoteSearchDoesNotReachData()
    {
        FakeQuoteRepository quotes = new();
        FakeQuoteViewRepository views = new(quotes);
        QuoteProvider provider = CreateQuoteProvider(quotes, new FakeReferences(), new FakeAuditRepository(),
            new FakeUnitOfWork(), views: views);

        await Assert.ThrowsAsync<SalesValidationException>(() => provider.SearchAsync(
            new QuoteSearchRequest(PageSize: 101), CancellationToken.None));
        Assert.AreEqual(0, views.SearchCalls);
    }

    [TestMethod]
    public async Task InvalidQuoteReferenceInputFailsBeforeCollaboratorCalls()
    {
        var references = new FakeReferences();
        QuoteProvider provider = CreateQuoteProvider(new FakeQuoteRepository(), references,
            new FakeAuditRepository(), new FakeUnitOfWork());
        QuoteCreateRequest request = Request() with { CustomerId = 0 };

        SalesValidationException error = await Assert.ThrowsAsync<SalesValidationException>(() =>
            provider.CreateAsync(request, CancellationToken.None));

        Assert.IsTrue(error.Errors.ContainsKey("customerId"));
        Assert.AreEqual(0, references.ReadCalls);
    }

    [TestMethod]
    public async Task DashboardPassesThroughOneDatabaseSummaryCall()
    {
        FakeDashboardRepository repository = new();
        DashboardProvider provider = new(repository);

        DashboardSummaryDto value = await provider.GetSummaryAsync(CancellationToken.None);

        Assert.AreSame(repository.Value, value);
        Assert.AreEqual(1, repository.Calls);
    }

    #endregion

    #region Private Methods

    private static QuoteProvider CreateQuoteProvider(
        FakeQuoteRepository quotes,
        FakeReferences references,
        FakeAuditRepository audits,
        FakeUnitOfWork unitOfWork,
        FakeSession? session = null,
        FakeQuoteViewRepository? views = null) => new(
            quotes, views ?? new FakeQuoteViewRepository(quotes), references,
            new SalesWorkflowCoordinator(unitOfWork, audits, new FakeOperationContext(), new FakeTimeProvider(),
                new FakeClassifier(), session ?? new FakeSession()));

    private static QuoteCreateRequest Request() => new(
        1, 10, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), null,
        [new SalesLineRequest(1, 1, 10m, 0), new SalesLineRequest(2, 2, 5m, 10)]);

    private static Quote ExistingQuote()
    {
        Quote quote = Quote.CreateDraft("Q-00000001", new QuoteCreateRequest(
                1, 10, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), null,
                [new SalesLineRequest(1, 1, 10m, 0)]),
            new(1, "ACCOUNT", "Customer", "customer@example.test", null, true),
            new(10, 1, "shipping", "Dock", "Street", null, "City", null, "1000", "AR"),
            new Dictionary<int, ProductSalesReference> { [1] = new(1, "SKU-1", "Product 1", true) },
            9, Now);
        quote.Id = 42;
        quote.RowVersion = Version.ToArray();
        return quote;
    }

    #endregion
}
