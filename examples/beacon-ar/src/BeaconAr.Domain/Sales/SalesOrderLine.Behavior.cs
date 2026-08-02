using BeaconAr.Domain.Sales.Contracts;

namespace BeaconAr.Domain.Receivables.Entities;

public partial class SalesOrderLine
{
    #region Public Methods

    public static SalesOrderLine Create(SalesLineRequest request, string sku, string productName) => new()
    {
        ProductId = request.ProductId,
        SkuSnapshot = sku,
        ProductNameSnapshot = productName,
        Quantity = request.Quantity,
        UnitPrice = request.UnitPrice,
        DiscountPercent = request.DiscountPercent,
    };

    public static SalesOrderLine Copy(QuoteLine source) => new()
    {
        ProductId = source.ProductId,
        SkuSnapshot = source.SkuSnapshot,
        ProductNameSnapshot = source.ProductNameSnapshot,
        Quantity = source.Quantity,
        UnitPrice = source.UnitPrice,
        DiscountPercent = source.DiscountPercent,
    };

    #endregion
}
