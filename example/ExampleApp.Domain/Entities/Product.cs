using System.ComponentModel.DataAnnotations;
using ExampleApp.Domain.Dtos;
using ExampleApp.Interfaces;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Exceptions;

namespace ExampleApp.Domain.Entities;

/// <summary>
/// Product entity implementation with validation logic
/// </summary>
public class Product : EntityBase<IProduct, Product, ProductView>, IProduct
{
    public int Id { get; set; } = 0;

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


    public override void Validate()
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(this);
        var isValid = Validator.TryValidateObject(this, validationContext, validationResults, true);

        // Custom business rules
        if (Price < 0.01m)
        {
            validationResults.Add(new ValidationResult("Price must be greater than 0"));
            isValid = false;
        }

        if (StockQuantity <= 0 && IsAvailable)
        {
            validationResults.Add(new ValidationResult("A product with no stock cannot be available"));
            isValid = false;
        }

        if (!isValid)
        {
            var validator = new DomainValidator();
            foreach (var validationResult in validationResults)
            {
                validator.AddError(validationResult.ErrorMessage ?? "Validation failed");
            }
            throw new DomainException(validator.ToString() ?? "Validation issue");
        }
    }

    public ProductView MapToViewDto()
    {
        return new ProductView
        {
            Id = Id,
            Name = Name,
            Price = Price,
            Description = Description,
            Category = Category,
            StockQuantity = StockQuantity,
            IsAvailable = IsAvailable,
            CreatedDate = CreatedDate
        };
    }

    public void MapFromEditDto(ProductEditDto dto)
    {
        if (dto is not null)
        {
            if (this.Id == 0)
            {
                Id = dto.Id;
            }

            Name = dto.Name;
            Price = dto.Price;
            Description = dto.Description;
            Category = dto.Category;
            StockQuantity = dto.StockQuantity;
            IsAvailable = dto.IsAvailable;
            ModifiedDate = DateTime.UtcNow;
        }
    }
}