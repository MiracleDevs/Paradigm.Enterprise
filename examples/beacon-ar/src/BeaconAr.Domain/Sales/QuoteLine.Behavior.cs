using BeaconAr.Domain.Sales.Contracts;

namespace BeaconAr.Domain.Receivables.Entities;

public partial class QuoteLine
{
    #region Public Methods

    public static QuoteLine Create(SalesLineRequest request, string sku, string productName) => new()
    {
        ProductId = request.ProductId,
        SkuSnapshot = sku,
        ProductNameSnapshot = productName,
        Quantity = request.Quantity,
        UnitPrice = request.UnitPrice,
        DiscountPercent = request.DiscountPercent,
    };

    #endregion
}
