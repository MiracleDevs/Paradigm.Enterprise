using Paradigm.Enterprise.Interfaces;

namespace ExampleApp.Interfaces.Inventory;

/// <summary>
/// Represents a product entity interface in our example application
/// </summary>
public interface IProduct : IEntity<int>
{
    DateTime CreatedDate { get; }
    DateTime ModifiedDate { get; }
    string Name { get; }
    decimal Price { get; }
    string Description { get; }
    string Category { get; }
    int StockQuantity { get; }
    bool IsAvailable { get; }
}
