using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Validation;

namespace BeaconAr.Domain.Receivables.Generated;

public partial class Product
{
    #region Public Methods

    public static Product Create(ProductCreateRequest request, int userId, DateTimeOffset now)
    {
        ProductCreateRequest value = MasterDataRequestValidator.Normalize(request);
        return new Product
        {
            Sku = value.Sku!,
            Name = value.Name!,
            Category = value.Category!,
            UnitPrice = value.UnitPrice,
            StockQuantity = value.StockQuantity,
            ThumbnailUrl = value.ThumbnailUrl,
            IsActive = value.IsActive,
            CreatedByUserId = userId,
            CreationDate = now,
        };
    }

    public void Replace(ProductUpdateRequest request, int userId, DateTimeOffset now)
    {
        ProductUpdateRequest value = MasterDataRequestValidator.Normalize(request);
        Sku = value.Sku!;
        Name = value.Name!;
        Category = value.Category!;
        UnitPrice = value.UnitPrice;
        StockQuantity = value.StockQuantity;
        ThumbnailUrl = value.ThumbnailUrl;
        IsActive = value.IsActive;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    public void Activate(int userId, DateTimeOffset now)
    {
        IsActive = true;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    public void Deactivate(int userId, DateTimeOffset now)
    {
        IsActive = false;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    #endregion
}
