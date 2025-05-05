using ExampleApp.Domain.Inventory.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace ExampleApp.Domain.Inventory.Repositories;

/// <summary>
/// Repository implementation for products
/// </summary>
public interface IProductRepository : IEditRepository<Product>
{
}
