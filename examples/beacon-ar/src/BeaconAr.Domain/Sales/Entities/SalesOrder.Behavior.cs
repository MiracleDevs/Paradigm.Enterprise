using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;
using BeaconAr.Interfaces.Sales.Entities;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Exceptions;
using OrderState = BeaconAr.Interfaces.Sales.Enums.SalesOrderStatus;

namespace BeaconAr.Domain.Sales.Entities;

public partial class SalesOrder
{
    #region Constants

    private const decimal MaximumStoredAmount = 99_999_999_999_999_999.99m;

    #endregion

    #region Properties

    public DomainTracker<SalesOrderLine> SalesOrderLinesDomainTracker { get; } = new();

    #endregion

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
        ValidateCustomerAndAddress(customer, address);
        string? tracking = NormalizeTracking(request.TrackingNumber);
        ValidateCarrier(request.CarrierId, carrier);
        IReadOnlyList<SalesOrderLine> lines = BuildLines(request.Lines, products);
        SalesOrder order = new()
        {
            OrderNumber = number,
            SourceQuoteId = null,
            CustomerId = customer.Id,
            ShippingAddressId = address.Id,
            StatusId = (int)OrderState.Draft,
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
        foreach (QuoteLine line in quote.QuoteLines)
            line.Validate();
        SalesOrder order = new()
        {
            OrderNumber = number,
            SourceQuoteId = quote.Id,
            CustomerId = quote.CustomerId,
            ShippingAddressId = quote.ShippingAddressId,
            StatusId = (int)OrderState.Draft,
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
        ValidateAggregate(order.SalesOrderLines);
        return order;
    }

    public IReadOnlyList<SalesOrderLine> Replace(
        SalesOrderUpdateRequest request,
        CustomerSalesReference customer,
        AddressSalesReference address,
        IReadOnlyDictionary<int, ProductSalesReference> products,
        CarrierSalesReference? carrier,
        int actorId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureDraft("updated");
        bool referencesChanged = request.CustomerId != CustomerId || request.ShippingAddressId != ShippingAddressId;
        if (referencesChanged)
            ValidateCustomerAndAddress(customer, address);
        string? tracking = NormalizeTracking(request.TrackingNumber);
        if (request.CarrierId != CarrierId)
            ValidateCarrier(request.CarrierId, carrier);
        IReadOnlyList<SalesOrderLine> lines = BuildLines(request.Lines, products, SalesOrderLines);
        ValidateMutationAudit(actorId, now, CreationDate);
        ValidateHeaderState(OrderNumber, SourceQuoteId, request.CustomerId, request.ShippingAddressId, StatusId,
            request.CarrierId, tracking,
            referencesChanged ? customer.AccountNumber : CustomerAccountNumberSnapshot,
            referencesChanged ? customer.Name : CustomerNameSnapshot,
            referencesChanged ? customer.Email : CustomerEmailSnapshot,
            referencesChanged ? customer.Phone : CustomerPhoneSnapshot,
            referencesChanged ? address.Label : ShippingLabelSnapshot,
            referencesChanged ? address.Line1 : ShippingLine1Snapshot,
            referencesChanged ? address.Line2 : ShippingLine2Snapshot,
            referencesChanged ? address.City : ShippingCitySnapshot,
            referencesChanged ? address.State : ShippingStateSnapshot,
            referencesChanged ? address.PostalCode : ShippingPostalCodeSnapshot,
            referencesChanged ? address.Country : ShippingCountrySnapshot,
            referencesChanged ? address.AddressTypeCode : ShippingAddressTypeCodeSnapshot,
            DeletionDate, DeletedByUserId);

        CustomerId = request.CustomerId;
        ShippingAddressId = request.ShippingAddressId;
        RequestedShipDate = request.RequestedShipDate;
        CarrierId = request.CarrierId;
        TrackingNumber = tracking;
        if (referencesChanged)
            ApplySnapshots(customer, address);
        Touch(actorId, now);
        return lines;
    }

    public OrderState TransitionTo(
        SalesOrderStatusTransitionRequest request,
        CarrierSalesReference? carrier,
        int actorId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (DeletionDate.HasValue || !Enum.IsDefined(request.Status))
            throw InvalidTransition();
        OrderState current = (OrderState)StatusId;
        bool allowed = current == OrderState.Draft && request.Status is OrderState.Confirmed or OrderState.Cancelled ||
                       current == OrderState.Confirmed && request.Status is OrderState.Processing or OrderState.Cancelled ||
                       current == OrderState.Processing && request.Status is OrderState.Shipped or OrderState.Cancelled ||
                       current == OrderState.Shipped && request.Status == OrderState.Completed;
        if (!allowed)
            throw InvalidTransition();

        bool hasShippingPayload = request.CarrierId.HasValue || request.TrackingNumber is not null;
        if (request.Status != OrderState.Shipped && hasShippingPayload)
            throw InvalidTransition();
        if (request.Status == OrderState.Confirmed)
            ValidateComplete();
        if (request.Status == OrderState.Shipped)
        {
            int? carrierId = request.CarrierId ?? CarrierId;
            string? tracking = NormalizeTracking(request.TrackingNumber ?? TrackingNumber);
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

    public void AddSalesOrderLines(SalesOrderLine? entity)
    {
        if (entity is null)
            return;
        SalesOrderLines.Add(entity);
        SalesOrderLinesDomainTracker.Add(entity);
    }

    public void RemoveSalesOrderLines(SalesOrderLine? entity)
    {
        if (entity is null)
            return;
        SalesOrderLines.Remove(entity);
        SalesOrderLinesDomainTracker.Remove(entity);
    }

    #endregion

    #region Private Methods

    partial void ValidateEntity()
    {
        ValidateHeaderState(OrderNumber, SourceQuoteId, CustomerId, ShippingAddressId, StatusId, CarrierId,
            TrackingNumber, CustomerAccountNumberSnapshot, CustomerNameSnapshot, CustomerEmailSnapshot,
            CustomerPhoneSnapshot, ShippingLabelSnapshot, ShippingLine1Snapshot, ShippingLine2Snapshot,
            ShippingCitySnapshot, ShippingStateSnapshot, ShippingPostalCodeSnapshot, ShippingCountrySnapshot,
            ShippingAddressTypeCodeSnapshot, DeletionDate, DeletedByUserId);
        foreach (SalesOrderLine line in SalesOrderLines)
            line.Validate();
        ValidateAggregate(SalesOrderLines);
    }

    partial void BeforeMap(ISalesOrder model)
    {
        _ = Id;
        ValidateHeaderState(model.OrderNumber, model.SourceQuoteId, model.CustomerId, model.ShippingAddressId,
            model.StatusId, model.CarrierId, model.TrackingNumber, model.CustomerAccountNumberSnapshot,
            model.CustomerNameSnapshot, model.CustomerEmailSnapshot, model.CustomerPhoneSnapshot,
            model.ShippingLabelSnapshot, model.ShippingLine1Snapshot, model.ShippingLine2Snapshot,
            model.ShippingCitySnapshot, model.ShippingStateSnapshot, model.ShippingPostalCodeSnapshot,
            model.ShippingCountrySnapshot, model.ShippingAddressTypeCodeSnapshot, model.DeletionDate,
            model.DeletedByUserId);
    }

    partial void AfterMap(ISalesOrder model) => TrackingNumber = NormalizeTracking(TrackingNumber);

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
        if (DeletionDate.HasValue || StatusId != (int)OrderState.Draft)
            throw new SalesException("invalid_sales_order_transition", $"Only a draft sales order can be {action}.");
    }

    private void Touch(int actorId, DateTimeOffset now)
    {
        ModifiedByUserId = actorId;
        ModificationDate = now;
    }

    private void ValidateComplete()
    {
        ValidateAggregate(SalesOrderLines);
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

    private static List<SalesOrderLine> BuildLines(IReadOnlyList<SalesLineRequest>? requests,
        IReadOnlyDictionary<int, ProductSalesReference> products, IEnumerable<SalesOrderLine>? existingLines = null)
    {
        if (requests is null || requests.Count == 0)
            throw Validation("lines", "At least one line is required.");
        Dictionary<int, SalesOrderLine> existing = existingLines?.ToDictionary(line => line.ProductId) ?? [];
        HashSet<int> productIds = [];
        var lines = new List<SalesOrderLine>(requests.Count);
        for (int index = 0; index < requests.Count; index++)
        {
            SalesLineRequest request = requests[index];
            if (!productIds.Add(request.ProductId))
                throw Validation($"lines.{index}.productId", "Product IDs cannot repeat within a transaction.");
            if (!products.TryGetValue(request.ProductId, out ProductSalesReference? product))
                throw Validation($"lines.{index}.productId", "The selected product was not found.");
            if (existing.TryGetValue(request.ProductId, out SalesOrderLine? old))
            {
                if (!product.IsActive && (old.Quantity != request.Quantity || old.UnitPrice != request.UnitPrice ||
                                          old.DiscountPercent != request.DiscountPercent))
                    throw Validation($"lines.{index}.productId", "An inactive product cannot be newly selected or changed.");
                lines.Add(SalesOrderLine.Create(request, old.SkuSnapshot, old.ProductNameSnapshot));
            }
            else
            {
                if (!product.IsActive)
                    throw Validation($"lines.{index}.productId", "An inactive product cannot be newly selected or changed.");
                lines.Add(SalesOrderLine.Create(request, product.Sku, product.Name));
            }
        }
        ValidateAggregate(lines);
        return lines;
    }

    private static void ValidateAggregate(IEnumerable<SalesOrderLine> lines)
    {
        SalesOrderLine[] values = lines.ToArray();
        var rules = new DomainValidator();
        rules.Assert(values.Length > 0, "At least one sales order line is required.");
        rules.Assert(values.Select(line => line.ProductId).Distinct().Count() == values.Length,
            "Product IDs cannot repeat within a sales order.");
        try
        {
            decimal subtotal = values.Aggregate(0m, (sum, line) => checked(sum + line.CalculateSubtotal()));
            decimal discount = values.Aggregate(0m, (sum, line) => checked(sum + line.CalculateDiscount()));
            rules.Assert(subtotal <= MaximumStoredAmount && discount <= MaximumStoredAmount && subtotal - discount <= MaximumStoredAmount,
                "The calculated sales order totals are too large.");
        }
        catch (OverflowException)
        {
            rules.AddError("The calculated sales order totals are too large.");
        }
        rules.ThrowIfAny();
    }

    private static void ValidateCustomerAndAddress(CustomerSalesReference customer, AddressSalesReference address)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(address);
        if (!customer.IsActive)
            throw new SalesException("reference_inactive", "The selected customer is inactive.");
        if (address.CustomerId != customer.Id || address.AddressTypeCode is not "shipping" and not "both")
            throw new SalesException("invalid_shipping_address", "The selected address is not an eligible shipping address for the customer.");
    }

    private static string? NormalizeTracking(string? trackingNumber)
    {
        string? value = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim();
        if (value?.Length > 200)
            throw Validation("trackingNumber", "Tracking number cannot exceed 200 characters.");
        return value;
    }

    private static void ValidateMutationAudit(int actorId, DateTimeOffset now, DateTimeOffset creationDate)
    {
        var rules = new DomainValidator();
        rules.Assert(actorId > 0, "Sales order modification actor ID must be positive.");
        rules.Assert(now != default && now >= creationDate, "Sales order modification time cannot precede creation.");
        rules.ThrowIfAny();
    }

    private static void ValidateHeaderState(string? orderNumber, int? sourceQuoteId, int customerId,
        int addressId, int statusId, int? carrierId, string? trackingNumber, string? accountNumber,
        string? customerName, string? email, string? phone, string? shippingLabel, string? line1,
        string? line2, string? city, string? state, string? postalCode, string? country,
        string? addressTypeCode, DateTimeOffset? deletionDate, int? deletedByUserId)
    {
        var rules = new DomainValidator();
        rules.Assert(orderNumber?.Trim().Length is >= 1 and <= 20, "Order number is required and cannot exceed 20 characters.");
        rules.Assert(sourceQuoteId is null or > 0, "Source quote ID must be positive when supplied.");
        rules.Assert(customerId > 0, "Customer ID must be positive.");
        rules.Assert(addressId > 0, "Shipping address ID must be positive.");
        rules.Assert(Enum.IsDefined((OrderState)statusId), "Sales order status is invalid.");
        rules.Assert(carrierId is null or > 0, "Carrier ID must be positive when supplied.");
        rules.Assert(trackingNumber is null || trackingNumber.Trim().Length is >= 1 and <= 200,
            "Tracking number cannot be blank or exceed 200 characters.");
        bool requiresShipping = statusId is (int)OrderState.Shipped or (int)OrderState.Completed;
        rules.Assert(!requiresShipping || carrierId.HasValue && !string.IsNullOrWhiteSpace(trackingNumber),
            "Shipped and completed orders require a carrier and tracking number.");
        rules.Assert(accountNumber?.Length is >= 1 and <= 50, "Customer account snapshot is required and cannot exceed 50 characters.");
        rules.Assert(customerName?.Length is >= 1 and <= 120, "Customer name snapshot is required and cannot exceed 120 characters.");
        rules.Assert(email?.Length is >= 1 and <= 320, "Customer email snapshot is required and cannot exceed 320 characters.");
        rules.Assert(phone is null || phone.Length <= 50, "Customer phone snapshot cannot exceed 50 characters.");
        rules.Assert(shippingLabel?.Length is >= 1 and <= 120, "Shipping label snapshot is required and cannot exceed 120 characters.");
        rules.Assert(line1?.Length is >= 1 and <= 200, "Shipping line 1 snapshot is required and cannot exceed 200 characters.");
        rules.Assert(line2 is null || line2.Length <= 200, "Shipping line 2 snapshot cannot exceed 200 characters.");
        rules.Assert(city?.Length is >= 1 and <= 120, "Shipping city snapshot is required and cannot exceed 120 characters.");
        rules.Assert(state is null || state.Length <= 120, "Shipping state snapshot cannot exceed 120 characters.");
        rules.Assert(postalCode?.Length is >= 1 and <= 32, "Shipping postal code snapshot is required and cannot exceed 32 characters.");
        rules.Assert(country?.Length == 2, "Shipping country snapshot must be a two-letter code.");
        rules.Assert(addressTypeCode?.Length is >= 1 and <= 32, "Shipping address type snapshot is required and cannot exceed 32 characters.");
        rules.Assert(deletionDate.HasValue == deletedByUserId.HasValue, "Sales order deletion time and actor must be set together.");
        rules.ThrowIfAny();
    }

    private static SalesValidationException Validation(string field, string message) =>
        new(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) { [field] = [message] });

    #endregion
}
