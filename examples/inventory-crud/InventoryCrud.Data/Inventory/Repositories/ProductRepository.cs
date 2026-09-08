using InventoryCrud.Data.Inventory.Contexts;
using InventoryCrud.Domain.Inventory.Entities;
using InventoryCrud.Domain.Inventory.Repositories;
using Paradigm.Enterprise.Data.Repositories;

namespace InventoryCrud.Data.Inventory.Repositories;

/// <summary>
/// Repository implementation for products
/// </summary>
public class ProductRepository : EditRepositoryBase<Product, InventoryDbContext, int>, IProductRepository
{
    #region Constructors

    public ProductRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    #endregion
}
