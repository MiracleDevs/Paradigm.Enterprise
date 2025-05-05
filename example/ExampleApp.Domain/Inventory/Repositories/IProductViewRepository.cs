using ExampleApp.Domain.Inventory.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace ExampleApp.Domain.Inventory.Repositories;

public interface IProductViewRepository : IReadRepository<ProductView>
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