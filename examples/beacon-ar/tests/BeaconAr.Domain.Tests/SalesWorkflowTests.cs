using BeaconAr.Domain.Access.Entities;
using BeaconAr.Domain.MasterData.Entities;
using BeaconAr.Domain.Operations.Entities;
using BeaconAr.Domain.Sales.Entities;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;
using BeaconAr.Domain.Operations;
using OrderState = BeaconAr.Interfaces.Sales.Enums.SalesOrderStatus;
using QuoteState = BeaconAr.Interfaces.Sales.Enums.QuoteStatus;
using Paradigm.Enterprise.Domain.Exceptions;

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

        Assert.Throws<DomainException>(() =>
            Quote.CreateDraft("Q-00000001", QuoteRequest() with
            {
                Lines = [new(1, 1, 1.12345m, 0)],
            }, Customer(), Address(), Products(), 1, DateTimeOffset.UnixEpoch));
        SalesValidationException lines = Assert.Throws<SalesValidationException>(() =>
            Quote.CreateDraft("Q-00000001", QuoteRequest() with
            {
                Lines = [new(1, 1, 1m, 0), new(1, 1, 1m, 0)],
            }, Customer(), Address(), Products(), 1, DateTimeOffset.UnixEpoch));
        Assert.IsTrue(lines.Errors.ContainsKey("lines.1.productId"));
    }

    [TestMethod]
    public void FailedQuoteReplacementDoesNotMutateAggregate()
    {
        Quote quote = CreateQuote();
        QuoteUpdateRequest invalid = new(1, 10, quote.QuoteDate, quote.ValidUntil, "changed",
            [new SalesLineRequest(1, 2, 10m, 0)]);
        Dictionary<int, ProductSalesReference> inactive = new() { [1] = new(1, "NEW", "Changed", false) };

        Assert.Throws<SalesValidationException>(() => quote.Replace(invalid, Customer(), Address(), inactive, 8,
            DateTimeOffset.UnixEpoch.AddMinutes(1)));
        Assert.IsNull(quote.Notes);
        Assert.AreEqual(1, quote.QuoteLines.Single().Quantity);
        Assert.IsNull(quote.ModificationDate);
    }

    [TestMethod]
    public void QuoteReplacementRejectsInvalidNotesAndNonDraftStateWithoutAnyMutation()
    {
        Quote quote = CreateQuote();
        string draft = Snapshot(quote);
        var invalidNotes = new QuoteUpdateRequest(2, 20, new DateOnly(2025, 1, 2),
            new DateOnly(2025, 3, 1), new string('x', 1001), [new SalesLineRequest(1, 2, 11m, 1)]);

        Assert.Throws<SalesValidationException>(() => quote.Replace(invalidNotes, OtherCustomer(), OtherAddress(),
            Products(), 8, DateTimeOffset.UnixEpoch.AddMinutes(1)));
        Assert.AreEqual(draft, Snapshot(quote));

        quote.TransitionTo(QuoteState.Sent, 8, DateTimeOffset.UnixEpoch.AddMinutes(1));
        string sent = Snapshot(quote);
        var valid = invalidNotes with { Notes = "changed" };
        Assert.Throws<SalesException>(() => quote.Replace(valid, OtherCustomer(), OtherAddress(), Products(), 9,
            DateTimeOffset.UnixEpoch.AddMinutes(2)));
        Assert.AreEqual(sent, Snapshot(quote));
    }

    [TestMethod]
    public void SalesOrderReplacementRejectsInvalidTrackingAndNonDraftStateWithoutAnyMutation()
    {
        SalesOrder order = CreateOrder();
        string draft = Snapshot(order);
        var invalidTracking = new SalesOrderUpdateRequest(2, 20, new DateOnly(2025, 1, 5), 3,
            new string('x', 201), [new SalesLineRequest(1, 2, 11m, 1)]);
        var carrier = new CarrierSalesReference(3, "Carrier", true);

        Assert.Throws<SalesValidationException>(() => order.Replace(invalidTracking, OtherCustomer(), OtherAddress(),
            Products(), carrier, 8, DateTimeOffset.UnixEpoch.AddMinutes(1)));
        Assert.AreEqual(draft, Snapshot(order));

        order.TransitionTo(new(OrderState.Confirmed), null, 8, DateTimeOffset.UnixEpoch.AddMinutes(1));
        string confirmed = Snapshot(order);
        var valid = invalidTracking with { TrackingNumber = "TRACK" };
        Assert.Throws<SalesException>(() => order.Replace(valid, OtherCustomer(), OtherAddress(), Products(), carrier,
            9, DateTimeOffset.UnixEpoch.AddMinutes(2)));
        Assert.AreEqual(confirmed, Snapshot(order));
    }

    [TestMethod]
    public void SalesAggregatesExposeOnlyOneReplacementOperation()
    {
        Assert.IsNotNull(typeof(Quote).GetMethod(nameof(Quote.Replace)));
        Assert.IsNull(typeof(Quote).GetMethod("PrepareReplacement"));
        Assert.IsNull(typeof(Quote).GetMethod("ApplyReplacement"));
        Assert.IsNotNull(typeof(SalesOrder).GetMethod(nameof(SalesOrder.Replace)));
        Assert.IsNull(typeof(SalesOrder).GetMethod("PrepareReplacement"));
        Assert.IsNull(typeof(SalesOrder).GetMethod("ApplyReplacement"));
    }

    [TestMethod]
    public void UnchangedInactiveQuoteLinePreservesHistoricalSnapshot()
    {
        Quote quote = CreateQuote();
        QuoteUpdateRequest request = new(1, 10, quote.QuoteDate, quote.ValidUntil, null,
            [new SalesLineRequest(1, 1, 10m, 0)]);
        Dictionary<int, ProductSalesReference> inactive = new() { [1] = new(1, "NEW", "Changed", false) };

        QuoteLine line = quote.Replace(request, Customer(), Address(), inactive, 8,
            DateTimeOffset.UnixEpoch.AddMinutes(1)).Single();

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

        QuoteLine quoteLine = quote.Replace(quoteRequest, Customer(), Address(), renamed, 8,
            DateTimeOffset.UnixEpoch.AddMinutes(1)).Single();
        SalesOrderLine orderLine = order.Replace(orderRequest, Customer(), Address(), renamed, null, 8,
            DateTimeOffset.UnixEpoch.AddMinutes(1)).Single();

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

        DomainException quoteError = Assert.Throws<DomainException>(() =>
            Quote.CreateDraft("Q-00000002", quoteRequest, Customer(), Address(), products, 1, DateTimeOffset.UnixEpoch));
        DomainException orderError = Assert.Throws<DomainException>(() =>
            SalesOrder.CreateDirectDraft("SO-00000002", new SalesOrderCreateRequest(1, 10, null, null, null, quoteRequest.Lines),
                Customer(), Address(), products, null, 1, DateTimeOffset.UnixEpoch));

        StringAssert.Contains(quoteError.Message, "too large");
        StringAssert.Contains(orderError.Message, "too large");
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
            new QuoteSearchRequest(PageNumber: 0, PageSize: 101, SortField: "DROP").Validate());
        Assert.IsTrue(search.Errors.ContainsKey("pageNumber"));
        Assert.IsTrue(search.Errors.ContainsKey("pageSize"));
        Assert.IsTrue(search.Errors.ContainsKey("sortField"));
        Assert.Throws<VersionTokenException>(() => VersionTokenCodec.Decode("AQID"));
        _ = VersionTokenCodec.Decode(Convert.ToBase64String(new byte[8]));
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

    private static CustomerSalesReference OtherCustomer() =>
        new(2, "OTHER", "Other customer", "other@example.test", "555", true);

    private static AddressSalesReference OtherAddress() =>
        new(20, 2, "shipping", "Other dock", "2 Street", "Suite", "Other city", "State", "2000", "US");

    private static string Snapshot(Quote quote) => string.Join('\u001f',
        quote.QuoteNumber, quote.CustomerId, quote.ShippingAddressId, quote.QuoteDate, quote.ValidUntil, quote.StatusId,
        quote.Notes, quote.CustomerAccountNumberSnapshot, quote.CustomerNameSnapshot, quote.CustomerEmailSnapshot,
        quote.CustomerPhoneSnapshot, quote.ShippingLabelSnapshot, quote.ShippingLine1Snapshot,
        quote.ShippingLine2Snapshot, quote.ShippingCitySnapshot, quote.ShippingStateSnapshot,
        quote.ShippingPostalCodeSnapshot, quote.ShippingCountrySnapshot, quote.ShippingAddressTypeCodeSnapshot,
        quote.CreatedByUserId, quote.CreationDate, quote.ModifiedByUserId, quote.ModificationDate,
        quote.DeletedByUserId, quote.DeletionDate, Convert.ToBase64String(quote.RowVersion ?? []),
        string.Join('|', quote.QuoteLines.OrderBy(line => line.Id).Select(line => string.Join(':', line.Id,
            line.QuoteId, line.ProductId, line.SkuSnapshot, line.ProductNameSnapshot, line.Quantity, line.UnitPrice,
            line.DiscountPercent, line.LineSubtotal, line.DiscountAmount, line.LineTotal))));

    private static string Snapshot(SalesOrder order) => string.Join('\u001f',
        order.OrderNumber, order.SourceQuoteId, order.CustomerId, order.ShippingAddressId, order.StatusId,
        order.RequestedShipDate, order.CarrierId, order.TrackingNumber, order.CustomerAccountNumberSnapshot,
        order.CustomerNameSnapshot, order.CustomerEmailSnapshot, order.CustomerPhoneSnapshot,
        order.ShippingLabelSnapshot, order.ShippingLine1Snapshot, order.ShippingLine2Snapshot,
        order.ShippingCitySnapshot, order.ShippingStateSnapshot, order.ShippingPostalCodeSnapshot,
        order.ShippingCountrySnapshot, order.ShippingAddressTypeCodeSnapshot, order.CreatedByUserId,
        order.CreationDate, order.ModifiedByUserId, order.ModificationDate, order.DeletedByUserId,
        order.DeletionDate, Convert.ToBase64String(order.RowVersion ?? []),
        string.Join('|', order.SalesOrderLines.OrderBy(line => line.Id).Select(line => string.Join(':', line.Id,
            line.SalesOrderId, line.ProductId, line.SkuSnapshot, line.ProductNameSnapshot, line.Quantity,
            line.UnitPrice, line.DiscountPercent, line.LineSubtotal, line.DiscountAmount, line.LineTotal))));

    #endregion
}
