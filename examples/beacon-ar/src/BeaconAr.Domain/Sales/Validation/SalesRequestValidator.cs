using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;

namespace BeaconAr.Domain.Sales.Validation;

public static class SalesRequestValidator
{
    #region Public Methods

    public static void Validate(QuoteCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTransactionIds(request.CustomerId, request.ShippingAddressId);
        SalesDomainValidation.ValidateDates(request.QuoteDate, request.ValidUntil);
        _ = SalesDomainValidation.NormalizeNotes(request.Notes);
        SalesDomainValidation.ValidateLineSyntax(request.Lines);
    }

    public static void Validate(QuoteUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTransactionIds(request.CustomerId, request.ShippingAddressId);
        SalesDomainValidation.ValidateDates(request.QuoteDate, request.ValidUntil);
        _ = SalesDomainValidation.NormalizeNotes(request.Notes);
        SalesDomainValidation.ValidateLineSyntax(request.Lines);
    }

    public static void Validate(SalesOrderCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateOrder(request.CustomerId, request.ShippingAddressId, request.CarrierId, request.TrackingNumber, request.Lines);
    }

    public static void Validate(SalesOrderUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateOrder(request.CustomerId, request.ShippingAddressId, request.CarrierId, request.TrackingNumber, request.Lines);
    }

    public static void Validate(QuoteSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Dictionary<string, IReadOnlyList<string>> errors = ValidateSearch(request.Search, request.PageNumber, request.PageSize, request.SortField, request.SortDirection,
            request.CustomerId, request.Status.HasValue && !Enum.IsDefined(request.Status.Value),
            "quoteNumber", "quoteDate", "validUntil", "status");
        Throw(errors);
    }

    public static void Validate(SalesOrderSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Dictionary<string, IReadOnlyList<string>> errors = ValidateSearch(request.Search, request.PageNumber, request.PageSize,
            request.SortField, request.SortDirection, request.CustomerId,
            request.Status.HasValue && !Enum.IsDefined(request.Status.Value),
            "orderNumber", "status", "requestedShipDate");
        if (request.SourceQuoteId is <= 0)
            errors["sourceQuoteId"] = ["Source quote ID must be positive."];
        Throw(errors);
    }

    public static void ValidateVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Error("version", "A version is required.");
        if (!VersionTokenCodec.TryDecode(value, out _))
            throw Error("version", "The version must be a canonical SQL Server rowversion token.");
    }

    public static string EncodeVersion(byte[] value) => VersionTokenCodec.Encode(value);

    public static byte[] DecodeVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Error("version", "A version is required.");
        if (!VersionTokenCodec.TryDecode(value, out byte[] decoded))
            throw Error("version", "The version must be a canonical SQL Server rowversion token.");
        return decoded;
    }

    #endregion

    #region Private Methods

    private static Dictionary<string, IReadOnlyList<string>> ValidateSearch(
        string? search,
        int pageNumber,
        int pageSize,
        string? sortField,
        SortDirection direction,
        int? customerId,
        bool invalidStatus,
        params string[] allowedSorts)
    {
        Dictionary<string, IReadOnlyList<string>> errors = new(StringComparer.Ordinal);
        if (search?.Trim().Length > 320)
            errors["search"] = ["Search cannot exceed 320 characters."];
        if (pageNumber <= 0)
            errors["pageNumber"] = ["Page number must be positive."];
        if (pageSize <= 0 || pageSize > 100)
            errors["pageSize"] = ["Page size must be between 1 and 100."];
        if (customerId is <= 0)
            errors["customerId"] = ["Customer ID must be positive."];
        if (!Enum.IsDefined(direction))
            errors["sortDirection"] = ["Sort direction is invalid."];
        if (invalidStatus)
            errors["status"] = ["Status is invalid."];
        if (!string.IsNullOrWhiteSpace(sortField) && !allowedSorts.Contains(sortField.Trim(), StringComparer.OrdinalIgnoreCase))
            errors["sortField"] = ["Sort field is invalid."];
        return errors;
    }

    private static SalesValidationException Error(string field, string message) =>
        new(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) { [field] = [message] });

    private static void Throw(Dictionary<string, IReadOnlyList<string>> errors)
    {
        if (errors.Count > 0)
            throw new SalesValidationException(errors);
    }

    private static void ValidateOrder(
        int customerId,
        int addressId,
        int? carrierId,
        string? trackingNumber,
        IReadOnlyList<SalesLineRequest>? lines)
    {
        ValidateTransactionIds(customerId, addressId);
        if (carrierId is <= 0)
            throw Error("carrierId", "Carrier ID must be positive.");
        _ = SalesDomainValidation.NormalizeTracking(trackingNumber);
        SalesDomainValidation.ValidateLineSyntax(lines);
    }

    private static void ValidateTransactionIds(int customerId, int addressId)
    {
        Dictionary<string, IReadOnlyList<string>> errors = new(StringComparer.Ordinal);
        if (customerId <= 0)
            errors["customerId"] = ["Customer ID must be positive."];
        if (addressId <= 0)
            errors["shippingAddressId"] = ["Shipping address ID must be positive."];
        Throw(errors);
    }

    #endregion
}
