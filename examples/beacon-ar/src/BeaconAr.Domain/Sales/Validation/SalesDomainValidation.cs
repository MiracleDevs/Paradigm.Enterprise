using BeaconAr.Domain.MasterData;
using BeaconAr.Domain.Receivables.Generated;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;

namespace BeaconAr.Domain.Sales.Validation;

internal static class SalesDomainValidation
{
    #region Constants

    private const decimal MaximumStoredAmount = 99_999_999_999_999_999.99m;

    #endregion

    #region Public Methods

    public static string? NormalizeNotes(string? notes)
    {
        string? value = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (value?.Length > 1000)
            throw Error("notes", "Notes cannot exceed 1,000 characters.");
        return value;
    }

    public static string? NormalizeTracking(string? trackingNumber)
    {
        string? value = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim();
        if (value?.Length > 200)
            throw Error("trackingNumber", "Tracking number cannot exceed 200 characters.");
        return value;
    }

    public static void ValidateDates(DateOnly quoteDate, DateOnly validUntil)
    {
        if (validUntil < quoteDate)
            throw Error("validUntil", "Valid until cannot precede quote date.");
    }

    public static void ValidateCustomerAndAddress(CustomerSalesReference customer, AddressSalesReference address)
    {
        if (!customer.IsActive)
            throw new SalesException("reference_inactive", "The selected customer is inactive.");
        if (address.CustomerId != customer.Id ||
            address.AddressTypeCode is not AddressTypeCodes.Shipping and not AddressTypeCodes.Both)
            throw new SalesException("invalid_shipping_address", "The selected address is not an eligible shipping address for the customer.");
    }

    public static IReadOnlyList<QuoteLine> BuildQuoteLines(
        IReadOnlyList<SalesLineRequest>? requests,
        IReadOnlyDictionary<int, ProductSalesReference> products,
        IEnumerable<QuoteLine>? existingLines = null)
    {
        ValidateLineRequests(requests);
        Dictionary<int, QuoteLine> existing = existingLines?.ToDictionary(line => line.ProductId) ?? [];
        return requests!.Select((request, index) =>
        {
            ProductSalesReference product = GetProduct(products, request.ProductId, index);
            if (existing.TryGetValue(request.ProductId, out QuoteLine? old))
            {
                if (!product.IsActive &&
                    (old.Quantity != request.Quantity || old.UnitPrice != request.UnitPrice || old.DiscountPercent != request.DiscountPercent))
                    throw Error($"lines.{index}.productId", "An inactive product cannot be newly selected or changed.");
                return QuoteLine.Create(request, old.SkuSnapshot, old.ProductNameSnapshot);
            }
            if (!product.IsActive)
                throw Error($"lines.{index}.productId", "An inactive product cannot be newly selected or changed.");
            return QuoteLine.Create(request, product.Sku, product.Name);
        }).ToArray();
    }

    public static IReadOnlyList<SalesOrderLine> BuildOrderLines(
        IReadOnlyList<SalesLineRequest>? requests,
        IReadOnlyDictionary<int, ProductSalesReference> products,
        IEnumerable<SalesOrderLine>? existingLines = null)
    {
        ValidateLineRequests(requests);
        Dictionary<int, SalesOrderLine> existing = existingLines?.ToDictionary(line => line.ProductId) ?? [];
        return requests!.Select((request, index) =>
        {
            ProductSalesReference product = GetProduct(products, request.ProductId, index);
            if (existing.TryGetValue(request.ProductId, out SalesOrderLine? old))
            {
                if (!product.IsActive &&
                    (old.Quantity != request.Quantity || old.UnitPrice != request.UnitPrice || old.DiscountPercent != request.DiscountPercent))
                    throw Error($"lines.{index}.productId", "An inactive product cannot be newly selected or changed.");
                return SalesOrderLine.Create(request, old.SkuSnapshot, old.ProductNameSnapshot);
            }
            if (!product.IsActive)
                throw Error($"lines.{index}.productId", "An inactive product cannot be newly selected or changed.");
            return SalesOrderLine.Create(request, product.Sku, product.Name);
        }).ToArray();
    }

    public static void ValidateStoredLines<TLine>(ICollection<TLine> lines)
    {
        if (lines.Count == 0)
            throw Error("lines", "At least one line is required.");
    }

    #endregion

    #region Private Methods

    internal static void ValidateLineSyntax(IReadOnlyList<SalesLineRequest>? lines) => ValidateLineRequests(lines);

    internal static void ValidateStoredPricing(IEnumerable<QuoteLine> lines) => ValidateLineRequests(lines
        .Select(line => new SalesLineRequest(line.ProductId, line.Quantity, line.UnitPrice, line.DiscountPercent))
        .ToArray());

    private static ProductSalesReference GetProduct(
        IReadOnlyDictionary<int, ProductSalesReference> products,
        int productId,
        int index)
    {
        if (!products.TryGetValue(productId, out ProductSalesReference? product))
            throw Error($"lines.{index}.productId", "The selected product was not found.");
        return product;
    }

    private static void ValidateLineRequests(IReadOnlyList<SalesLineRequest>? lines)
    {
        if (lines is null || lines.Count == 0)
            throw Error("lines", "At least one line is required.");

        Dictionary<string, IReadOnlyList<string>> errors = new(StringComparer.Ordinal);
        HashSet<int> products = [];
        decimal aggregateSubtotal = 0m;
        decimal aggregateDiscount = 0m;
        decimal aggregateTotal = 0m;
        for (int index = 0; index < lines.Count; index++)
        {
            SalesLineRequest line = lines[index];
            string prefix = $"lines.{index}";
            if (line.ProductId <= 0)
                errors[$"{prefix}.productId"] = ["Product ID must be positive."];
            else if (!products.Add(line.ProductId))
                errors[$"{prefix}.productId"] = ["Product IDs cannot repeat within a transaction."];
            if (line.Quantity <= 0)
                errors[$"{prefix}.quantity"] = ["Quantity must be greater than zero."];
            if (line.UnitPrice < 0 || GetScale(line.UnitPrice) > 4)
                errors[$"{prefix}.unitPrice"] = ["Unit price must be non-negative with at most four decimal places."];
            if (line.DiscountPercent is < 0 or > 100 || GetScale(line.DiscountPercent) > 2)
                errors[$"{prefix}.discountPercent"] = ["Discount percent must be between 0 and 100 with at most two decimal places."];
            bool pricingInputIsValid = line.Quantity > 0 && line.UnitPrice >= 0 && GetScale(line.UnitPrice) <= 4 &&
                                       line.DiscountPercent is >= 0 and <= 100 && GetScale(line.DiscountPercent) <= 2;
            if (!pricingInputIsValid)
                continue;
            try
            {
                decimal subtotal = MonetaryRounding.Round(line.Quantity * line.UnitPrice);
                decimal discount = MonetaryRounding.Round(subtotal * line.DiscountPercent / 100m);
                decimal total = subtotal - discount;
                if (subtotal > MaximumStoredAmount || discount > MaximumStoredAmount || total > MaximumStoredAmount)
                {
                    errors[$"{prefix}.unitPrice"] = ["The calculated line amount is too large."];
                    continue;
                }
                aggregateSubtotal = checked(aggregateSubtotal + subtotal);
                aggregateDiscount = checked(aggregateDiscount + discount);
                aggregateTotal = checked(aggregateTotal + total);
            }
            catch (OverflowException)
            {
                errors[$"{prefix}.unitPrice"] = ["The calculated line amount is too large."];
            }
        }
        if (aggregateSubtotal > MaximumStoredAmount || aggregateDiscount > MaximumStoredAmount || aggregateTotal > MaximumStoredAmount)
            errors["lines"] = ["The calculated transaction totals are too large."];
        if (errors.Count > 0)
            throw new SalesValidationException(errors);
    }

    private static int GetScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0x7F;

    private static SalesValidationException Error(string field, string message) =>
        new(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) { [field] = [message] });

    #endregion
}
