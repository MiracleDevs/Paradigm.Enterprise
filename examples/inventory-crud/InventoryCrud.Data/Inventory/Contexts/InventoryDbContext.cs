using InventoryCrud.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Context;

namespace InventoryCrud.Data.Inventory.Contexts;

/// <summary>
/// Owns Inventory persistence for the example application.
/// </summary>
public class InventoryDbContext : DbContextBase<int>
{
    #region Properties

    public DbSet<Product> Products { get; set; } = null!;

    public DbSet<ProductView> ProductViews { get; set; } = null!;

    #endregion

    #region Constructors

    public InventoryDbContext(
        IServiceProvider serviceProvider,
        DbContextOptions<InventoryDbContext> options)
        : base(serviceProvider, options)
    {
    }

    #endregion

    #region Overrides

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).IsRequired().HasMaxLength(100);
            entity.Property(product => product.Price).HasPrecision(18, 2);
            entity.Property(product => product.Description).HasMaxLength(500);
            entity.Property(product => product.Category).IsRequired().HasMaxLength(50);
            entity.Property(product => product.StockQuantity).IsRequired();
            entity.Property(product => product.IsAvailable).IsRequired();
            entity.Property(product => product.CreatedDate).IsRequired();
            entity.Property(product => product.ModifiedDate).IsRequired();
        });
        modelBuilder.Entity<ProductView>(entity =>
        {
            entity.ToView("ProductView");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).IsRequired().HasMaxLength(100);
            entity.Property(product => product.Price).HasPrecision(18, 2);
            entity.Property(product => product.Description).HasMaxLength(500);
            entity.Property(product => product.Category).IsRequired().HasMaxLength(50);
            entity.Property(product => product.StockQuantity).IsRequired();
            entity.Property(product => product.IsAvailable).IsRequired();
            entity.Property(product => product.CreatedDate).IsRequired();
            entity.Property(product => product.ModifiedDate).IsRequired();
        });
        modelBuilder.Entity<Product>().HasData(
            new
            {
                Id = 1,
                Name = "Gaming Laptop",
                Price = 1299.99m,
                Description = "High-performance gaming laptop with RTX graphics",
                Category = "Electronics",
                StockQuantity = 10,
                IsAvailable = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new
            {
                Id = 2,
                Name = "Wireless Headphones",
                Price = 199.99m,
                Description = "Noise-cancelling wireless headphones",
                Category = "Audio",
                StockQuantity = 25,
                IsAvailable = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });
    }

    #endregion
}
