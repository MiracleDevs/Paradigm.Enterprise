using InventoryCrud.Domain.Inventory.Entities;
using InventoryCrud.Domain.Inventory.Repositories;
using InventoryCrud.Interfaces.Inventory;
using Paradigm.Enterprise.Providers;

namespace InventoryCrud.Providers.Inventory;

/// <summary>
/// Provider for product management operations
/// </summary>
public class ProductProvider : EditProviderBase<IProduct, Product, ProductView, IProductRepository, IProductViewRepository, int>, IProductProvider
{
    #region Constructors

    public ProductProvider(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Get products by category
    /// </summary>
    public async Task<IEnumerable<ProductView>> GetByCategoryAsync(string category)
    {
        return await ViewRepository.FindByCategoryAsync(category);
    }

    /// <summary>
    /// Get all available products
    /// </summary>
    public async Task<IEnumerable<ProductView>> GetAvailableProductsAsync()
    {
        return await ViewRepository.GetAvailableProductsAsync();
    }

    #endregion
}
