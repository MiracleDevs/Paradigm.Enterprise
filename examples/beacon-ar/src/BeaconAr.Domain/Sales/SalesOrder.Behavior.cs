using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;
using BeaconAr.Domain.Sales.Validation;

namespace BeaconAr.Domain.Receivables.Generated;

public partial class SalesOrder
{
    #region Public Methods

    public static SalesOrder CreateDirectDraft(
        string number,
        SalesOrderCreateRequest request,
        CustomerSalesReference customer,
        AddressSalesReference address,
        IReadOnlyDictionary<int, ProductSalesReference> products,
        CarrierSalesReference? carrier,
        int actorId,
        DateTimeOffset now)
    {
        SalesDomainValidation.ValidateCustomerAndAddress(customer, address);
        string? tracking = SalesDomainValidation.NormalizeTracking(request.TrackingNumber);
        ValidateCarrier(request.CarrierId, carrier);
        IReadOnlyList<SalesOrderLine> lines = SalesDomainValidation.BuildOrderLines(request.Lines, products);
        SalesOrder order = new()
        {
            OrderNumber = number,
            SourceQuoteId = null,
            CustomerId = customer.Id,
            ShippingAddressId = address.Id,
            StatusId = (int)Sales.SalesOrderStatus.Draft,
            RequestedShipDate = request.RequestedShipDate,
            CarrierId = request.CarrierId,
            TrackingNumber = tracking,
            CreatedByUserId = actorId,
            CreationDate = now,
        };
        order.ApplySnapshots(customer, address);
        foreach (SalesOrderLine line in lines)
            order.SalesOrderLines.Add(line);
        return order;
    }

    public static SalesOrder CreateFromQuote(string number, Quote quote, int actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(quote);
        quote.ValidateForConversion();
        SalesDomainValidation.ValidateStoredPricing(quote.QuoteLines);
        SalesOrder order = new()
        {
            OrderNumber = number,
            SourceQuoteId = quote.Id,
            CustomerId = quote.CustomerId,
            ShippingAddressId = quote.ShippingAddressId,
            StatusId = (int)Sales.SalesOrderStatus.Draft,
            CustomerAccountNumberSnapshot = quote.CustomerAccountNumberSnapshot,
            CustomerNameSnapshot = quote.CustomerNameSnapshot,
            CustomerEmailSnapshot = quote.CustomerEmailSnapshot,
            CustomerPhoneSnapshot = quote.CustomerPhoneSnapshot,
            ShippingLabelSnapshot = quote.ShippingLabelSnapshot,
            ShippingLine1Snapshot = quote.ShippingLine1Snapshot,
            ShippingLine2Snapshot = quote.ShippingLine2Snapshot,
            ShippingCitySnapshot = quote.ShippingCitySnapshot,
            ShippingStateSnapshot = quote.ShippingStateSnapshot,
            ShippingPostalCodeSnapshot = quote.ShippingPostalCodeSnapshot,
            ShippingCountrySnapshot = quote.ShippingCountrySnapshot,
            ShippingAddressTypeCodeSnapshot = quote.ShippingAddressTypeCodeSnapshot,
            CreatedByUserId = actorId,
            CreationDate = now,
        };
        foreach (QuoteLine line in quote.QuoteLines.OrderBy(line => line.Id))
            order.SalesOrderLines.Add(SalesOrderLine.Copy(line));
        return order;
    }

    public IReadOnlyList<SalesOrderLine> PrepareReplacement(
        SalesOrderUpdateRequest request,
        CustomerSalesReference customer,
        AddressSalesReference address,
        IReadOnlyDictionary<int, ProductSalesReference> products,
        CarrierSalesReference? carrier)
    {
        EnsureDraft("updated");
        if (request.CustomerId != CustomerId || request.ShippingAddressId != ShippingAddressId)
            SalesDomainValidation.ValidateCustomerAndAddress(customer, address);
        _ = SalesDomainValidation.NormalizeTracking(request.TrackingNumber);
        if (request.CarrierId != CarrierId)
            ValidateCarrier(request.CarrierId, carrier);
        return SalesDomainValidation.BuildOrderLines(request.Lines, products, SalesOrderLines);
    }

    public void ApplyReplacement(
        SalesOrderUpdateRequest request,
        CustomerSalesReference customer,
        AddressSalesReference address,
        int actorId,
        DateTimeOffset now)
    {
        bool referencesChanged = request.CustomerId != CustomerId || request.ShippingAddressId != ShippingAddressId;
        CustomerId = request.CustomerId;
        ShippingAddressId = request.ShippingAddressId;
        RequestedShipDate = request.RequestedShipDate;
        CarrierId = request.CarrierId;
        TrackingNumber = SalesDomainValidation.NormalizeTracking(request.TrackingNumber);
        if (referencesChanged)
            ApplySnapshots(customer, address);
        Touch(actorId, now);
    }

    public Sales.SalesOrderStatus TransitionTo(
        SalesOrderStatusTransitionRequest request,
        CarrierSalesReference? carrier,
        int actorId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (DeletionDate.HasValue || !Enum.IsDefined(request.Status))
            throw InvalidTransition();
        Sales.SalesOrderStatus current = (Sales.SalesOrderStatus)StatusId;
        bool allowed = current == Sales.SalesOrderStatus.Draft && request.Status is Sales.SalesOrderStatus.Confirmed or Sales.SalesOrderStatus.Cancelled ||
                       current == Sales.SalesOrderStatus.Confirmed && request.Status is Sales.SalesOrderStatus.Processing or Sales.SalesOrderStatus.Cancelled ||
                       current == Sales.SalesOrderStatus.Processing && request.Status is Sales.SalesOrderStatus.Shipped or Sales.SalesOrderStatus.Cancelled ||
                       current == Sales.SalesOrderStatus.Shipped && request.Status == Sales.SalesOrderStatus.Completed;
        if (!allowed)
            throw InvalidTransition();

        bool hasShippingPayload = request.CarrierId.HasValue || request.TrackingNumber is not null;
        if (request.Status != Sales.SalesOrderStatus.Shipped && hasShippingPayload)
            throw InvalidTransition();
        if (request.Status == Sales.SalesOrderStatus.Confirmed)
            ValidateComplete();
        if (request.Status == Sales.SalesOrderStatus.Shipped)
        {
            int? carrierId = request.CarrierId ?? CarrierId;
            string? tracking = SalesDomainValidation.NormalizeTracking(request.TrackingNumber ?? TrackingNumber);
            if (!carrierId.HasValue || tracking is null)
                throw new SalesException("invalid_sales_order_transition", "Shipping requires an active carrier and a tracking number.");
            if (request.CarrierId.HasValue)
                ValidateCarrier(carrierId, carrier);
            else if (carrier is null || !carrier.IsActive)
                throw new SalesException("reference_inactive", "The assigned carrier is inactive.");
            CarrierId = carrierId;
            TrackingNumber = tracking;
        }
        StatusId = (int)request.Status;
        Touch(actorId, now);
        return current;
    }

    public void Tombstone(int actorId, DateTimeOffset now)
    {
        EnsureDraft("deleted");
        DeletionDate = now;
        DeletedByUserId = actorId;
        Touch(actorId, now);
    }

    #endregion

    #region Private Methods

    private void ApplySnapshots(CustomerSalesReference customer, AddressSalesReference address)
    {
        CustomerAccountNumberSnapshot = customer.AccountNumber;
        CustomerNameSnapshot = customer.Name;
        CustomerEmailSnapshot = customer.Email;
        CustomerPhoneSnapshot = customer.Phone;
        ShippingLabelSnapshot = address.Label;
        ShippingLine1Snapshot = address.Line1;
        ShippingLine2Snapshot = address.Line2;
        ShippingCitySnapshot = address.City;
        ShippingStateSnapshot = address.State;
        ShippingPostalCodeSnapshot = address.PostalCode;
        ShippingCountrySnapshot = address.Country;
        ShippingAddressTypeCodeSnapshot = address.AddressTypeCode;
    }

    private void EnsureDraft(string action)
    {
        if (DeletionDate.HasValue || StatusId != (int)Sales.SalesOrderStatus.Draft)
            throw new SalesException("invalid_sales_order_transition", $"Only a draft sales order can be {action}.");
    }

    private void Touch(int actorId, DateTimeOffset now)
    {
        ModifiedByUserId = actorId;
        ModificationDate = now;
    }

    private void ValidateComplete()
    {
        SalesDomainValidation.ValidateStoredLines(SalesOrderLines);
        if (string.IsNullOrWhiteSpace(CustomerAccountNumberSnapshot) || string.IsNullOrWhiteSpace(CustomerNameSnapshot) ||
            string.IsNullOrWhiteSpace(CustomerEmailSnapshot) || string.IsNullOrWhiteSpace(ShippingLine1Snapshot))
            throw new SalesException("invalid_sales_order_transition", "The sales order is incomplete and cannot be confirmed.");
    }

    private static void ValidateCarrier(int? carrierId, CarrierSalesReference? carrier)
    {
        if (carrierId.HasValue && (carrier is null || carrier.Id != carrierId.Value))
            throw new SalesValidationException(new Dictionary<string, IReadOnlyList<string>>
            {
                ["carrierId"] = ["The selected carrier was not found."],
            });
        if (carrier is not null && !carrier.IsActive)
            throw new SalesException("reference_inactive", "The selected carrier is inactive.");
    }

    private static SalesException InvalidTransition() =>
        new("invalid_sales_order_transition", "The requested sales order status transition is not allowed.");

    #endregion
}
