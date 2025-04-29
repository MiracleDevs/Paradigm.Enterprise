using ExampleApp.Data.Contexts;
using ExampleApp.Domain.Dtos;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace ExampleApp.Data.Repositories;

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
        return await this.EntityContext.ProductViews
            .Where(p => p.Category == category)
            .ToListAsync();
    }

    /// <summary>
    /// Get all available products
    /// </summary>
    public async Task<IEnumerable<ProductView>> GetAvailableProductsAsync()
    {
        return await this.EntityContext.ProductViews
            .Where(p => p.IsAvailable && p.StockQuantity > 0)
            .ToListAsync();
    }
}