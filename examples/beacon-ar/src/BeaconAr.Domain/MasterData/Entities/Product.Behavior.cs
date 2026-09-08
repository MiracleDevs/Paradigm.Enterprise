using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Interfaces.MasterData.Entities;
using Paradigm.Enterprise.Domain.Exceptions;

namespace BeaconAr.Domain.MasterData.Entities;

public partial class Product
{
    #region Nested Types

    private sealed record ProposedState(
        string Sku,
        string Name,
        string Category,
        decimal UnitPrice,
        int StockQuantity,
        string? ThumbnailUrl,
        bool IsActive);

    #endregion

    #region Constants

    private const decimal MaximumUnitPrice = 999999999999999.9999m;

    #endregion

    #region Public Methods

    public static Product Create(ProductCreateRequest request, int userId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        ProposedState state = Normalize(request.Sku, request.Name, request.Category, request.UnitPrice,
            request.StockQuantity, request.ThumbnailUrl, request.IsActive);
        ValidateState(state);
        return new Product
        {
            Sku = state.Sku,
            Name = state.Name,
            Category = state.Category,
            UnitPrice = state.UnitPrice,
            StockQuantity = state.StockQuantity,
            ThumbnailUrl = state.ThumbnailUrl,
            IsActive = state.IsActive,
            CreatedByUserId = userId,
            CreationDate = now,
        };
    }

    public void Replace(ProductUpdateRequest request, int userId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        ProposedState state = Normalize(request.Sku, request.Name, request.Category, request.UnitPrice,
            request.StockQuantity, request.ThumbnailUrl, request.IsActive);
        ValidateState(state);
        Sku = state.Sku;
        Name = state.Name;
        Category = state.Category;
        UnitPrice = state.UnitPrice;
        StockQuantity = state.StockQuantity;
        ThumbnailUrl = state.ThumbnailUrl;
        IsActive = state.IsActive;
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

    #region Private Methods

    partial void ValidateEntity() => ValidateState(Normalize(Sku, Name, Category, UnitPrice, StockQuantity, ThumbnailUrl, IsActive));

    partial void BeforeMap(IProduct model)
    {
        _ = Id;
        ValidateState(Normalize(model.Sku, model.Name, model.Category, model.UnitPrice, model.StockQuantity,
            model.ThumbnailUrl, model.IsActive));
    }

    partial void AfterMap(IProduct model)
    {
        ProposedState state = Normalize(Sku, Name, Category, UnitPrice, StockQuantity, ThumbnailUrl, IsActive);
        Sku = state.Sku;
        Name = state.Name;
        Category = state.Category;
        ThumbnailUrl = state.ThumbnailUrl;
    }

    private static ProposedState Normalize(string? sku, string? name, string? category, decimal unitPrice,
        int stockQuantity, string? thumbnailUrl, bool isActive) => new(
        sku?.Trim() ?? string.Empty,
        name?.Trim() ?? string.Empty,
        category?.Trim() ?? string.Empty,
        unitPrice,
        stockQuantity,
        string.IsNullOrWhiteSpace(thumbnailUrl) ? null : thumbnailUrl.Trim(),
        isActive);

    private static void ValidateState(ProposedState state)
    {
        var rules = new DomainValidator();
        rules.Assert(state.Sku.Length is >= 2 and <= 32, "SKU must be between 2 and 32 characters.");
        rules.Assert(state.Name.Length is >= 2 and <= 120, "Name must be between 2 and 120 characters.");
        rules.Assert(state.Category.Length is >= 1 and <= 120, "Category is required and cannot exceed 120 characters.");
        rules.Assert(state.UnitPrice > 0 && state.UnitPrice <= MaximumUnitPrice && HasScaleAtMost(state.UnitPrice, 4),
            "Unit price must be greater than zero, fit decimal(19,4), and have no more than four fractional digits.");
        rules.Assert(state.StockQuantity >= 0, "Stock quantity cannot be negative.");
        rules.Assert(state.ThumbnailUrl is null || state.ThumbnailUrl.Length <= 2048 && IsSafeThumbnail(state.ThumbnailUrl),
            "Thumbnail URL must be a safe absolute HTTP or HTTPS URL.");
        rules.ThrowIfAny();
    }

    private static bool HasScaleAtMost(decimal value, int scale) => value == decimal.Round(value, scale);

    private static bool IsSafeThumbnail(string value)
    {
        if (value.Any(char.IsControl) || value.Contains('{') || value.Contains('}') ||
            !Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) || string.IsNullOrWhiteSpace(uri.Host))
            return false;
        return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) && string.IsNullOrEmpty(uri.UserInfo);
    }

    #endregion
}
