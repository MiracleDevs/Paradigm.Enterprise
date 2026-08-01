using BeaconAr.Domain.Receivables.Generated;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;
using BeaconAr.Domain.Sales.Validation;
using OrderState = BeaconAr.Domain.Sales.SalesOrderStatus;
using QuoteState = BeaconAr.Domain.Sales.QuoteStatus;

namespace BeaconAr.Domain.Tests;

[TestClass]
public sealed class SalesWorkflowTests
{
    #region Public Methods

    [TestMethod]
    public void QuoteCreationOwnsSnapshotsPricingInputsAndAuditState()
    {
        Quote quote = CreateQuote();

        Assert.AreEqual("Q-00000001", quote.QuoteNumber);
        Assert.AreEqual((int)QuoteState.Draft, quote.StatusId);
        Assert.AreEqual("ACCOUNT", quote.CustomerAccountNumberSnapshot);
        Assert.AreEqual("SKU", quote.QuoteLines.Single().SkuSnapshot);
        Assert.AreEqual(7, quote.CreatedByUserId);
        Assert.AreEqual(DateTimeOffset.UnixEpoch, quote.CreationDate);
    }

    [TestMethod]
    public void QuoteRejectsDateDuplicateAndDecimalScaleWithFieldPaths()
    {
        QuoteCreateRequest request = QuoteRequest() with
        {
            ValidUntil = new DateOnly(2025, 1, 1),
            QuoteDate = new DateOnly(2025, 1, 2),
        };
        SalesValidationException dates = Assert.Throws<SalesValidationException>(() =>
            Quote.CreateDraft("Q-00000001", request, Customer(), Address(), Products(), 1, DateTimeOffset.UnixEpoch));
        Assert.IsTrue(dates.Errors.ContainsKey("validUntil"));

        SalesValidationException lines = Assert.Throws<SalesValidationException>(() =>
            Quote.CreateDraft("Q-00000001", QuoteRequest() with
            {
                Lines = [new(1, 1, 1.12345m, 0), new(1, 1, 1m, 0)],
            }, Customer(), Address(), Products(), 1, DateTimeOffset.UnixEpoch));
        Assert.IsTrue(lines.Errors.ContainsKey("lines.0.unitPrice"));
        Assert.IsTrue(lines.Errors.ContainsKey("lines.1.productId"));
    }

    [TestMethod]
    public void FailedQuoteReplacementDoesNotMutateAggregate()
    {
        Quote quote = CreateQuote();
        QuoteUpdateRequest invalid = new(1, 10, quote.QuoteDate, quote.ValidUntil, "changed",
            [new SalesLineRequest(1, 2, 10m, 0)]);
        Dictionary<int, ProductSalesReference> inactive = new() { [1] = new(1, "NEW", "Changed", false) };

        Assert.Throws<SalesValidationException>(() => quote.PrepareReplacement(invalid, Customer(), Address(), inactive));
        Assert.IsNull(quote.Notes);
        Assert.AreEqual(1, quote.QuoteLines.Single().Quantity);
        Assert.IsNull(quote.ModificationDate);
    }

    [TestMethod]
    public void UnchangedInactiveQuoteLinePreservesHistoricalSnapshot()
    {
        Quote quote = CreateQuote();
        QuoteUpdateRequest request = new(1, 10, quote.QuoteDate, quote.ValidUntil, null,
            [new SalesLineRequest(1, 1, 10m, 0)]);
        Dictionary<int, ProductSalesReference> inactive = new() { [1] = new(1, "NEW", "Changed", false) };

        QuoteLine line = quote.PrepareReplacement(request, Customer(), Address(), inactive).Single();

        Assert.AreEqual("SKU", line.SkuSnapshot);
        Assert.AreEqual("Product", line.ProductNameSnapshot);
    }

    [TestMethod]
    public void RetainedActiveProductPreservesQuoteAndOrderSnapshots()
    {
        Quote quote = CreateQuote();
        SalesOrder order = CreateOrder();
        Dictionary<int, ProductSalesReference> renamed = new() { [1] = new(1, "RENAMED", "Renamed product", true) };
        QuoteUpdateRequest quoteRequest = new(1, 10, quote.QuoteDate, quote.ValidUntil, "notes changed",
            [new SalesLineRequest(1, 2, 10m, 0)]);
        SalesOrderUpdateRequest orderRequest = new(1, 10, null, null, null,
            [new SalesLineRequest(1, 2, 10m, 0)]);

        QuoteLine quoteLine = quote.PrepareReplacement(quoteRequest, Customer(), Address(), renamed).Single();
        SalesOrderLine orderLine = order.PrepareReplacement(orderRequest, Customer(), Address(), renamed, null).Single();

        Assert.AreEqual("SKU", quoteLine.SkuSnapshot);
        Assert.AreEqual("Product", quoteLine.ProductNameSnapshot);
        Assert.AreEqual("SKU", orderLine.SkuSnapshot);
        Assert.AreEqual("Product", orderLine.ProductNameSnapshot);
    }

    [TestMethod]
    public void AggregatePricingOverflowIsRejectedWithTransactionFieldPath()
    {
        const decimal maximumUnitPrice = 999_999_999_999_999.9999m;
        QuoteCreateRequest quoteRequest = QuoteRequest() with
        {
            Lines =
            [
                new SalesLineRequest(1, 99, maximumUnitPrice, 0),
                new SalesLineRequest(2, 99, maximumUnitPrice, 0),
            ],
        };
        Dictionary<int, ProductSalesReference> products = Products();
        products.Add(2, new ProductSalesReference(2, "SKU-2", "Second product", true));

        SalesValidationException quoteError = Assert.Throws<SalesValidationException>(() =>
            Quote.CreateDraft("Q-00000002", quoteRequest, Customer(), Address(), products, 1, DateTimeOffset.UnixEpoch));
        SalesValidationException orderError = Assert.Throws<SalesValidationException>(() =>
            SalesOrder.CreateDirectDraft("SO-00000002", new SalesOrderCreateRequest(1, 10, null, null, null, quoteRequest.Lines),
                Customer(), Address(), products, null, 1, DateTimeOffset.UnixEpoch));

        Assert.IsTrue(quoteError.Errors.ContainsKey("lines"));
        Assert.IsTrue(orderError.Errors.ContainsKey("lines"));
    }

    [TestMethod]
    public void QuoteStateMachineAllowsOnlyPublishedEdges()
    {
        Quote quote = CreateQuote();
        Assert.AreEqual(QuoteState.Draft, quote.TransitionTo(QuoteState.Sent, 2, DateTimeOffset.UnixEpoch.AddMinutes(1)));
        Assert.AreEqual(QuoteState.Sent, quote.TransitionTo(QuoteState.Accepted, 2, DateTimeOffset.UnixEpoch.AddMinutes(2)));

        SalesException exception = Assert.Throws<SalesException>(() =>
            quote.TransitionTo(QuoteState.Rejected, 2, DateTimeOffset.UnixEpoch.AddMinutes(3)));
        Assert.AreEqual("invalid_quote_transition", exception.Code);
        Assert.AreEqual((int)QuoteState.Accepted, quote.StatusId);
    }

    [TestMethod]
    public void QuoteTombstonePreservesLinesAndBlocksTransitions()
    {
        Quote quote = CreateQuote();
        quote.Tombstone(9, DateTimeOffset.UnixEpoch.AddDays(1));

        Assert.AreEqual(1, quote.QuoteLines.Count);
        Assert.AreEqual(9, quote.DeletedByUserId);
        Assert.Throws<SalesException>(() => quote.TransitionTo(QuoteState.Sent, 9, DateTimeOffset.UnixEpoch.AddDays(2)));
    }

    [TestMethod]
    public void ConvertedOrderCopiesSnapshotsAndPricingInputsExactly()
    {
        Quote quote = CreateQuote();
        quote.Id = 44;
        quote.TransitionTo(QuoteState.Sent, 1, DateTimeOffset.UnixEpoch);
        quote.TransitionTo(QuoteState.Accepted, 1, DateTimeOffset.UnixEpoch);

        SalesOrder order = SalesOrder.CreateFromQuote("SO-00000001", quote, 1, DateTimeOffset.UnixEpoch);

        Assert.AreEqual(44, order.SourceQuoteId);
        Assert.AreEqual("ACCOUNT", order.CustomerAccountNumberSnapshot);
        Assert.AreEqual("SKU", order.SalesOrderLines.Single().SkuSnapshot);
        Assert.AreEqual(10m, order.SalesOrderLines.Single().UnitPrice);
    }

    [TestMethod]
    public void SalesOrderStateMachineEnforcesShippingPayload()
    {
        SalesOrder order = CreateOrder();
        CarrierSalesReference carrier = new(3, "Carrier", true);
        order.TransitionTo(new(OrderState.Confirmed), null, 1, DateTimeOffset.UnixEpoch);
        order.TransitionTo(new(OrderState.Processing), null, 1, DateTimeOffset.UnixEpoch);

        SalesException missing = Assert.Throws<SalesException>(() =>
            order.TransitionTo(new(OrderState.Shipped), null, 1, DateTimeOffset.UnixEpoch));
        Assert.AreEqual("invalid_sales_order_transition", missing.Code);
        Assert.AreEqual((int)OrderState.Processing, order.StatusId);

        order.TransitionTo(new(OrderState.Shipped, 3, " TRACK "), carrier, 1, DateTimeOffset.UnixEpoch);
        Assert.AreEqual("TRACK", order.TrackingNumber);
        order.TransitionTo(new(OrderState.Completed), carrier, 1, DateTimeOffset.UnixEpoch);
        Assert.Throws<SalesException>(() => order.TransitionTo(new(OrderState.Cancelled), carrier, 1, DateTimeOffset.UnixEpoch));
    }

    [TestMethod]
    public void SearchAndVersionValidationRejectUnsafeInputs()
    {
        SalesValidationException search = Assert.Throws<SalesValidationException>(() =>
            SalesRequestValidator.Validate(new QuoteSearchRequest(PageNumber: 0, PageSize: 101, SortField: "DROP")));
        Assert.IsTrue(search.Errors.ContainsKey("pageNumber"));
        Assert.IsTrue(search.Errors.ContainsKey("pageSize"));
        Assert.IsTrue(search.Errors.ContainsKey("sortField"));
        Assert.Throws<SalesValidationException>(() => SalesRequestValidator.ValidateVersion("AQID"));
        SalesRequestValidator.ValidateVersion(Convert.ToBase64String(new byte[8]));
    }

    [TestMethod]
    public void MonetaryRoundingMatchesPublishedHalfAwayFromZeroPolicy()
    {
        Assert.AreEqual(1.01m, Domain.Sales.MonetaryRounding.Round(1.005m));
        Assert.AreEqual(-1.01m, Domain.Sales.MonetaryRounding.Round(-1.005m));
    }

    #endregion

    #region Private Methods

    private static Quote CreateQuote() => Quote.CreateDraft(
        "Q-00000001", QuoteRequest(), Customer(), Address(), Products(), 7, DateTimeOffset.UnixEpoch);

    private static SalesOrder CreateOrder() => SalesOrder.CreateDirectDraft(
        "SO-00000001", new SalesOrderCreateRequest(1, 10, null, null, null,
            [new SalesLineRequest(1, 1, 10m, 0)]),
        Customer(), Address(), Products(), null, 7, DateTimeOffset.UnixEpoch);

    private static QuoteCreateRequest QuoteRequest() => new(
        1, 10, new DateOnly(2025, 1, 1), new DateOnly(2025, 2, 1), null,
        [new SalesLineRequest(1, 1, 10m, 0)]);

    private static CustomerSalesReference Customer() =>
        new(1, "ACCOUNT", "Customer", "customer@example.test", null, true);

    private static AddressSalesReference Address() =>
        new(10, 1, "shipping", "Dock", "1 Street", null, "City", null, "1000", "AR");

    private static Dictionary<int, ProductSalesReference> Products() =>
        new() { [1] = new(1, "SKU", "Product", true) };

    #endregion
}
