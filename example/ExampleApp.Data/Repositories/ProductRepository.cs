using ExampleApp.Data.Contexts;
using ExampleApp.Domain.Entities;
using Paradigm.Enterprise.Data.Repositories;

namespace ExampleApp.Data.Repositories;

/// <summary>
/// Repository implementation for products
/// </summary>
public class ProductRepository : EditRepositoryBase<Product, ApplicationDbContext>, IProductRepository
{
    public ProductRepository(IServiceProvider serviceProvider): base(serviceProvider)
    {
    }
} 
