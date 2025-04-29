using ExampleApp.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace ExampleApp.Data.Repositories;

/// <summary>
/// Repository implementation for products
/// </summary>
public interface IProductRepository : IEditRepository<Product>
{
} 
