# Paradigm.Enterprise.Interfaces

The Interfaces project defines the core contracts used throughout the Paradigm.Enterprise framework. These interfaces establish the foundation for the framework's architecture and ensure consistent implementation across different components.

## Key Interfaces

### IEntity

The `IEntity` interface is the base contract for all domain entities in the framework:

```csharp
public interface IEntity
{
    int Id { get; }
    bool IsNew();
}
```

- **Id** - Unique identifier for the entity
- **IsNew()** - Determines if the entity is a new instance or an existing one from persistence

### IAuditableEntity

The `IAuditableEntity` interface extends `IEntity` with auditing capabilities:

```csharp
public interface IAuditableEntity : IEntity
{
    int? CreatedByUserId { get; set; }
    DateTimeOffset CreationDate { get; set; }
    int? ModifiedByUserId { get; set; }
    DateTimeOffset? ModificationDate { get; set; }
}
```

This interface adds properties for tracking:
- Who created the entity
- When it was created
- Who last modified it
- When it was last modified

## Usage

These interfaces are implemented by domain entities throughout the application:

```csharp
// Example of a domain entity implementing IEntity
public class Product : EntityBase, IEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    
    public bool IsNew() => Id == default;
}

// Example of an auditable entity
public class Customer : EntityBase, IAuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    public int? CreatedByUserId { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public int? ModifiedByUserId { get; set; }
    public DateTimeOffset? ModificationDate { get; set; }
    
    public bool IsNew() => Id == default;
}
```

## Integration with Other Components

The interfaces defined in this project are used extensively throughout other components of the framework:

- **Domain** - Provides base implementations of these interfaces
- **Data** - Uses these interfaces for repository and data access operations
- **Providers** - Uses these interfaces for CRUD operations and business logic

## NuGet Package

```
Install-Package Paradigm.Enterprise.Interfaces
``` 