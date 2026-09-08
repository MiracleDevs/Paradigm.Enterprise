using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;
using BeaconAr.Interfaces.Sales.Entities;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Exceptions;
using QuoteState = BeaconAr.Interfaces.Sales.Enums.QuoteStatus;

namespace BeaconAr.Domain.Sales.Entities;

public partial class Quote
{
    #region Constants

    private const decimal MaximumStoredAmount = 99_999_999_999_999_999.99m;

    #endregion

    #region Properties

    public DomainTracker<QuoteLine> QuoteLinesDomainTracker { get; } = new();

    #endregion

    #region Public Methods

    public static Quote CreateDraft(
        string number,
        QuoteCreateRequest request,
        CustomerSalesReference customer,
        AddressSalesReference address,
        IReadOnlyDictionary<int, ProductSalesReference> products,
        int actorId,
        DateTimeOffset now)
    {
        ValidateDates(request.QuoteDate, request.ValidUntil);
        ValidateCustomerAndAddress(customer, address);
        string? notes = NormalizeNotes(request.Notes);
        IReadOnlyList<QuoteLine> lines = BuildLines(request.Lines, products);
        Quote quote = new()
        {
            QuoteNumber = number,
            CustomerId = customer.Id,
            ShippingAddressId = address.Id,
            QuoteDate = request.QuoteDate,
            ValidUntil = request.ValidUntil,
            StatusId = (int)QuoteState.Draft,
            Notes = notes,
            CreatedByUserId = actorId,
            CreationDate = now,
        };
        quote.ApplySnapshots(customer, address);
        foreach (QuoteLine line in lines)
            quote.QuoteLines.Add(line);
        return quote;
    }

    public IReadOnlyList<QuoteLine> Replace(
        QuoteUpdateRequest request,
        CustomerSalesReference customer,
        AddressSalesReference address,
        IReadOnlyDictionary<int, ProductSalesReference> products,
        int actorId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureDraft("updated");
        ValidateDates(request.QuoteDate, request.ValidUntil);
        bool referencesChanged = request.CustomerId != CustomerId || request.ShippingAddressId != ShippingAddressId;
        if (referencesChanged)
            ValidateCustomerAndAddress(customer, address);
        string? notes = NormalizeNotes(request.Notes);
        IReadOnlyList<QuoteLine> lines = BuildLines(request.Lines, products, QuoteLines);
        ValidateMutationAudit(actorId, now, CreationDate);
        ValidateHeaderState(QuoteNumber, request.CustomerId, request.ShippingAddressId, request.QuoteDate,
            request.ValidUntil, StatusId, notes,
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
        QuoteDate = request.QuoteDate;
        ValidUntil = request.ValidUntil;
        Notes = notes;
        if (referencesChanged)
            ApplySnapshots(customer, address);
        Touch(actorId, now);
        return lines;
    }

    public QuoteState TransitionTo(QuoteState target, int actorId, DateTimeOffset now)
    {
        if (DeletionDate.HasValue || !Enum.IsDefined(target))
            throw InvalidTransition();
        QuoteState current = (QuoteState)StatusId;
        bool allowed = current == QuoteState.Draft && target == QuoteState.Sent ||
                       current == QuoteState.Sent && target is QuoteState.Accepted or QuoteState.Rejected or QuoteState.Expired;
        if (!allowed)
            throw InvalidTransition();
        if (target == QuoteState.Sent)
            ValidateComplete();
        StatusId = (int)target;
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

    public void AddQuoteLines(QuoteLine? entity)
    {
        if (entity is null)
            return;
        QuoteLines.Add(entity);
        QuoteLinesDomainTracker.Add(entity);
    }

    public void RemoveQuoteLines(QuoteLine? entity)
    {
        if (entity is null)
            return;
        QuoteLines.Remove(entity);
        QuoteLinesDomainTracker.Remove(entity);
    }

    public void ValidateForConversion()
    {
        if (DeletionDate.HasValue || StatusId != (int)QuoteState.Accepted)
            throw new SalesException("quote_not_accepted", "Only an accepted quote can be converted.");
        ValidateComplete();
    }

    #endregion

    #region Private Methods

    partial void ValidateEntity()
    {
        ValidateHeaderState(QuoteNumber, CustomerId, ShippingAddressId, QuoteDate, ValidUntil, StatusId, Notes,
            CustomerAccountNumberSnapshot, CustomerNameSnapshot, CustomerEmailSnapshot, CustomerPhoneSnapshot,
            ShippingLabelSnapshot, ShippingLine1Snapshot, ShippingLine2Snapshot, ShippingCitySnapshot,
            ShippingStateSnapshot, ShippingPostalCodeSnapshot, ShippingCountrySnapshot,
            ShippingAddressTypeCodeSnapshot, DeletionDate, DeletedByUserId);
        foreach (QuoteLine line in QuoteLines)
            line.Validate();
        ValidateAggregate(QuoteLines);
    }

    partial void BeforeMap(IQuote model)
    {
        _ = Id;
        ValidateHeaderState(model.QuoteNumber, model.CustomerId, model.ShippingAddressId, model.QuoteDate,
            model.ValidUntil, model.StatusId, model.Notes, model.CustomerAccountNumberSnapshot,
            model.CustomerNameSnapshot, model.CustomerEmailSnapshot, model.CustomerPhoneSnapshot,
            model.ShippingLabelSnapshot, model.ShippingLine1Snapshot, model.ShippingLine2Snapshot,
            model.ShippingCitySnapshot, model.ShippingStateSnapshot, model.ShippingPostalCodeSnapshot,
            model.ShippingCountrySnapshot, model.ShippingAddressTypeCodeSnapshot, model.DeletionDate,
            model.DeletedByUserId);
    }

    partial void AfterMap(IQuote model) => Notes = NormalizeNotes(Notes);

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
        if (DeletionDate.HasValue || StatusId != (int)QuoteState.Draft)
            throw new SalesException("invalid_quote_transition", $"Only a draft quote can be {action}.");
    }

    private void Touch(int actorId, DateTimeOffset now)
    {
        ModifiedByUserId = actorId;
        ModificationDate = now;
    }

    private void ValidateComplete()
    {
        ValidateDates(QuoteDate, ValidUntil);
        ValidateAggregate(QuoteLines);
        _ = NormalizeNotes(Notes);
        if (string.IsNullOrWhiteSpace(CustomerAccountNumberSnapshot) || string.IsNullOrWhiteSpace(CustomerNameSnapshot) ||
            string.IsNullOrWhiteSpace(CustomerEmailSnapshot) || string.IsNullOrWhiteSpace(ShippingLine1Snapshot))
            throw new SalesException("invalid_quote_transition", "The quote is incomplete and cannot be sent.");
    }

    private static SalesException InvalidTransition() =>
        new("invalid_quote_transition", "The requested quote status transition is not allowed.");

    private static List<QuoteLine> BuildLines(IReadOnlyList<SalesLineRequest>? requests,
        IReadOnlyDictionary<int, ProductSalesReference> products, IEnumerable<QuoteLine>? existingLines = null)
    {
        if (requests is null || requests.Count == 0)
            throw Validation("lines", "At least one line is required.");
        Dictionary<int, QuoteLine> existing = existingLines?.ToDictionary(line => line.ProductId) ?? [];
        HashSet<int> productIds = [];
        var lines = new List<QuoteLine>(requests.Count);
        for (int index = 0; index < requests.Count; index++)
        {
            SalesLineRequest request = requests[index];
            if (!productIds.Add(request.ProductId))
                throw Validation($"lines.{index}.productId", "Product IDs cannot repeat within a transaction.");
            if (!products.TryGetValue(request.ProductId, out ProductSalesReference? product))
                throw Validation($"lines.{index}.productId", "The selected product was not found.");
            if (existing.TryGetValue(request.ProductId, out QuoteLine? old))
            {
                if (!product.IsActive && (old.Quantity != request.Quantity || old.UnitPrice != request.UnitPrice ||
                                          old.DiscountPercent != request.DiscountPercent))
                    throw Validation($"lines.{index}.productId", "An inactive product cannot be newly selected or changed.");
                lines.Add(QuoteLine.Create(request, old.SkuSnapshot, old.ProductNameSnapshot));
            }
            else
            {
                if (!product.IsActive)
                    throw Validation($"lines.{index}.productId", "An inactive product cannot be newly selected or changed.");
                lines.Add(QuoteLine.Create(request, product.Sku, product.Name));
            }
        }
        ValidateAggregate(lines);
        return lines;
    }

    private static void ValidateAggregate(IEnumerable<QuoteLine> lines)
    {
        QuoteLine[] values = lines.ToArray();
        var rules = new DomainValidator();
        rules.Assert(values.Length > 0, "At least one quote line is required.");
        rules.Assert(values.Select(line => line.ProductId).Distinct().Count() == values.Length,
            "Product IDs cannot repeat within a quote.");
        try
        {
            decimal subtotal = values.Aggregate(0m, (sum, line) => checked(sum + line.CalculateSubtotal()));
            decimal discount = values.Aggregate(0m, (sum, line) => checked(sum + line.CalculateDiscount()));
            rules.Assert(subtotal <= MaximumStoredAmount && discount <= MaximumStoredAmount && subtotal - discount <= MaximumStoredAmount,
                "The calculated quote totals are too large.");
        }
        catch (OverflowException)
        {
            rules.AddError("The calculated quote totals are too large.");
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

    private static void ValidateDates(DateOnly quoteDate, DateOnly validUntil)
    {
        if (validUntil < quoteDate)
            throw Validation("validUntil", "Valid until cannot precede quote date.");
    }

    private static void ValidateMutationAudit(int actorId, DateTimeOffset now, DateTimeOffset creationDate)
    {
        var rules = new DomainValidator();
        rules.Assert(actorId > 0, "Quote modification actor ID must be positive.");
        rules.Assert(now != default && now >= creationDate, "Quote modification time cannot precede creation.");
        rules.ThrowIfAny();
    }

    private static string? NormalizeNotes(string? notes)
    {
        string? value = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (value?.Length > 1000)
            throw Validation("notes", "Notes cannot exceed 1,000 characters.");
        return value;
    }

    private static void ValidateHeaderState(string? quoteNumber, int customerId, int addressId,
        DateOnly quoteDate, DateOnly validUntil, int statusId, string? notes, string? accountNumber,
        string? customerName, string? email, string? phone, string? shippingLabel, string? line1,
        string? line2, string? city, string? state, string? postalCode, string? country,
        string? addressTypeCode, DateTimeOffset? deletionDate, int? deletedByUserId)
    {
        var rules = new DomainValidator();
        rules.Assert(quoteNumber?.Trim().Length is >= 1 and <= 20, "Quote number is required and cannot exceed 20 characters.");
        rules.Assert(customerId > 0, "Customer ID must be positive.");
        rules.Assert(addressId > 0, "Shipping address ID must be positive.");
        rules.Assert(validUntil >= quoteDate, "Valid until cannot precede quote date.");
        rules.Assert(Enum.IsDefined((QuoteState)statusId), "Quote status is invalid.");
        rules.Assert(notes is null || notes.Trim().Length <= 1000, "Notes cannot exceed 1,000 characters.");
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
        rules.Assert(deletionDate.HasValue == deletedByUserId.HasValue, "Quote deletion time and actor must be set together.");
        rules.ThrowIfAny();
    }

    private static SalesValidationException Validation(string field, string message) =>
        new(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) { [field] = [message] });

    #endregion
}
