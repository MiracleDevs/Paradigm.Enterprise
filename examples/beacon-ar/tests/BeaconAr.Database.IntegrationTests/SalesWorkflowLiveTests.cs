using BeaconAr.Data.MasterData;
using BeaconAr.Data.Operations;
using BeaconAr.Data.Receivables;
using BeaconAr.Data.Reporting;
using BeaconAr.Data.Sales;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Reporting.Contracts;
using BeaconAr.Domain.Reporting.Repositories;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;
using BeaconAr.Providers.Reporting;
using BeaconAr.Providers.Sales;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Extensions;
using Paradigm.Enterprise.Domain.Uow;
using System.Data;
using OrderState = BeaconAr.Domain.Sales.SalesOrderStatus;
using QuoteState = BeaconAr.Domain.Sales.QuoteStatus;

namespace BeaconAr.Database.IntegrationTests;

[TestClass]
[TestCategory("Integration")]
public sealed class SalesWorkflowLiveTests
{
    #region Nested Types

    private sealed class TestOperationContext(int userId, string correlationId) : IApplicationOperationContext
    {
        public int UserId { get; } = userId;
        public string CorrelationId { get; } = correlationId;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => FixedTime;
    }

    private sealed record Fixture(int UserId, int CustomerId, int AddressId, int ProductId, int CarrierId);

    private sealed class CancelAfterFirstSaveUnitOfWork(UnitOfWork inner, CancellationTokenSource cancellation) : IUnitOfWork
    {
        private int _saveCalls;

        public bool HasActiveTransaction => inner.HasActiveTransaction;

        public async Task CommitChangesAsync()
        {
            await inner.CommitChangesAsync();
            if (Interlocked.Increment(ref _saveCalls) == 1)
                cancellation.Cancel();
        }

        public ITransaction CreateTransaction() => inner.CreateTransaction();

        public void RegisterCommiteable(ICommiteable commiteable) => inner.RegisterCommiteable(commiteable);

        public ITransaction UseCurrentTransaction() => inner.UseCurrentTransaction();

        public void UseTransaction(ITransaction transaction) => inner.UseTransaction(transaction);

        public void UseTransaction(IDbCommand command) => inner.UseTransaction(command);

        public void Dispose() => inner.Dispose();
    }

    private enum ReferenceKind
    {
        Customer,
        Address,
        Products,
        Carrier,
    }

    private sealed class ReferenceReadGate(ReferenceKind kind)
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ReferenceKind Kind { get; } = kind;
        public Task Entered => _entered.Task;

        public async Task PauseAsync(ReferenceKind candidate)
        {
            if (candidate != Kind)
                return;
            _entered.TrySetResult();
            await _release.Task;
        }

        public void Release() => _release.TrySetResult();
    }

    private sealed class CoordinatedSalesReferenceRepository(
        SalesReferenceRepository inner,
        ReferenceReadGate gate) : ISalesReferenceRepository
    {
        public async Task<CustomerSalesReference?> GetCustomerAsync(int id, CancellationToken cancellationToken)
        {
            CustomerSalesReference? result = await inner.GetCustomerAsync(id, cancellationToken);
            await gate.PauseAsync(ReferenceKind.Customer);
            return result;
        }

        public async Task<AddressSalesReference?> GetAddressAsync(int id, CancellationToken cancellationToken)
        {
            AddressSalesReference? result = await inner.GetAddressAsync(id, cancellationToken);
            await gate.PauseAsync(ReferenceKind.Address);
            return result;
        }

        public async Task<IReadOnlyDictionary<int, ProductSalesReference>> GetProductsAsync(
            IReadOnlyCollection<int> ids,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<int, ProductSalesReference> result = await inner.GetProductsAsync(ids, cancellationToken);
            await gate.PauseAsync(ReferenceKind.Products);
            return result;
        }

        public async Task<CarrierSalesReference?> GetCarrierAsync(int id, CancellationToken cancellationToken)
        {
            CarrierSalesReference? result = await inner.GetCarrierAsync(id, cancellationToken);
            await gate.PauseAsync(ReferenceKind.Carrier);
            return result;
        }

        public void Dispose()
        {
        }
    }

    #endregion

    #region Fields

    private static readonly DateTimeOffset FixedTime = new(2026, 8, 1, 15, 30, 0, TimeSpan.Zero);

    #endregion

    #region Public Methods

    [TestMethod]
    public async Task QuoteConversionAndFulfillmentPersistOneAtomicWorkflow()
    {
        string connectionString = GetConnectionString();
        Fixture fixture = await SeedFixtureAsync(connectionString);
        using ServiceProvider services = CreateServices(connectionString, fixture.UserId, $"sales-{Guid.NewGuid():N}");
        using IServiceScope scope = services.CreateScope();
        IQuoteProvider quotes = scope.ServiceProvider.GetRequiredService<IQuoteProvider>();
        IQuoteConversionProvider conversion = scope.ServiceProvider.GetRequiredService<IQuoteConversionProvider>();
        ISalesOrderProvider orders = scope.ServiceProvider.GetRequiredService<ISalesOrderProvider>();

        QuoteDto quote = await quotes.CreateAsync(new QuoteCreateRequest(
            fixture.CustomerId, fixture.AddressId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), " live ",
            [new SalesLineRequest(fixture.ProductId, 1, 1.005m, 0)]), CancellationToken.None);
        Assert.IsTrue(quote.CustomerAccountNumber.StartsWith("ACCOUNT-", StringComparison.Ordinal));
        Assert.IsTrue(quote.Lines.Single().Sku.StartsWith("SKU-", StringComparison.Ordinal));
        Assert.AreEqual(1.01m, quote.GrandTotal);
        Assert.IsTrue(quote.QuoteNumber.StartsWith("Q-", StringComparison.Ordinal));

        quote = await quotes.TransitionAsync(quote.Id, new(QuoteState.Sent), quote.Version, CancellationToken.None);
        quote = await quotes.TransitionAsync(quote.Id, new(QuoteState.Accepted), quote.Version, CancellationToken.None);
        QuoteConversionResult first = await conversion.ConvertAsync(quote.Id, CancellationToken.None);
        QuoteConversionResult replay = await conversion.ConvertAsync(quote.Id, CancellationToken.None);
        Assert.IsTrue(first.Created);
        Assert.IsFalse(replay.Created);
        Assert.AreEqual(first.SalesOrder.Id, replay.SalesOrder.Id);
        Assert.AreEqual(quote.CustomerName, first.SalesOrder.CustomerName);
        Assert.AreEqual(quote.Lines.Single().Sku, first.SalesOrder.Lines.Single().Sku);

        SalesOrderDto order = first.SalesOrder;
        order = await orders.TransitionAsync(order.Id, new(OrderState.Confirmed), order.Version, CancellationToken.None);
        order = await orders.TransitionAsync(order.Id, new(OrderState.Processing), order.Version, CancellationToken.None);
        order = await orders.TransitionAsync(order.Id, new(OrderState.Shipped, fixture.CarrierId, " TRACK-1 "), order.Version, CancellationToken.None);
        order = await orders.TransitionAsync(order.Id, new(OrderState.Completed), order.Version, CancellationToken.None);
        Assert.AreEqual(OrderState.Completed, order.Status);
        Assert.AreEqual("TRACK-1", order.TrackingNumber);

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new("""
            SELECT
              (SELECT COUNT(1) FROM [dbo].[QuoteStatusHistory] WHERE [QuoteId] = @QuoteId),
              (SELECT COUNT(1) FROM [dbo].[SalesOrderStatusHistory] WHERE [SalesOrderId] = @OrderId),
              (SELECT COUNT(1) FROM [dbo].[SalesOrder] WHERE [SourceQuoteId] = @QuoteId),
              (SELECT COUNT(1) FROM [dbo].[AuditLog] WHERE [ResourceType] = N'quote' AND [ResourceId] = CONVERT(NVARCHAR(50), @QuoteId)),
              (SELECT COUNT(1) FROM [dbo].[AuditLog] WHERE [ResourceType] = N'salesOrder' AND [ResourceId] = CONVERT(NVARCHAR(50), @OrderId)),
              (SELECT COUNT(1) FROM [dbo].[ApplicationUserView] WHERE [Id] = @UserId),
              (SELECT COUNT(1) FROM [dbo].[ProductView] WHERE [Id] = @ProductId),
              (SELECT COUNT(1) FROM [dbo].[CustomerView] WHERE [Id] = @CustomerId),
              (SELECT COUNT(1) FROM [dbo].[CustomerAddressView] WHERE [Id] = @AddressId AND [CustomerId] = @CustomerId),
              (SELECT COUNT(1) FROM [dbo].[CarrierView] WHERE [Id] = @CarrierId),
              (SELECT COUNT(1) FROM [dbo].[QuoteView] WHERE [Id] = @QuoteId AND [SalesOrderId] = @OrderId),
              (SELECT COUNT(1) FROM [dbo].[QuoteLineView] WHERE [QuoteId] = @QuoteId AND [ProductId] = @ProductId),
              (SELECT COUNT(1) FROM [dbo].[SalesOrderView] WHERE [Id] = @OrderId AND [SourceQuoteId] = @QuoteId),
              (SELECT COUNT(1) FROM [dbo].[SalesOrderLineView] WHERE [SalesOrderId] = @OrderId AND [ProductId] = @ProductId),
              (SELECT [GrandTotal] FROM [dbo].[QuoteView] WHERE [Id] = @QuoteId),
              (SELECT [GrandTotal] FROM [dbo].[SalesOrderView] WHERE [Id] = @OrderId);
            """, connection);
        command.Parameters.AddWithValue("@QuoteId", quote.Id);
        command.Parameters.AddWithValue("@OrderId", order.Id);
        command.Parameters.AddWithValue("@UserId", fixture.UserId);
        command.Parameters.AddWithValue("@ProductId", fixture.ProductId);
        command.Parameters.AddWithValue("@CustomerId", fixture.CustomerId);
        command.Parameters.AddWithValue("@AddressId", fixture.AddressId);
        command.Parameters.AddWithValue("@CarrierId", fixture.CarrierId);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(3, reader.GetInt32(0));
        Assert.AreEqual(5, reader.GetInt32(1));
        Assert.AreEqual(1, reader.GetInt32(2));
        Assert.AreEqual(4, reader.GetInt32(3));
        Assert.AreEqual(5, reader.GetInt32(4));
        for (int ordinal = 5; ordinal <= 13; ordinal++)
            Assert.AreEqual(1, reader.GetInt32(ordinal));
        Assert.AreEqual(1.01m, reader.GetDecimal(14));
        Assert.AreEqual(1.01m, reader.GetDecimal(15));
    }

    [TestMethod]
    public async Task StaleDraftReplacementCannotOverwriteNewerLines()
    {
        string connectionString = GetConnectionString();
        Fixture fixture = await SeedFixtureAsync(connectionString);
        using ServiceProvider services = CreateServices(connectionString, fixture.UserId, $"race-{Guid.NewGuid():N}");
        using IServiceScope scope = services.CreateScope();
        IQuoteProvider quotes = scope.ServiceProvider.GetRequiredService<IQuoteProvider>();
        QuoteDto created = await quotes.CreateAsync(new QuoteCreateRequest(
            fixture.CustomerId, fixture.AddressId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2), null,
            [new SalesLineRequest(fixture.ProductId, 1, 10m, 0)]), CancellationToken.None);
        string stale = created.Version;
        QuoteUpdateRequest winner = new(fixture.CustomerId, fixture.AddressId, created.QuoteDate, created.ValidUntil,
            "winner", [new SalesLineRequest(fixture.ProductId, 2, 10m, 0)]);
        QuoteDto updated = await quotes.UpdateAsync(created.Id, winner, stale, CancellationToken.None);

        SalesException conflict = await Assert.ThrowsAsync<SalesException>(() => quotes.UpdateAsync(created.Id,
            winner with { Notes = "stale", Lines = [new SalesLineRequest(fixture.ProductId, 3, 10m, 0)] },
            stale, CancellationToken.None));

        Assert.AreEqual("concurrency_conflict", conflict.Code);
        QuoteDto reloaded = await quotes.GetByIdAsync(created.Id, CancellationToken.None);
        Assert.AreEqual("winner", reloaded.Notes);
        Assert.AreEqual(2, reloaded.Lines.Single().Quantity);
        Assert.AreEqual(updated.Version, reloaded.Version);
    }

    [TestMethod]
    public async Task ConcurrentQuoteConversionsReturnOneSingletonOrder()
    {
        string connectionString = GetConnectionString();
        Fixture fixture = await SeedFixtureAsync(connectionString);
        QuoteDto accepted;
        using (ServiceProvider setupServices = CreateServices(connectionString, fixture.UserId, $"setup-{Guid.NewGuid():N}"))
        using (IServiceScope setupScope = setupServices.CreateScope())
        {
            IQuoteProvider quotes = setupScope.ServiceProvider.GetRequiredService<IQuoteProvider>();
            accepted = await quotes.CreateAsync(new QuoteCreateRequest(
                fixture.CustomerId, fixture.AddressId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2), null,
                [new SalesLineRequest(fixture.ProductId, 1, 10m, 0)]), CancellationToken.None);
            accepted = await quotes.TransitionAsync(accepted.Id, new(QuoteState.Sent), accepted.Version, CancellationToken.None);
            accepted = await quotes.TransitionAsync(accepted.Id, new(QuoteState.Accepted), accepted.Version, CancellationToken.None);
        }

        using ServiceProvider firstServices = CreateServices(connectionString, fixture.UserId, $"convert-a-{Guid.NewGuid():N}");
        using ServiceProvider secondServices = CreateServices(connectionString, fixture.UserId, $"convert-b-{Guid.NewGuid():N}");
        using IServiceScope firstScope = firstServices.CreateScope();
        using IServiceScope secondScope = secondServices.CreateScope();
        IQuoteConversionProvider firstProvider = firstScope.ServiceProvider.GetRequiredService<IQuoteConversionProvider>();
        IQuoteConversionProvider secondProvider = secondScope.ServiceProvider.GetRequiredService<IQuoteConversionProvider>();
        TaskCompletionSource start = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<QuoteConversionResult> firstTask = Task.Run(async () =>
        {
            await start.Task;
            return await firstProvider.ConvertAsync(accepted.Id, CancellationToken.None);
        });
        Task<QuoteConversionResult> secondTask = Task.Run(async () =>
        {
            await start.Task;
            return await secondProvider.ConvertAsync(accepted.Id, CancellationToken.None);
        });
        start.SetResult();
        QuoteConversionResult[] results = await Task.WhenAll(firstTask, secondTask);

        Assert.AreEqual(results[0].SalesOrder.Id, results[1].SalesOrder.Id);
        Assert.AreEqual(1, results.Count(result => result.Created));
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new("""
            SELECT COUNT(1),
                   (SELECT COUNT(1) FROM [dbo].[SalesOrderStatusHistory] AS [H]
                    INNER JOIN [dbo].[SalesOrder] AS [O] ON [O].[Id] = [H].[SalesOrderId]
                    WHERE [O].[SourceQuoteId] = @QuoteId)
            FROM [dbo].[SalesOrder]
            WHERE [SourceQuoteId] = @QuoteId;
            """, connection);
        command.Parameters.AddWithValue("@QuoteId", accepted.Id);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(1, reader.GetInt32(0));
        Assert.AreEqual(1, reader.GetInt32(1));
    }

    [TestMethod]
    public async Task SearchesTombstonesAndDashboardAreDatabaseBounded()
    {
        string connectionString = GetConnectionString();
        Fixture fixture = await SeedFixtureAsync(connectionString);
        using ServiceProvider services = CreateServices(connectionString, fixture.UserId, $"search-{Guid.NewGuid():N}");
        using IServiceScope scope = services.CreateScope();
        IQuoteProvider quotes = scope.ServiceProvider.GetRequiredService<IQuoteProvider>();
        ISalesOrderProvider orders = scope.ServiceProvider.GetRequiredService<ISalesOrderProvider>();
        IDashboardProvider dashboard = scope.ServiceProvider.GetRequiredService<IDashboardProvider>();

        QuoteDto quote = await quotes.CreateAsync(new QuoteCreateRequest(
            fixture.CustomerId, fixture.AddressId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2), null,
            [new SalesLineRequest(fixture.ProductId, 1, 10m, 0)]), CancellationToken.None);
        PageResult<QuoteSummaryDto> page = await quotes.SearchAsync(new QuoteSearchRequest(
            Search: quote.QuoteNumber, PageNumber: 1, PageSize: 10, SortField: "quoteNumber"), CancellationToken.None);
        Assert.AreEqual(1, page.ItemsCount);
        Assert.AreEqual(quote.Id, page.Items.Single().Id);

        await quotes.DeleteAsync(quote.Id, quote.Version, CancellationToken.None);
        page = await quotes.SearchAsync(new QuoteSearchRequest(Search: quote.QuoteNumber), CancellationToken.None);
        Assert.AreEqual(0, page.ItemsCount);
        await Assert.ThrowsAsync<SalesException>(() => quotes.GetByIdAsync(quote.Id, CancellationToken.None));

        SalesOrderDto order = await orders.CreateDirectAsync(new SalesOrderCreateRequest(
            fixture.CustomerId, fixture.AddressId, null, fixture.CarrierId, null,
            [new SalesLineRequest(fixture.ProductId, 1, 10m, 0)]), CancellationToken.None);
        PageResult<SalesOrderSummaryDto> orderPage = await orders.SearchAsync(new SalesOrderSearchRequest(
            Search: order.OrderNumber, SortField: "orderNumber"), CancellationToken.None);
        Assert.AreEqual(order.Id, orderPage.Items.Single().Id);
        DashboardSummaryDto summary = await dashboard.GetSummaryAsync(CancellationToken.None);
        Assert.IsTrue(summary.Products > 0);
        Assert.IsTrue(summary.Customers > 0);
        Assert.IsTrue(summary.Carriers > 0);
        Assert.IsTrue(summary.ActiveOrders > 0);
        Assert.AreEqual(TimeSpan.Zero, summary.AsOf.Offset);
    }

    [TestMethod]
    public async Task AggregateOverflowAndInterruptedCreatesCommitNoPartialRows()
    {
        string connectionString = GetConnectionString();
        Fixture fixture = await SeedFixtureAsync(connectionString);
        int secondProductId = await InsertProductAsync(connectionString, fixture.UserId);
        string overflowCorrelation = $"overflow-{Guid.NewGuid():N}";
        using (ServiceProvider services = CreateServices(connectionString, fixture.UserId, overflowCorrelation))
        using (IServiceScope scope = services.CreateScope())
        {
            IReadOnlyList<SalesLineRequest> lines =
            [
                new(fixture.ProductId, 99, 999_999_999_999_999.9999m, 0),
                new(secondProductId, 99, 999_999_999_999_999.9999m, 0),
            ];
            SalesValidationException quoteError = await Assert.ThrowsAsync<SalesValidationException>(() =>
                scope.ServiceProvider.GetRequiredService<IQuoteProvider>().CreateAsync(new QuoteCreateRequest(
                    fixture.CustomerId, fixture.AddressId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2), null, lines),
                    CancellationToken.None));
            SalesValidationException orderError = await Assert.ThrowsAsync<SalesValidationException>(() =>
                scope.ServiceProvider.GetRequiredService<ISalesOrderProvider>().CreateDirectAsync(new SalesOrderCreateRequest(
                    fixture.CustomerId, fixture.AddressId, null, null, null, lines), CancellationToken.None));
            Assert.IsTrue(quoteError.Errors.ContainsKey("lines"));
            Assert.IsTrue(orderError.Errors.ContainsKey("lines"));
        }
        Assert.AreEqual(0, await CountAsync(connectionString,
            "SELECT COUNT(1) FROM [dbo].[Quote] WHERE [CustomerId] = @Id", fixture.CustomerId));
        Assert.AreEqual(0, await CountAsync(connectionString,
            "SELECT COUNT(1) FROM [dbo].[SalesOrder] WHERE [CustomerId] = @Id", fixture.CustomerId));
        Assert.AreEqual(0, await CountCorrelationAuditsAsync(connectionString, overflowCorrelation));

        using var cancellation = new CancellationTokenSource();
        string rollbackCorrelation = $"sales-rollback-{Guid.NewGuid():N}";
        using (ServiceProvider services = CreateServices(
            connectionString, fixture.UserId, rollbackCorrelation, cancellation))
        using (IServiceScope scope = services.CreateScope())
        {
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                scope.ServiceProvider.GetRequiredService<IQuoteProvider>().CreateAsync(new QuoteCreateRequest(
                    fixture.CustomerId, fixture.AddressId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2), null,
                    [new SalesLineRequest(fixture.ProductId, 1, 10m, 0)]), cancellation.Token));
        }
        Assert.AreEqual(0, await CountAsync(connectionString,
            "SELECT COUNT(1) FROM [dbo].[Quote] WHERE [CustomerId] = @Id", fixture.CustomerId));
        Assert.AreEqual(0, await CountCorrelationAuditsAsync(connectionString, rollbackCorrelation));
    }

    [TestMethod]
    public async Task DraftReplacementAndConversionPreserveAllCapturedSnapshots()
    {
        string connectionString = GetConnectionString();
        Fixture fixture = await SeedFixtureAsync(connectionString);
        using ServiceProvider services = CreateServices(connectionString, fixture.UserId, $"snapshots-{Guid.NewGuid():N}");
        using IServiceScope scope = services.CreateScope();
        IQuoteProvider quotes = scope.ServiceProvider.GetRequiredService<IQuoteProvider>();
        ISalesOrderProvider orders = scope.ServiceProvider.GetRequiredService<ISalesOrderProvider>();
        IQuoteConversionProvider conversion = scope.ServiceProvider.GetRequiredService<IQuoteConversionProvider>();
        QuoteDto quote = await quotes.CreateAsync(CreateQuoteRequest(fixture), CancellationToken.None);
        SalesOrderDto direct = await orders.CreateDirectAsync(CreateOrderRequest(fixture), CancellationToken.None);
        string capturedSku = quote.Lines.Single().Sku;
        string capturedProduct = quote.Lines.Single().ProductName;
        string capturedCustomer = quote.CustomerName;
        string capturedAddress = quote.ShippingLine1;

        await RenameFixtureAsync(connectionString, fixture);
        quote = await quotes.UpdateAsync(quote.Id, new QuoteUpdateRequest(
            fixture.CustomerId, fixture.AddressId, quote.QuoteDate, quote.ValidUntil, "header only",
            [new SalesLineRequest(fixture.ProductId, 2, 10m, 0)]), quote.Version, CancellationToken.None);
        direct = await orders.UpdateAsync(direct.Id, new SalesOrderUpdateRequest(
            fixture.CustomerId, fixture.AddressId, null, fixture.CarrierId, null,
            [new SalesLineRequest(fixture.ProductId, 2, 10m, 0)]), direct.Version, CancellationToken.None);

        Assert.AreEqual(capturedSku, quote.Lines.Single().Sku);
        Assert.AreEqual(capturedProduct, quote.Lines.Single().ProductName);
        Assert.AreEqual(capturedSku, direct.Lines.Single().Sku);
        Assert.AreEqual(capturedProduct, direct.Lines.Single().ProductName);
        Assert.AreEqual(capturedCustomer, quote.CustomerName);
        Assert.AreEqual(capturedAddress, quote.ShippingLine1);
        quote = await quotes.TransitionAsync(quote.Id, new(QuoteState.Sent), quote.Version, CancellationToken.None);
        quote = await quotes.TransitionAsync(quote.Id, new(QuoteState.Accepted), quote.Version, CancellationToken.None);
        SalesOrderDto converted = (await conversion.ConvertAsync(quote.Id, CancellationToken.None)).SalesOrder;
        Assert.AreEqual(capturedSku, converted.Lines.Single().Sku);
        Assert.AreEqual(capturedCustomer, converted.CustomerName);
        Assert.AreEqual(capturedAddress, converted.ShippingLine1);
    }

    [TestMethod]
    public async Task EveryTerminalLifecycleBranchRejectsFurtherMutationWithoutNewFacts()
    {
        string connectionString = GetConnectionString();
        Fixture fixture = await SeedFixtureAsync(connectionString);
        using ServiceProvider services = CreateServices(connectionString, fixture.UserId, $"lifecycle-{Guid.NewGuid():N}");
        using IServiceScope scope = services.CreateScope();
        IQuoteProvider quotes = scope.ServiceProvider.GetRequiredService<IQuoteProvider>();
        ISalesOrderProvider orders = scope.ServiceProvider.GetRequiredService<ISalesOrderProvider>();

        foreach (QuoteState terminal in new[] { QuoteState.Rejected, QuoteState.Expired })
        {
            QuoteDto quote = await quotes.CreateAsync(CreateQuoteRequest(fixture), CancellationToken.None);
            SalesException skipped = await Assert.ThrowsAsync<SalesException>(() =>
                quotes.TransitionAsync(quote.Id, new(terminal), quote.Version, CancellationToken.None));
            Assert.AreEqual("invalid_quote_transition", skipped.Code);
            quote = await quotes.TransitionAsync(quote.Id, new(QuoteState.Sent), quote.Version, CancellationToken.None);
            quote = await quotes.TransitionAsync(quote.Id, new(terminal), quote.Version, CancellationToken.None);
            string terminalVersion = quote.Version;
            await Assert.ThrowsAsync<SalesException>(() => quotes.TransitionAsync(
                quote.Id, new(terminal), terminalVersion, CancellationToken.None));
            await Assert.ThrowsAsync<SalesException>(() => quotes.DeleteAsync(quote.Id, terminalVersion, CancellationToken.None));
            QuoteDto unchanged = await quotes.GetByIdAsync(quote.Id, CancellationToken.None);
            Assert.AreEqual(terminal, unchanged.Status);
            Assert.AreEqual(terminalVersion, unchanged.Version);
            Assert.AreEqual(3, await CountHistoryAsync(connectionString, "QuoteStatusHistory", "QuoteId", quote.Id));
            Assert.AreEqual(3, await CountResourceAuditsAsync(connectionString, "quote", quote.Id));
        }

        foreach (int stepsBeforeCancellation in new[] { 0, 1, 2 })
        {
            SalesOrderDto order = await orders.CreateDirectAsync(CreateOrderRequest(fixture), CancellationToken.None);
            if (stepsBeforeCancellation >= 1)
                order = await orders.TransitionAsync(order.Id, new(OrderState.Confirmed), order.Version, CancellationToken.None);
            if (stepsBeforeCancellation >= 2)
                order = await orders.TransitionAsync(order.Id, new(OrderState.Processing), order.Version, CancellationToken.None);
            if (stepsBeforeCancellation == 0)
                await Assert.ThrowsAsync<SalesException>(() => orders.TransitionAsync(
                    order.Id, new(OrderState.Processing), order.Version, CancellationToken.None));
            order = await orders.TransitionAsync(order.Id, new(OrderState.Cancelled), order.Version, CancellationToken.None);
            string terminalVersion = order.Version;
            await Assert.ThrowsAsync<SalesException>(() => orders.TransitionAsync(
                order.Id, new(OrderState.Cancelled), terminalVersion, CancellationToken.None));
            await Assert.ThrowsAsync<SalesException>(() => orders.DeleteAsync(order.Id, terminalVersion, CancellationToken.None));
            SalesOrderDto unchanged = await orders.GetByIdAsync(order.Id, CancellationToken.None);
            Assert.AreEqual(OrderState.Cancelled, unchanged.Status);
            Assert.AreEqual(terminalVersion, unchanged.Version);
            Assert.AreEqual(stepsBeforeCancellation + 2,
                await CountHistoryAsync(connectionString, "SalesOrderStatusHistory", "SalesOrderId", order.Id));
            Assert.AreEqual(stepsBeforeCancellation + 2,
                await CountResourceAuditsAsync(connectionString, "salesOrder", order.Id));
        }
    }

    [TestMethod]
    public async Task EligibilityLocksSerializeCustomerProductAndCarrierDeactivation()
    {
        string connectionString = GetConnectionString();
        await AssertCreatePrecedesDeactivationAsync(connectionString, ReferenceKind.Customer);
        await AssertCreatePrecedesDeactivationAsync(connectionString, ReferenceKind.Address);
        await AssertCreatePrecedesDeactivationAsync(connectionString, ReferenceKind.Products);

        Fixture fixture = await SeedFixtureAsync(connectionString);
        SalesOrderDto processing;
        using (ServiceProvider setupServices = CreateServices(connectionString, fixture.UserId, $"carrier-setup-{Guid.NewGuid():N}"))
        using (IServiceScope setupScope = setupServices.CreateScope())
        {
            ISalesOrderProvider orders = setupScope.ServiceProvider.GetRequiredService<ISalesOrderProvider>();
            processing = await orders.CreateDirectAsync(CreateOrderRequest(fixture), CancellationToken.None);
            processing = await orders.TransitionAsync(processing.Id, new(OrderState.Confirmed), processing.Version, CancellationToken.None);
            processing = await orders.TransitionAsync(processing.Id, new(OrderState.Processing), processing.Version, CancellationToken.None);
        }

        ReferenceReadGate carrierGate = new(ReferenceKind.Carrier);
        using ServiceProvider shippingServices = CreateServices(
            connectionString, fixture.UserId, $"carrier-race-{Guid.NewGuid():N}", referenceGate: carrierGate);
        using IServiceScope shippingScope = shippingServices.CreateScope();
        Task<SalesOrderDto> shipping = shippingScope.ServiceProvider.GetRequiredService<ISalesOrderProvider>()
            .TransitionAsync(processing.Id, new(OrderState.Shipped, fixture.CarrierId, "TRACK-RACE"),
                processing.Version, CancellationToken.None);
        await carrierGate.Entered.WaitAsync(TimeSpan.FromSeconds(10));
        Task deactivate = SetInactiveAsync(connectionString, "Carrier", fixture.CarrierId);
        Assert.AreNotEqual(deactivate, await Task.WhenAny(deactivate, Task.Delay(200)));
        carrierGate.Release();
        SalesOrderDto shipped = await shipping.WaitAsync(TimeSpan.FromSeconds(10));
        await deactivate.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual(OrderState.Shipped, shipped.Status);
        Assert.AreEqual(0, await CountAsync(connectionString,
            "SELECT COUNT(1) FROM [dbo].[Carrier] WHERE [Id] = @Id AND [IsActive] = 1", fixture.CarrierId));
    }

    #endregion

    #region Private Methods

    private static QuoteCreateRequest CreateQuoteRequest(Fixture fixture) => new(
        fixture.CustomerId, fixture.AddressId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), null,
        [new SalesLineRequest(fixture.ProductId, 1, 10m, 0)]);

    private static async Task AssertCreatePrecedesDeactivationAsync(
        string connectionString,
        ReferenceKind referenceKind)
    {
        Fixture fixture = await SeedFixtureAsync(connectionString);
        ReferenceReadGate gate = new(referenceKind);
        using ServiceProvider services = CreateServices(
            connectionString, fixture.UserId, $"reference-race-{Guid.NewGuid():N}", referenceGate: gate);
        using IServiceScope scope = services.CreateScope();
        Task<QuoteDto> create = scope.ServiceProvider.GetRequiredService<IQuoteProvider>()
            .CreateAsync(CreateQuoteRequest(fixture), CancellationToken.None);
        await gate.Entered.WaitAsync(TimeSpan.FromSeconds(10));
        string table = referenceKind switch
        {
            ReferenceKind.Customer => "Customer",
            ReferenceKind.Address => "CustomerAddress",
            _ => "Product",
        };
        int id = referenceKind switch
        {
            ReferenceKind.Customer => fixture.CustomerId,
            ReferenceKind.Address => fixture.AddressId,
            _ => fixture.ProductId,
        };
        Task makeIneligible = referenceKind == ReferenceKind.Address
            ? SetAddressBillingOnlyAsync(connectionString, id)
            : SetInactiveAsync(connectionString, table, id);
        Assert.AreNotEqual(makeIneligible, await Task.WhenAny(makeIneligible, Task.Delay(200)));
        gate.Release();
        QuoteDto quote = await create.WaitAsync(TimeSpan.FromSeconds(10));
        await makeIneligible.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.IsTrue(quote.Id > 0);
        if (referenceKind == ReferenceKind.Address)
            Assert.AreEqual(1, await CountAsync(connectionString,
                "SELECT COUNT(1) FROM [dbo].[CustomerAddress] WHERE [Id] = @Id AND [AddressTypeId] = 1", id));
        else
            Assert.AreEqual(0, await CountAsync(connectionString,
                $"SELECT COUNT(1) FROM [dbo].[{table}] WHERE [Id] = @Id AND [IsActive] = 1", id));
    }

    private static SalesOrderCreateRequest CreateOrderRequest(Fixture fixture) => new(
        fixture.CustomerId, fixture.AddressId, null, fixture.CarrierId, null,
        [new SalesLineRequest(fixture.ProductId, 1, 10m, 0)]);

    private static async Task<int> CountAsync(string connectionString, string sql, int id)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static Task<int> CountHistoryAsync(
        string connectionString,
        string table,
        string foreignKey,
        int id)
    {
        string sql = (table, foreignKey) switch
        {
            ("QuoteStatusHistory", "QuoteId") =>
                "SELECT COUNT(1) FROM [dbo].[QuoteStatusHistory] WHERE [QuoteId] = @Id",
            ("SalesOrderStatusHistory", "SalesOrderId") =>
                "SELECT COUNT(1) FROM [dbo].[SalesOrderStatusHistory] WHERE [SalesOrderId] = @Id",
            _ => throw new ArgumentOutOfRangeException(nameof(table)),
        };
        return CountAsync(connectionString, sql, id);
    }

    private static async Task<int> CountCorrelationAuditsAsync(string connectionString, string correlationId)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new(
            "SELECT COUNT(1) FROM [dbo].[AuditLog] WHERE [CorrelationId] = @CorrelationId", connection);
        command.Parameters.AddWithValue("@CorrelationId", correlationId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<int> CountResourceAuditsAsync(
        string connectionString,
        string resourceType,
        int resourceId)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new("""
            SELECT COUNT(1)
            FROM [dbo].[AuditLog]
            WHERE [ResourceType] = @ResourceType AND [ResourceId] = @ResourceId;
            """, connection);
        command.Parameters.AddWithValue("@ResourceType", resourceType);
        command.Parameters.AddWithValue("@ResourceId", resourceId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<int> InsertProductAsync(string connectionString, int userId)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new("""
            INSERT INTO [dbo].[Product]
                ([Sku], [Name], [Category], [UnitPrice], [StockQuantity], [IsActive], [CreatedByUserId], [CreationDate])
            OUTPUT INSERTED.[Id]
            VALUES (@Sku, N'Second product', N'Test', 10, 100, 1, @UserId, SYSDATETIMEOFFSET());
            """, connection);
        command.Parameters.AddWithValue("@Sku", $"SKU-{Guid.NewGuid():N}"[..32]);
        command.Parameters.AddWithValue("@UserId", userId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task RenameFixtureAsync(string connectionString, Fixture fixture)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new("""
            UPDATE [dbo].[Product] SET [Sku] = @Sku, [Name] = N'Renamed product' WHERE [Id] = @ProductId;
            UPDATE [dbo].[Customer] SET [Name] = N'Renamed customer' WHERE [Id] = @CustomerId;
            UPDATE [dbo].[CustomerAddress] SET [Line1] = N'Renamed street' WHERE [Id] = @AddressId;
            """, connection);
        command.Parameters.AddWithValue("@Sku", $"RENAMED-{Guid.NewGuid():N}"[..32]);
        command.Parameters.AddWithValue("@ProductId", fixture.ProductId);
        command.Parameters.AddWithValue("@CustomerId", fixture.CustomerId);
        command.Parameters.AddWithValue("@AddressId", fixture.AddressId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task SetInactiveAsync(string connectionString, string table, int id)
    {
        string sql = table switch
        {
            "Customer" => "UPDATE [dbo].[Customer] SET [IsActive] = 0 WHERE [Id] = @Id",
            "Product" => "UPDATE [dbo].[Product] SET [IsActive] = 0 WHERE [Id] = @Id",
            "Carrier" => "UPDATE [dbo].[Carrier] SET [IsActive] = 0 WHERE [Id] = @Id",
            _ => throw new ArgumentOutOfRangeException(nameof(table)),
        };
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new(sql, connection) { CommandTimeout = 10 };
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task SetAddressBillingOnlyAsync(string connectionString, int id)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new(
            "UPDATE [dbo].[CustomerAddress] SET [AddressTypeId] = 1 WHERE [Id] = @Id", connection)
        {
            CommandTimeout = 10,
        };
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync();
    }

    private static ServiceProvider CreateServices(
        string connectionString,
        int userId,
        string correlationId,
        CancellationTokenSource? cancellationAfterFirstSave = null,
        ReferenceReadGate? referenceGate = null)
    {
        ServiceCollection services = new();
        services.RegisterLoggedUserService();
        if (cancellationAfterFirstSave is null)
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        else
        {
            services.AddScoped<UnitOfWork>();
            services.AddScoped<IUnitOfWork>(provider => new CancelAfterFirstSaveUnitOfWork(
                provider.GetRequiredService<UnitOfWork>(), cancellationAfterFirstSave));
        }
        services.AddDbContext<ReceivablesDbContext>((_, options) => options.UseSqlServer(connectionString));
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IQuoteViewRepository, QuoteViewRepository>();
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<ISalesOrderViewRepository, SalesOrderViewRepository>();
        services.AddScoped<SalesReferenceRepository>();
        if (referenceGate is null)
            services.AddScoped<ISalesReferenceRepository>(provider => provider.GetRequiredService<SalesReferenceRepository>());
        else
        {
            services.AddSingleton(referenceGate);
            services.AddScoped<ISalesReferenceRepository>(provider => new CoordinatedSalesReferenceRepository(
                provider.GetRequiredService<SalesReferenceRepository>(), provider.GetRequiredService<ReferenceReadGate>()));
        }
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISalesPersistenceErrorClassifier, SalesPersistenceErrorClassifier>();
        services.AddScoped<IPersistenceSession, PersistenceSession>();
        services.AddScoped<IApplicationOperationContext>(_ => new TestOperationContext(userId, correlationId));
        services.AddSingleton<TimeProvider, FixedTimeProvider>();
        services.AddScoped<IQuoteProvider, QuoteProvider>();
        services.AddScoped<ISalesOrderProvider, SalesOrderProvider>();
        services.AddScoped<IQuoteConversionProvider, QuoteConversionProvider>();
        services.AddScoped<IDashboardProvider, DashboardProvider>();
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task<Fixture> SeedFixtureAsync(string connectionString)
    {
        string key = Guid.NewGuid().ToString("N");
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new("""
            INSERT INTO [dbo].[ApplicationUser] ([Issuer], [Subject], [DisplayName], [CreationDate])
            VALUES (N'https://sales-tests.example', @Key, N'Sales Test', SYSDATETIMEOFFSET());
            DECLARE @UserId INT = CONVERT(INT, SCOPE_IDENTITY());
            INSERT INTO [dbo].[Customer] ([AccountNumber], [Name], [Email], [CreditLimit], [PaymentTermsDays], [IsActive], [CreatedByUserId], [CreationDate])
            VALUES (CONCAT(N'ACCOUNT-', @Key), N'Customer', CONCAT(@Key, N'@example.test'), 0, 30, 1, @UserId, SYSDATETIMEOFFSET());
            DECLARE @CustomerId INT = CONVERT(INT, SCOPE_IDENTITY());
            INSERT INTO [dbo].[CustomerAddress] ([CustomerId], [AddressTypeId], [Label], [Line1], [City], [PostalCode], [Country], [CreatedByUserId], [CreationDate])
            VALUES (@CustomerId, 2, N'Dock', N'1 Street', N'Buenos Aires', N'1000', N'AR', @UserId, SYSDATETIMEOFFSET());
            DECLARE @AddressId INT = CONVERT(INT, SCOPE_IDENTITY());
            INSERT INTO [dbo].[Product] ([Sku], [Name], [Category], [UnitPrice], [StockQuantity], [IsActive], [CreatedByUserId], [CreationDate])
            VALUES (CONCAT(N'SKU-', LEFT(@Key, 20)), N'Product', N'Test', 10, 100, 1, @UserId, SYSDATETIMEOFFSET());
            DECLARE @ProductId INT = CONVERT(INT, SCOPE_IDENTITY());
            INSERT INTO [dbo].[Carrier] ([Code], [Name], [ServiceLevel], [IsActive], [CreatedByUserId], [CreationDate])
            VALUES (CONCAT(N'C-', LEFT(@Key, 20)), N'Carrier', N'Ground', 1, @UserId, SYSDATETIMEOFFSET());
            DECLARE @CarrierId INT = CONVERT(INT, SCOPE_IDENTITY());
            SELECT @UserId, @CustomerId, @AddressId, @ProductId, @CarrierId;
            """, connection);
        command.Parameters.AddWithValue("@Key", key);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        return new Fixture(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4));
    }

    private static string GetConnectionString()
    {
        string? value = Environment.GetEnvironmentVariable("ConnectionStrings__DatabaseConnection");
        if (string.IsNullOrWhiteSpace(value))
            Assert.Inconclusive("Set ConnectionStrings__DatabaseConnection to run database integration tests.");
        return value;
    }

    #endregion
}
