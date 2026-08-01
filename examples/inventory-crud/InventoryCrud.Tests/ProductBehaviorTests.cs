using InventoryCrud.Domain.Inventory.Entities;
using Paradigm.Enterprise.Domain.Exceptions;

namespace InventoryCrud.Tests;

[TestClass]
public class ProductBehaviorTests
{
    #region Public Methods

    [TestMethod]
    public void Activation_requires_stock_and_deactivation_owns_the_transition()
    {
        var product = new Product();
        product.UpdateDetails("Keyboard", 80m, "Mechanical", "Accessories", 0);

        Assert.ThrowsExactly<DomainException>(product.Activate);

        product.UpdateDetails("Keyboard", 80m, "Mechanical", "Accessories", 3);
        product.Activate();
        Assert.IsTrue(product.IsAvailable);

        product.Deactivate();
        Assert.IsFalse(product.IsAvailable);
    }

    [TestMethod]
    public void Entity_mapping_routes_availability_through_behavior()
    {
        var product = new Product();
        var available = new ProductView
        {
            Id = 7,
            Name = "Mouse",
            Price = 25m,
            Description = "Wireless",
            Category = "Accessories",
            StockQuantity = 5,
            IsAvailable = true
        };

        var mapped = product.MapFrom(null!, available);

        Assert.AreSame(product, mapped);
        Assert.AreEqual(7, product.Id);
        Assert.IsTrue(product.IsAvailable);

        product.MapFrom(null!, WithAvailability(available, false));
        Assert.IsFalse(product.IsAvailable);
    }

    #endregion

    #region Private Methods

    private static ProductView WithAvailability(ProductView source, bool value) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Price = source.Price,
        Description = source.Description,
        Category = source.Category,
        StockQuantity = source.StockQuantity,
        IsAvailable = value
    };

    #endregion
}
