using InventoryCrud.Data.Inventory.Contexts;
using InventoryCrud.Domain.Inventory.Entities;
using InventoryCrud.Domain.Inventory.Repositories;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace InventoryCrud.Data.Inventory.Repositories;

public class ProductViewRepository : ReadRepositoryBase<ProductView, InventoryDbContext, int>, IProductViewRepository
{
    #region Constructors

    public ProductViewRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

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

    #endregion
}
