using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;
using BeaconAr.Domain.Sales.Validation;
using Paradigm.Enterprise.Domain.Entities;

namespace BeaconAr.Domain.Sales.Entities;

public partial class Quote
{
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
        SalesDomainValidation.ValidateDates(request.QuoteDate, request.ValidUntil);
        SalesDomainValidation.ValidateCustomerAndAddress(customer, address);
        string? notes = SalesDomainValidation.NormalizeNotes(request.Notes);
        IReadOnlyList<QuoteLine> lines = SalesDomainValidation.BuildQuoteLines(request.Lines, products);
        Quote quote = new()
        {
            QuoteNumber = number,
            CustomerId = customer.Id,
            ShippingAddressId = address.Id,
            QuoteDate = request.QuoteDate,
            ValidUntil = request.ValidUntil,
            StatusId = (int)Sales.QuoteStatus.Draft,
            Notes = notes,
            CreatedByUserId = actorId,
            CreationDate = now,
        };
        quote.ApplySnapshots(customer, address);
        foreach (QuoteLine line in lines)
            quote.QuoteLines.Add(line);
        return quote;
    }

    public IReadOnlyList<QuoteLine> PrepareReplacement(
        QuoteUpdateRequest request,
        CustomerSalesReference customer,
        AddressSalesReference address,
        IReadOnlyDictionary<int, ProductSalesReference> products)
    {
        EnsureDraft("updated");
        SalesDomainValidation.ValidateDates(request.QuoteDate, request.ValidUntil);
        if (request.CustomerId != CustomerId || request.ShippingAddressId != ShippingAddressId)
            SalesDomainValidation.ValidateCustomerAndAddress(customer, address);
        SalesDomainValidation.NormalizeNotes(request.Notes);
        return SalesDomainValidation.BuildQuoteLines(request.Lines, products, QuoteLines);
    }

    public void ApplyReplacement(
        QuoteUpdateRequest request,
        CustomerSalesReference customer,
        AddressSalesReference address,
        int actorId,
        DateTimeOffset now)
    {
        bool referencesChanged = request.CustomerId != CustomerId || request.ShippingAddressId != ShippingAddressId;
        CustomerId = request.CustomerId;
        ShippingAddressId = request.ShippingAddressId;
        QuoteDate = request.QuoteDate;
        ValidUntil = request.ValidUntil;
        Notes = SalesDomainValidation.NormalizeNotes(request.Notes);
        if (referencesChanged)
            ApplySnapshots(customer, address);
        Touch(actorId, now);
    }

    public Sales.QuoteStatus TransitionTo(Sales.QuoteStatus target, int actorId, DateTimeOffset now)
    {
        if (DeletionDate.HasValue || !Enum.IsDefined(target))
            throw InvalidTransition();
        Sales.QuoteStatus current = (Sales.QuoteStatus)StatusId;
        bool allowed = current == Sales.QuoteStatus.Draft && target == Sales.QuoteStatus.Sent ||
                       current == Sales.QuoteStatus.Sent && target is Sales.QuoteStatus.Accepted or Sales.QuoteStatus.Rejected or Sales.QuoteStatus.Expired;
        if (!allowed)
            throw InvalidTransition();
        if (target == Sales.QuoteStatus.Sent)
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
        if (DeletionDate.HasValue || StatusId != (int)Sales.QuoteStatus.Accepted)
            throw new SalesException("quote_not_accepted", "Only an accepted quote can be converted.");
        ValidateComplete();
    }

    #endregion

    #region Private Methods

    partial void ValidateEntity()
    {
        foreach (QuoteLine line in QuoteLines)
            line.Validate();
    }

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
        if (DeletionDate.HasValue || StatusId != (int)Sales.QuoteStatus.Draft)
            throw new SalesException("invalid_quote_transition", $"Only a draft quote can be {action}.");
    }

    private void Touch(int actorId, DateTimeOffset now)
    {
        ModifiedByUserId = actorId;
        ModificationDate = now;
    }

    private void ValidateComplete()
    {
        SalesDomainValidation.ValidateDates(QuoteDate, ValidUntil);
        SalesDomainValidation.ValidateStoredLines(QuoteLines);
        _ = SalesDomainValidation.NormalizeNotes(Notes);
        if (string.IsNullOrWhiteSpace(CustomerAccountNumberSnapshot) || string.IsNullOrWhiteSpace(CustomerNameSnapshot) ||
            string.IsNullOrWhiteSpace(CustomerEmailSnapshot) || string.IsNullOrWhiteSpace(ShippingLine1Snapshot))
            throw new SalesException("invalid_quote_transition", "The quote is incomplete and cannot be sent.");
    }

    private static SalesException InvalidTransition() =>
        new("invalid_quote_transition", "The requested quote status transition is not allowed.");

    #endregion
}
