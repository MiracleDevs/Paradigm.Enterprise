using ExampleApp.Domain.Inventory.Entities;
using ExampleApp.Domain.Inventory.Repositories;
using ExampleApp.Interfaces.Inventory;
using Paradigm.Enterprise.Providers;

namespace ExampleApp.Providers.Inventory;

/// <summary>
/// Provider for product management operations
/// </summary>
public class ProductProvider : EditProviderBase<IProduct, Product, ProductView, IProductRepository, IProductViewRepository>, IProductProvider
{
    public ProductProvider(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

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
}
