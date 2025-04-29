# 1. Paradigm.Enterprise.Providers

The Providers project implements the Provider pattern, which acts as a facade over repositories and other services to simplify business logic implementation. Providers serve as an application service layer that orchestrates operations between repositories, domain services, and external services.

## 1.1. Key Components

### 1.1.1. IProvider

The base provider interface that all provider interfaces should inherit from:

```csharp
public interface IProvider
{
}
```

### 1.1.2. IReadProvider

Interface for read operations on entities:

```csharp
public interface IReadProvider<TView> : IProvider
    where TView : IEntity
{
    Task<TView?> GetByIdAsync(int id);
    Task<IEnumerable<TView>> GetAllAsync();
    Task<PaginatedResultDto<TView>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters);
}
```

### 1.1.3. IEditProvider

Interface for CRUD operations on entities:

```csharp
public interface IEditProvider<TView, TEdit> : IReadProvider<TView>
    where TView : IEntity
    where TEdit : IEntity
{
    Task<TView> CreateAsync(TEdit model);
    Task<TView> UpdateAsync(TEdit model);
    Task DeleteAsync(int id);
}
```

### 1.1.4. IAuditableProvider

Interface for providers that work with auditable entities:

```csharp
public interface IAuditableProvider<TView, TEdit> : IEditProvider<TView, TEdit>
    where TView : IEntity
    where TEdit : IEntity
{
}
```

### 1.1.5. Provider Base Classes

The project includes several base provider implementations:

- **ProviderBase** - Base implementation for simple providers
- **ReadProviderBase\<TView>** - Implementation of IReadProvider
  - Updated in version 1.0.10: Improved `SearchPaginatedAsync` method for more optimized queries
- **EditProviderBase<TView, TEdit>** - Implementation of IEditProvider
  - Added in version 1.0.8: Methods for executing actions before the edit model is mapped to an entity
- **AuditableProviderBase<TView, TEdit>** - Implementation for auditable entities

## 1.2. Usage Example

```csharp
// Define provider interface
public interface IProductProvider : IEditProvider<ProductView, ProductEdit>
{
    Task<IEnumerable<ProductView>> GetProductsByCategory(int categoryId);
}

// Implement provider
public class ProductProvider : EditProviderBase<ProductView, ProductEdit>, IProductProvider
{
    private readonly IRepository<Product> _repository;

    public ProductProvider(
        IRepository<Product> repository,
        IUnitOfWork unitOfWork,
        IServiceProvider serviceProvider)
        : base(repository, unitOfWork, serviceProvider)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ProductView>> GetProductsByCategory(int categoryId)
    {
        var products = await _repository.QueryAsync(p => p.CategoryId == categoryId);
        return products.Select(p => p.MapTo(_serviceProvider));
    }

    // Example of using pre-mapping hooks added in version 1.0.8
    protected override async Task<TView> CreateWithPreMappingAsync(TEdit model, Func<Task> preMappingAction)
    {
        // Custom validation before mapping
        if (string.IsNullOrEmpty(model.Name))
        {
            throw new ValidationException("Product name is required");
        }

        // Execute pre-mapping action
        await preMappingAction();

        // Continue with standard create process
        return await base.CreateAsync(model);
    }

    protected override async Task<TView> UpdateWithPreMappingAsync(TEdit model, Func<Task> preMappingAction)
    {
        // Custom logic before mapping to entity
        if (model.Price < 0)
        {
            model.Price = 0; // Ensure price is not negative
        }

        // Execute pre-mapping action
        await preMappingAction();

        // Continue with standard update process
        return await base.UpdateAsync(model);
    }

    // Example of optimized search with improvements from version 1.0.10
    public override async Task<PaginatedResultDto<ProductView>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters)
    {
        // The optimized search method in ReadProviderBase now handles pagination,
        // filtering, and sorting more efficiently
        return await base.SearchPaginatedAsync(parameters);
    }
}

// Register provider in ASP.NET Core
public void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<IProductProvider, ProductProvider>();
}

// Use provider in a controller
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ApiControllerBase<IProductProvider, ProductView, ProductSearchParameters>
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
        var products = await Provider.GetProductsByCategory(categoryId);
        return Ok(products);
    }
}
```

## 1.3. Key Features

1. **Simplified Business Logic** - Encapsulates complex operations into a clean API
2. **Separation of Concerns** - Separates data access from business rules
3. **Reusability** - Promotes code reuse across different UI layers
4. **Testability** - Enables easier unit testing with mocks
5. **Extensibility** - Easy to extend with additional business operations
6. **Pre-Mapping Actions** - Added in version 1.0.8: Support for running actions before mapping edit models to entities
7. **Optimized Queries** - Updated in version 1.0.10: Improved search pagination for better performance

## 1.4. NuGet Package

```shell
Install-Package Paradigm.Enterprise.Providers
```
