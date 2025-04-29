using Paradigm.Enterprise.Interfaces;

namespace ExampleApp.Interfaces;

/// <summary>
/// Represents a product entity interface in our example application
/// </summary>
public interface IProduct: IEntity
{
    int Id { get; set; }
    DateTime CreatedDate { get; set; }
    DateTime ModifiedDate { get; set; }
    string Name { get; set; }
    decimal Price { get; set; }
    string Description { get; set; }
    string Category { get; set; }
    int StockQuantity { get; set; }
    bool IsAvailable { get; set; }
}
