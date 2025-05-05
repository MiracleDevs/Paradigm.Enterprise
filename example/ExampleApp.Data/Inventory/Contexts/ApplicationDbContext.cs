using ExampleApp.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Context;

namespace ExampleApp.Data.Inventory.Contexts;

/// <summary>
/// Database context for our example application
/// </summary>
public class ApplicationDbContext : DbContextBase
{
    public DbSet<Product> Products { get; set; } = null!;

    public DbSet<ProductView> ProductViews { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StockQuantity).IsRequired();
            entity.Property(e => e.IsAvailable).IsRequired();
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.ModifiedDate).IsRequired();
        });

        // Configure Product entity
        modelBuilder.Entity<ProductView>(entity =>
        {
            entity.ToView("ProductView");     
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StockQuantity).IsRequired();
            entity.Property(e => e.IsAvailable).IsRequired();
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.ModifiedDate).IsRequired();
        });

        // Seed some example data
        modelBuilder.Entity<Product>().HasData(
            new Product
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
            new Product
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
            }
        );
    }
}