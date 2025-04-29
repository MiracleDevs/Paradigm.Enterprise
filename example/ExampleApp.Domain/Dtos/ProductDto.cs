namespace ExampleApp.Domain.Dtos;

/// <summary>
/// Data transfer object for product creation/editing
/// </summary>
public class ProductEditDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
}
