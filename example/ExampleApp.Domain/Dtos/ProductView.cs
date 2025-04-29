using ExampleApp.Interfaces;
using Paradigm.Enterprise.Domain.Entities;

namespace ExampleApp.Domain.Dtos;

/// <summary>
/// Data transfer object for product viewing
/// </summary>
public class ProductView: EntityBase, IProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
} 