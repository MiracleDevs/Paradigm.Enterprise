# 1. Paradigm.Enterprise.Domain

The Domain project provides the core building blocks for implementing domain entities, value objects, and business logic in the Paradigm.Enterprise framework. It contains base classes and utilities for creating a clean domain model following domain-driven design principles.

## 1.1. Key Components

### 1.1.1. EntityBase

`EntityBase` is the foundational class for all domain entities, implementing the `IEntity` interface:

```csharp
public abstract class EntityBase : Interfaces.IEntity
{
    [Key]
    public int Id { get; set; }

    public bool IsNew() => Id == default;
}
```

The class provides a generic version with mapping capabilities:

```csharp
public abstract class EntityBase<TInterface, TEntity, TView> : EntityBase
    where TInterface : Interfaces.IEntity
    where TEntity : EntityBase<TInterface, TEntity, TView>, TInterface
    where TView : EntityBase, TInterface, new()
{
    public virtual TEntity? MapFrom(IServiceProvider serviceProvider, TInterface model) { ... }
    public virtual TView MapTo(IServiceProvider serviceProvider) { ... }
    public virtual void AfterMapping() { }
    public virtual void BeforeMapping() { }
    public virtual void Validate() { }
}
```

### 1.1.2. Entities

The Domain project includes several ready-to-use entities:

- **DomainTracker** - A specialized entity for tracking domain object changes

### 1.1.3. Repositories

The Domain project defines repository interfaces for working with entities:

- **IRepository\<T>** - Base repository interface for CRUD operations
- **IAuditableRepository\<T>** - Repository interface for auditable entities

### 1.1.4. Unit of Work

The unit of work pattern is implemented to manage transactions and persistence:

- **IUnitOfWork** - Interface for transactions and commit operations
- **UnitOfWorkBase** - Base implementation of the unit of work pattern

### 1.1.5. DTOs (Data Transfer Objects)

The project includes several DTOs for common use cases:

- **PaginatedResultDto\<T>** - For handling paginated results
- **FilterTextPaginatedParameters** - For search operations with filtering and pagination

### 1.1.6. Mappers

Mapping utilities for converting between entities, DTOs, and views:

- **EntityMapper** - Maps between domain entities and DTOs

## 1.2. Usage Example

```csharp
// Example of creating a domain entity
public class Product : EntityBase
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
}

// Example of creating an auditable entity
public class Customer : AuditableEntityBase
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}

// Using the repository
public class ProductService
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IRepository<Product> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Product> CreateProduct(string name, decimal price)
    {
        var product = new Product { Name = name, Price = price };
        await _repository.CreateAsync(product);
        await _unitOfWork.CommitAsync();
        return product;
    }
}
```

## 1.3. NuGet Package

```shell
Install-Package Paradigm.Enterprise.Domain
```
