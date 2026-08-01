using System.Net.Mail;
using BeaconAr.Domain.MasterData.Contracts;

namespace BeaconAr.Domain.MasterData.Validation;

public static class MasterDataRequestValidator
{
    #region Constants

    private const decimal MaximumCreditLimit = 99999999999999999.99m;
    private const decimal MaximumUnitPrice = 999999999999999.9999m;

    #endregion

    #region Public Methods

    public static ProductCreateRequest Normalize(ProductCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ProductCreateRequest normalized = request with
        {
            Sku = Required(request.Sku),
            Name = Required(request.Name),
            Category = Required(request.Category),
            ThumbnailUrl = Optional(request.ThumbnailUrl),
        };
        ValidateProduct(normalized.Sku, normalized.Name, normalized.Category, normalized.UnitPrice,
            normalized.StockQuantity, normalized.ThumbnailUrl);
        return normalized;
    }

    public static ProductUpdateRequest Normalize(ProductUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ProductUpdateRequest normalized = request with
        {
            Sku = Required(request.Sku),
            Name = Required(request.Name),
            Category = Required(request.Category),
            ThumbnailUrl = Optional(request.ThumbnailUrl),
        };
        ValidateProduct(normalized.Sku, normalized.Name, normalized.Category, normalized.UnitPrice,
            normalized.StockQuantity, normalized.ThumbnailUrl);
        return normalized;
    }

    public static CustomerCreateRequest Normalize(CustomerCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        CustomerCreateRequest normalized = request with
        {
            AccountNumber = Required(request.AccountNumber),
            Name = Required(request.Name),
            Email = Required(request.Email),
            Phone = Optional(request.Phone),
        };
        ValidateCustomer(normalized.AccountNumber, normalized.Name, normalized.Email, normalized.Phone,
            normalized.CreditLimit, normalized.PaymentTermsDays);
        return normalized;
    }

    public static CustomerUpdateRequest Normalize(CustomerUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        CustomerUpdateRequest normalized = request with
        {
            AccountNumber = Required(request.AccountNumber),
            Name = Required(request.Name),
            Email = Required(request.Email),
            Phone = Optional(request.Phone),
        };
        ValidateCustomer(normalized.AccountNumber, normalized.Name, normalized.Email, normalized.Phone,
            normalized.CreditLimit, normalized.PaymentTermsDays);
        return normalized;
    }

    public static AddressCreateRequest Normalize(AddressCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        AddressCreateRequest normalized = request with
        {
            Type = Required(request.Type).ToLowerInvariant(),
            Label = Required(request.Label),
            Line1 = Required(request.Line1),
            Line2 = Optional(request.Line2),
            City = Required(request.City),
            State = Optional(request.State),
            PostalCode = Required(request.PostalCode),
            Country = Required(request.Country).ToUpperInvariant(),
        };
        ValidateAddress(normalized.CustomerId, normalized.Type, normalized.Label, normalized.Line1,
            normalized.Line2, normalized.City, normalized.State, normalized.PostalCode, normalized.Country,
            normalized.DefaultBilling, normalized.DefaultShipping);
        return normalized;
    }

    public static AddressUpdateRequest Normalize(AddressUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        AddressUpdateRequest normalized = request with
        {
            Type = Required(request.Type).ToLowerInvariant(),
            Label = Required(request.Label),
            Line1 = Required(request.Line1),
            Line2 = Optional(request.Line2),
            City = Required(request.City),
            State = Optional(request.State),
            PostalCode = Required(request.PostalCode),
            Country = Required(request.Country).ToUpperInvariant(),
        };
        ValidateAddress(normalized.CustomerId, normalized.Type, normalized.Label, normalized.Line1,
            normalized.Line2, normalized.City, normalized.State, normalized.PostalCode, normalized.Country,
            normalized.DefaultBilling, normalized.DefaultShipping);
        return normalized;
    }

    public static CarrierCreateRequest Normalize(CarrierCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        CarrierCreateRequest normalized = request with
        {
            Code = Required(request.Code),
            Name = Required(request.Name),
            ServiceLevel = Required(request.ServiceLevel),
            TrackingUrlTemplate = Optional(request.TrackingUrlTemplate),
        };
        ValidateCarrier(normalized.Code, normalized.Name, normalized.ServiceLevel, normalized.TrackingUrlTemplate);
        return normalized;
    }

    public static CarrierUpdateRequest Normalize(CarrierUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        CarrierUpdateRequest normalized = request with
        {
            Code = Required(request.Code),
            Name = Required(request.Name),
            ServiceLevel = Required(request.ServiceLevel),
            TrackingUrlTemplate = Optional(request.TrackingUrlTemplate),
        };
        ValidateCarrier(normalized.Code, normalized.Name, normalized.ServiceLevel, normalized.TrackingUrlTemplate);
        return normalized;
    }

    public static void ValidateSearch(MasterDataSearchRequest request, params string[] allowedSortFields)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new ValidationErrorBuilder();
        errors.Assert(request.PageNumber > 0, "pageNumber", "Page number must be greater than zero.");
        errors.Assert(request.PageSize is > 0 and <= 100, "pageSize", "Page size must be between 1 and 100.");
        errors.Assert(Enum.IsDefined(request.SortDirection), "sortDirection", "Sort direction must be asc or desc.");
        string? search = Optional(request.Search);
        errors.Assert(search is null || search.Length <= 320, "search", "Search cannot exceed 320 characters.");

        string sortField = string.IsNullOrWhiteSpace(request.SortField) ? "id" : request.SortField.Trim();
        errors.Assert(allowedSortFields.Contains(sortField, StringComparer.OrdinalIgnoreCase), "sortField", "Sort field is not supported.");

        if (request is AddressSearchRequest address)
        {
            errors.Assert(address.CustomerId is null or > 0, "customerId", "Customer ID must be greater than zero.");
            string? type = Optional(address.Type);
            errors.Assert(type is null || type.Length <= 32 && !type.Any(char.IsControl),
                "type", "Address type cannot exceed 32 characters or contain control characters.");
            errors.Assert(!address.Usage.HasValue || Enum.IsDefined(address.Usage.Value),
                "usage", "Address usage must be billing or shipping.");
        }

        errors.ThrowIfAny();
    }

    #endregion

    #region Private Methods

    private static string Required(string? value) => value?.Trim() ?? string.Empty;

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateProduct(string? sku, string? name, string? category, decimal unitPrice,
        int stockQuantity, string? thumbnailUrl)
    {
        var errors = new ValidationErrorBuilder();
        errors.Assert(sku is { Length: >= 2 and <= 32 }, "sku", "SKU must be between 2 and 32 characters.");
        errors.Assert(name is { Length: >= 2 and <= 120 }, "name", "Name must be between 2 and 120 characters.");
        errors.Assert(category is { Length: >= 1 and <= 120 }, "category", "Category is required and cannot exceed 120 characters.");
        errors.Assert(unitPrice > 0, "unitPrice", "Unit price must be greater than zero.");
        errors.Assert(unitPrice <= MaximumUnitPrice && HasScaleAtMost(unitPrice, 4), "unitPrice",
            "Unit price must fit decimal(19,4) and have no more than four fractional digits.");
        errors.Assert(stockQuantity >= 0, "stockQuantity", "Stock quantity cannot be negative.");
        errors.Assert(thumbnailUrl is null || thumbnailUrl.Length <= 2048 && SafeUrlValidator.IsSafeThumbnail(thumbnailUrl),
            "thumbnailUrl", "Thumbnail URL must be a safe absolute HTTP or HTTPS URL.");
        errors.ThrowIfAny();
    }

    private static void ValidateCustomer(string? accountNumber, string? name, string? email, string? phone,
        decimal creditLimit, short paymentTermsDays)
    {
        var errors = new ValidationErrorBuilder();
        errors.Assert(accountNumber is { Length: >= 1 and <= 50 }, "accountNumber", "Account number is required and cannot exceed 50 characters.");
        errors.Assert(name is { Length: >= 1 and <= 120 }, "name", "Name is required and cannot exceed 120 characters.");
        errors.Assert(email is { Length: >= 1 and <= 320 } && IsValidEmail(email), "email", "Email must be syntactically valid and cannot exceed 320 characters.");
        errors.Assert(phone is null || phone.Length <= 50, "phone", "Phone cannot exceed 50 characters.");
        errors.Assert(creditLimit >= 0, "creditLimit", "Credit limit cannot be negative.");
        errors.Assert(creditLimit <= MaximumCreditLimit && HasScaleAtMost(creditLimit, 2), "creditLimit",
            "Credit limit must fit decimal(19,2) and have no more than two fractional digits.");
        errors.Assert(paymentTermsDays is 0 or 15 or 30 or 45 or 60, "paymentTermsDays", "Payment terms must be 0, 15, 30, 45, or 60 days.");
        errors.ThrowIfAny();
    }

    private static void ValidateAddress(int customerId, string? type, string? label, string? line1, string? line2,
        string? city, string? state, string? postalCode, string? country, bool defaultBilling, bool defaultShipping)
    {
        var errors = new ValidationErrorBuilder();
        errors.Assert(customerId > 0, "customerId", "Customer ID must be greater than zero.");
        errors.Assert(type is AddressTypeCodes.Billing or AddressTypeCodes.Shipping or AddressTypeCodes.Both,
            "type", "Address type must be billing, shipping, or both.");
        errors.Assert(label is { Length: >= 1 and <= 120 }, "label", "Label is required and cannot exceed 120 characters.");
        errors.Assert(line1 is { Length: >= 1 and <= 200 }, "line1", "Address line 1 is required and cannot exceed 200 characters.");
        errors.Assert(line2 is null || line2.Length <= 200, "line2", "Address line 2 cannot exceed 200 characters.");
        errors.Assert(city is { Length: >= 1 and <= 120 }, "city", "City is required and cannot exceed 120 characters.");
        errors.Assert(state is null || state.Length <= 120, "state", "State cannot exceed 120 characters.");
        errors.Assert(postalCode is { Length: >= 1 and <= 32 }, "postalCode", "Postal code is required and cannot exceed 32 characters.");
        errors.Assert(country is { Length: 2 } && country.All(character => character is >= 'A' and <= 'Z'),
            "country", "Country must be a two-letter code.");
        errors.Assert(!defaultBilling || type is AddressTypeCodes.Billing or AddressTypeCodes.Both,
            "defaultBilling", "A billing default must have billing or both type.");
        errors.Assert(!defaultShipping || type is AddressTypeCodes.Shipping or AddressTypeCodes.Both,
            "defaultShipping", "A shipping default must have shipping or both type.");
        errors.ThrowIfAny();
    }

    private static void ValidateCarrier(string? code, string? name, string? serviceLevel, string? trackingUrlTemplate)
    {
        var errors = new ValidationErrorBuilder();
        errors.Assert(code is { Length: >= 1 and <= 32 }, "code", "Code is required and cannot exceed 32 characters.");
        errors.Assert(name is { Length: >= 1 and <= 120 }, "name", "Name is required and cannot exceed 120 characters.");
        errors.Assert(serviceLevel is { Length: >= 1 and <= 120 }, "serviceLevel", "Service level is required and cannot exceed 120 characters.");
        errors.Assert(trackingUrlTemplate is null || trackingUrlTemplate.Length <= 2048 && SafeUrlValidator.IsSafeTrackingTemplate(trackingUrlTemplate),
            "trackingUrlTemplate", "Tracking URL template must be a safe absolute HTTPS URL with only the optional {trackingNumber} placeholder.");
        errors.ThrowIfAny();
    }

    private static bool IsValidEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace))
            return false;

        try
        {
            return new MailAddress(value).Address == value;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool HasScaleAtMost(decimal value, int scale) => value == decimal.Round(value, scale);

    #endregion
}
