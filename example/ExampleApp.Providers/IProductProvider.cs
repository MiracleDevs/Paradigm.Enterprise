using ExampleApp.Domain.Dtos;
using Paradigm.Enterprise.Providers;

namespace ExampleApp.Providers;

/// <summary>
/// Provider for product management operations
/// </summary>
public interface IProductProvider : IEditProvider<ProductView>
{
    /// <summary>
    /// Get products by category
    /// </summary>
    Task<IEnumerable<ProductView>> GetByCategoryAsync(string category);

    /// <summary>
    /// Get all available products
    /// </summary>
    Task<IEnumerable<ProductView>> GetAvailableProductsAsync();
}