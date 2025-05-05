using System.ComponentModel.DataAnnotations;
using ExampleApp.Interfaces.Inventory;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Exceptions;

namespace ExampleApp.Domain.Inventory.Entities;

/// <summary>
/// Product entity implementation with validation logic
/// </summary>
public class Product : EntityBase<IProduct, Product, ProductView>, IProduct
{
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    [Required(ErrorMessage = "Product name is required")]
    [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 10000, ErrorMessage = "Price must be greater than 0 and less than 10,000")]
    public decimal Price { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required")]
    public string Category { get; set; } = string.Empty;

    [Range(0, 10000, ErrorMessage = "Stock quantity must be between 0 and 10,000")]
    public int StockQuantity { get; set; }

    public bool IsAvailable { get; set; }

    public override Product? MapFrom(IServiceProvider serviceProvider, IProduct model)
    {
        this.Id = model.Id;
        this.Name = model.Name;
        this.Price = model.Price;
        this.Description = model.Description;
        this.Category = model.Category;
        this.StockQuantity = model.StockQuantity;
        this.IsAvailable = model.IsAvailable;
        return this;
    }

    public override ProductView MapTo(IServiceProvider serviceProvider)
    {
        var view = serviceProvider.GetRequiredService<ProductView>();
        view.Id = this.Id;
        view.Name = this.Name;
        view.Price = this.Price;
        view.Description = this.Description;
        view.Category = this.Category;
        view.StockQuantity = this.StockQuantity;
        return view;
    }

    public override void Validate()
    {
        var validator = new DomainValidator();
        validator.Assert(!string.IsNullOrWhiteSpace(this.Name), "Name is required");
        validator.Assert(this.Price > 0, "Price must be greater than 0");
        validator.Assert(this.StockQuantity >= 0, "Stock quantity must be greater than or equal to 0");
        validator.Assert(!string.IsNullOrWhiteSpace(this.Category), "Category is required");
        validator.Assert(this.Description.Length <= 500, "Description cannot exceed 500 characters");
        validator.Assert(!this.IsAvailable || this.StockQuantity > 0, "A product with no stock cannot be available");
        validator.ThrowIfAny();
    }
}