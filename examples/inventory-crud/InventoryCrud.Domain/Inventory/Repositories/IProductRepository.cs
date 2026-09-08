using InventoryCrud.Domain.Inventory.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace InventoryCrud.Domain.Inventory.Repositories;

/// <summary>
/// Repository implementation for products
/// </summary>
public interface IProductRepository : IEditRepository<Product, int>
{
}
