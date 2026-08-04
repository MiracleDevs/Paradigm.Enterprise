using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Interfaces.Sales.Entities;
using Paradigm.Enterprise.Domain.Exceptions;

namespace BeaconAr.Domain.Sales.Entities;

public partial class SalesOrderLine
{
    #region Constants

    private const decimal MaximumStoredAmount = 99_999_999_999_999_999.99m;
    private const decimal MaximumUnitPrice = 999_999_999_999_999.9999m;

    #endregion

    #region Public Methods

    public static SalesOrderLine Create(SalesLineRequest request, string sku, string productName)
    {
        ArgumentNullException.ThrowIfNull(request);
        var value = new SalesOrderLine
        {
            ProductId = request.ProductId,
            SkuSnapshot = sku?.Trim() ?? string.Empty,
            ProductNameSnapshot = productName?.Trim() ?? string.Empty,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            DiscountPercent = request.DiscountPercent,
        };
        value.Validate();
        return value;
    }

    public static SalesOrderLine Copy(QuoteLine source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var value = new SalesOrderLine
        {
            ProductId = source.ProductId,
            SkuSnapshot = source.SkuSnapshot,
            ProductNameSnapshot = source.ProductNameSnapshot,
            Quantity = source.Quantity,
            UnitPrice = source.UnitPrice,
            DiscountPercent = source.DiscountPercent,
        };
        value.Validate();
        return value;
    }

    public decimal CalculateSubtotal() => MonetaryRounding.Round(checked(Quantity * UnitPrice));

    public decimal CalculateDiscount()
    {
        decimal subtotal = CalculateSubtotal();
        return MonetaryRounding.Round(checked(subtotal * DiscountPercent / 100m));
    }

    #endregion

    #region Private Methods

    partial void ValidateEntity() => ValidateState(ProductId, SkuSnapshot, ProductNameSnapshot, Quantity, UnitPrice, DiscountPercent);

    partial void BeforeMap(ISalesOrderLine model)
    {
        _ = Id;
        ValidateState(model.ProductId, model.SkuSnapshot, model.ProductNameSnapshot, model.Quantity,
            model.UnitPrice, model.DiscountPercent);
    }

    private static void ValidateState(int productId, string? sku, string? productName, int quantity,
        decimal unitPrice, decimal discountPercent)
    {
        var rules = new DomainValidator();
        rules.Assert(productId > 0, "Product ID must be positive.");
        rules.Assert(sku?.Trim().Length is >= 1 and <= 32, "SKU snapshot is required and cannot exceed 32 characters.");
        rules.Assert(productName?.Trim().Length is >= 1 and <= 120, "Product name snapshot is required and cannot exceed 120 characters.");
        rules.Assert(quantity > 0, "Quantity must be greater than zero.");
        rules.Assert(unitPrice >= 0 && unitPrice <= MaximumUnitPrice && GetScale(unitPrice) <= 4,
            "Unit price must be non-negative, fit decimal(19,4), and have at most four decimal places.");
        rules.Assert(discountPercent is >= 0 and <= 100 && GetScale(discountPercent) <= 2,
            "Discount percent must be between 0 and 100 with at most two decimal places.");

        if (quantity > 0 && unitPrice >= 0 && unitPrice <= MaximumUnitPrice && GetScale(unitPrice) <= 4 &&
            discountPercent is >= 0 and <= 100 && GetScale(discountPercent) <= 2)
        {
            try
            {
                decimal subtotal = MonetaryRounding.Round(checked(quantity * unitPrice));
                decimal discount = MonetaryRounding.Round(checked(subtotal * discountPercent / 100m));
                rules.Assert(subtotal <= MaximumStoredAmount && discount <= MaximumStoredAmount && subtotal - discount <= MaximumStoredAmount,
                    "The calculated line amount is too large.");
            }
            catch (OverflowException)
            {
                rules.AddError("The calculated line amount is too large.");
            }
        }

        rules.ThrowIfAny();
    }

    private static int GetScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0x7F;

    #endregion
}
