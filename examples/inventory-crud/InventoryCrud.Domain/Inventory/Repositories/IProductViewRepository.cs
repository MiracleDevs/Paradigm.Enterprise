using InventoryCrud.Domain.Inventory.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace InventoryCrud.Domain.Inventory.Repositories;

public interface IProductViewRepository : IReadRepository<ProductView, int>
{
    /// <summary>
    /// Find products by category
    /// </summary>
    Task<IEnumerable<ProductView>> FindByCategoryAsync(string category);


    /// <summary>
    /// Get all available products
    /// </summary>
    Task<IEnumerable<ProductView>> GetAvailableProductsAsync();
}
