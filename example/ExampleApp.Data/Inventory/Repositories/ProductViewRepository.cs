using ExampleApp.Data.Inventory.Contexts;
using ExampleApp.Domain.Inventory.Entities;
using ExampleApp.Domain.Inventory.Repositories;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace ExampleApp.Data.Inventory.Repositories;

public class ProductViewRepository : ReadRepositoryBase<ProductView, ApplicationDbContext>, IProductViewRepository
{
    public ProductViewRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <summary>
    /// Find products by category
    /// </summary>
    public async Task<IEnumerable<ProductView>> FindByCategoryAsync(string category)
    {
        return await EntityContext.ProductViews
            .Where(p => p.Category == category)
            .ToListAsync();
    }

    /// <summary>
    /// Get all available products
    /// </summary>
    public async Task<IEnumerable<ProductView>> GetAvailableProductsAsync()
    {
        return await EntityContext.ProductViews
            .Where(p => p.IsAvailable && p.StockQuantity > 0)
            .ToListAsync();
    }
}