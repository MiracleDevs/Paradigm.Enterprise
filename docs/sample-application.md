# 1. Sample Application with Paradigm.Enterprise

This guide demonstrates how to build a complete API using the Paradigm.Enterprise framework. We'll create a simple product catalog API with basic CRUD operations.

## 1.1. Project Setup

1. Create a new ASP.NET Core Web API project:

    ```shell
    dotnet new webapi -n ProductCatalog.Api
    ```

2. Add the required Paradigm.Enterprise NuGet packages:

    ```shell
    dotnet add package Paradigm.Enterprise.WebApi
    dotnet add package Paradigm.Enterprise.Data.SqlServer
    ```

## 1.2. Domain Layer

First, let's define our domain entities:

### 1.2.1. Product.cs

```csharp
using Paradigm.Enterprise.Domain.Entities;

namespace ProductCatalog.Domain.Entities
{
    public class Product : EntityBase
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
```

### 1.2.2. ProductCategory.cs

```csharp
using Paradigm.Enterprise.Domain.Entities;

namespace ProductCatalog.Domain.Entities
{
    public class ProductCategory : EntityBase
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
```

## 1.3. Data Layer

Next, let's set up the data context:

### 1.3.1. AppDbContext.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Context;
using ProductCatalog.Domain.Entities;

namespace ProductCatalog.Data
{
    public class AppDbContext : EfDataContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ProductCategory>()
                .Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(50);
        }
    }
}
```

## 1.4. DTOs (Data Transfer Objects)

Define the DTOs for our API:

### 1.4.1. ProductDto.cs

```csharp
using Paradigm.Enterprise.Interfaces;

namespace ProductCatalog.Api.Dtos
{
    public class ProductViewDto : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public bool IsNew() => Id == default;
    }

    public class ProductEditDto : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsNew() => Id == default;
    }

    public class ProductSearchParameters : Paradigm.Enterprise.Domain.Dtos.FilterTextPaginatedParameters
    {
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? IsActive { get; set; }
    }
}
```

## 1.5. Repositories

Let's create repositories for our entities:

### 1.5.1. ProductRepository.cs

```csharp
using Paradigm.Enterprise.Data.Repositories;
using ProductCatalog.Data;
using ProductCatalog.Domain.Entities;

namespace ProductCatalog.Data.Repositories
{
    public class ProductRepository : RepositoryBase<Product>
    {
        public ProductRepository(AppDbContext context) : base(context)
        {
        }
    }
}
```

### 1.5.2. ProductCategoryRepository.cs

```csharp
using Paradigm.Enterprise.Data.Repositories;
using ProductCatalog.Data;
using ProductCatalog.Domain.Entities;

namespace ProductCatalog.Data.Repositories
{
    public class ProductCategoryRepository : RepositoryBase<ProductCategory>
    {
        public ProductCategoryRepository(AppDbContext context) : base(context)
        {
        }
    }
}
```

## 1.6. Providers

Create providers for the business logic:

### 1.6.1. IProductProvider.cs

```csharp
using Paradigm.Enterprise.Providers;
using ProductCatalog.Api.Dtos;

namespace ProductCatalog.Api.Providers
{
    public interface IProductProvider : IEditProvider<ProductViewDto, ProductEditDto>
    {
        Task<IEnumerable<ProductViewDto>> GetProductsByCategoryAsync(int categoryId);
    }
}
```

### 1.6.2. ProductProvider.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Providers;
using ProductCatalog.Api.Dtos;
using ProductCatalog.Data;
using ProductCatalog.Data.Repositories;
using ProductCatalog.Domain.Entities;

namespace ProductCatalog.Api.Providers
{
    public class ProductProvider : EditProviderBase<ProductViewDto, ProductEditDto>, IProductProvider
    {
        private readonly ProductRepository _repository;
        private readonly AppDbContext _context;

        public ProductProvider(
            ProductRepository repository,
            IUnitOfWork unitOfWork,
            IServiceProvider serviceProvider,
            AppDbContext context)
            : base(repository, unitOfWork, serviceProvider)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<IEnumerable<ProductViewDto>> GetProductsByCategoryAsync(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Join(_context.ProductCategories,
                    p => p.CategoryId,
                    c => c.Id,
                    (p, c) => new ProductViewDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        CategoryId = p.CategoryId,
                        CategoryName = c.Name,
                        IsActive = p.IsActive
                    })
                .ToListAsync();

            return products;
        }

        public override async Task<PaginatedResultDto<ProductViewDto>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters)
        {
            var searchParams = parameters as ProductSearchParameters;

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchParams?.FilterText))
            {
                query = query.Where(p => p.Name.Contains(searchParams.FilterText) ||
                                        p.Description.Contains(searchParams.FilterText));
            }

            if (searchParams?.CategoryId.HasValue == true)
            {
                query = query.Where(p => p.CategoryId == searchParams.CategoryId.Value);
            }

            if (searchParams?.MinPrice.HasValue == true)
            {
                query = query.Where(p => p.Price >= searchParams.MinPrice.Value);
            }

            if (searchParams?.MaxPrice.HasValue == true)
            {
                query = query.Where(p => p.Price <= searchParams.MaxPrice.Value);
            }

            if (searchParams?.IsActive.HasValue == true)
            {
                query = query.Where(p => p.IsActive == searchParams.IsActive.Value);
            }

            var totalItems = await query.CountAsync();

            // Apply pagination
            query = query.Skip((searchParams?.PageNumber ?? 1 - 1) * (searchParams?.PageSize ?? 10))
                        .Take(searchParams?.PageSize ?? 10);

            // Join with categories
            var result = await query
                .Join(_context.ProductCategories,
                    p => p.CategoryId,
                    c => c.Id,
                    (p, c) => new ProductViewDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        CategoryId = p.CategoryId,
                        CategoryName = c.Name,
                        IsActive = p.IsActive
                    })
                .ToListAsync();

            return new PaginatedResultDto<ProductViewDto>
            {
                Items = result,
                TotalItems = totalItems,
                PageNumber = searchParams?.PageNumber ?? 1,
                PageSize = searchParams?.PageSize ?? 10
            };
        }

        protected override ProductViewDto MapEntityToView(Product entity)
        {
            var category = _context.ProductCategories.FirstOrDefault(c => c.Id == entity.CategoryId);

            return new ProductViewDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Price = entity.Price,
                CategoryId = entity.CategoryId,
                CategoryName = category?.Name ?? string.Empty,
                IsActive = entity.IsActive
            };
        }

        protected override Product MapEditToEntity(ProductEditDto model)
        {
            var entity = model.Id != default
                ? _repository.GetByIdAsync(model.Id).Result
                : new Product();

            entity.Name = model.Name;
            entity.Description = model.Description;
            entity.Price = model.Price;
            entity.CategoryId = model.CategoryId;
            entity.IsActive = model.IsActive;

            return entity;
        }
    }
}
```

## 1.7. Controllers

Finally, let's create the API controllers:

### 1.7.1. ProductsController.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using Paradigm.Enterprise.WebApi.Controllers;
using ProductCatalog.Api.Dtos;
using ProductCatalog.Api.Providers;

namespace ProductCatalog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ApiControllerCrudBase<IProductProvider, ProductViewDto, ProductEditDto, ProductSearchParameters>
    {
        public ProductsController(
            ILogger<ProductsController> logger,
            IProductProvider provider)
            : base(logger, provider)
        {
        }

        [HttpGet("by-category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            var products = await Provider.GetProductsByCategoryAsync(categoryId);
            return Ok(products);
        }
    }
}
```

## 1.8. Program.cs

Set up the application in the Program.cs file:

```csharp
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Uow;
using ProductCatalog.Api.Providers;
using ProductCatalog.Data;
using ProductCatalog.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add repositories
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<ProductCategoryRepository>();

// Add unit of work
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

// Add providers
builder.Services.AddScoped<IProductProvider, ProductProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## 1.9. appsettings.json

Configure the connection string in appsettings.json:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ProductCatalog;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## 1.10. Running the Application

1. Create the database using Entity Framework migrations:

    ```shell
    dotnet ef migrations add InitialCreate
    dotnet ef database update
    ```

2. Run the application:

    ```shell
    dotnet run
    ```

3. Access the Swagger UI at <https://localhost:5001/swagger> to test the API endpoints.

## 1.11. Conclusion

This sample application demonstrates how to use the Paradigm.Enterprise framework to build a complete API with CRUD operations. The framework provides a solid foundation for implementing best practices like repository pattern, unit of work, and provider pattern, while reducing boilerplate code.
