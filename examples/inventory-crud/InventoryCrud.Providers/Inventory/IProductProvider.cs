using InventoryCrud.Domain.Inventory.Entities;
using Paradigm.Enterprise.Providers;

namespace InventoryCrud.Providers.Inventory;

/// <summary>
/// Provider for product management operations
/// </summary>
public interface IProductProvider : IEditProvider<ProductView, int>
{
    /// <summary>
    /// Get products by category
    /// </summary>
    Task<IEnumerable<ProductView>> GetByCategoryAsync(string category);

    /// <summary>
    /// Get all available products
    /// </summary>
    Task<IEnumerable<ProductView>> GetAvailableProductsAsync();
}
