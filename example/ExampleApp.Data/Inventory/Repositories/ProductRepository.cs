using ExampleApp.Data.Inventory.Contexts;
using ExampleApp.Domain.Inventory.Entities;
using ExampleApp.Domain.Inventory.Repositories;
using Paradigm.Enterprise.Data.Repositories;

namespace ExampleApp.Data.Inventory.Repositories;

/// <summary>
/// Repository implementation for products
/// </summary>
public class ProductRepository : EditRepositoryBase<Product, ApplicationDbContext>, IProductRepository
{
    public ProductRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
