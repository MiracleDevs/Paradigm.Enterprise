# 1. Paradigm.Enterprise.Interfaces

The Interfaces project defines the core contracts used throughout the Paradigm.Enterprise framework. These interfaces establish the foundation for the framework's architecture and ensure consistent implementation across different components.

## 1.1. Key Interfaces

### 1.1.1. IEntity

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

### 1.1.2. IAuditableEntity

The `IAuditableEntity` interface extends `IEntity` with auditing capabilities. In version 1.0.6, this interface was refactored to support both `DateTime` and `DateTimeOffset` types:

```csharp
public interface IAuditableEntity : IEntity
{
    int? CreatedByUserId { get; set; }
    int? ModifiedByUserId { get; set; }
}

public interface IAuditableEntity<TDate> : IAuditableEntity where TDate : struct
{
    TDate CreationDate { get; set; }
    TDate? ModificationDate { get; set; }
}
```

This interface hierarchy adds properties for tracking:

- Who created the entity
- When it was created
- Who last modified it
- When it was last modified

The generic type parameter `TDate` allows for using either `DateTime` or `DateTimeOffset` as the date type.

## 1.2. Usage

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

// Example of an auditable entity with DateTime
public class Customer : EntityBase, IAuditableEntity<DateTime>
{
    public int Id { get; set; }
    public string Name { get; set; }

    public int? CreatedByUserId { get; set; }
    public int? ModifiedByUserId { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? ModificationDate { get; set; }

    public bool IsNew() => Id == default;
}

// Example of an auditable entity with DateTimeOffset
public class Order : EntityBase, IAuditableEntity<DateTimeOffset>
{
    public int Id { get; set; }
    public decimal Total { get; set; }

    public int? CreatedByUserId { get; set; }
    public int? ModifiedByUserId { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset? ModificationDate { get; set; }

    public bool IsNew() => Id == default;
}
```

## 1.3. Integration with Other Components

The interfaces defined in this project are used extensively throughout other components of the framework:

- **Domain** - Provides base implementations of these interfaces
- **Data** - Uses these interfaces for repository and data access operations
- **Providers** - Uses these interfaces for CRUD operations and business logic

## 1.4. NuGet Package

```shell
Install-Package Paradigm.Enterprise.Interfaces
```
