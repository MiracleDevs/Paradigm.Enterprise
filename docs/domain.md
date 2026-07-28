# Domain model

The domain model gives business concepts a stable home that is independent from HTTP and persistence mechanics. Paradigm.Enterprise supplies entity bases and validation helpers, but an application remains responsible for meaningful names, invariants, and aggregate boundaries.

`EntityBase<TId>` provides the identifier and determines whether an entity is new by comparing the identifier to its default value. The mapped form, `EntityBase<TId, TInterface, TEntity, TView>`, adds mapping hooks and `Validate`.

```csharp
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Exceptions;

public sealed class CatalogItem
    : EntityBase<Guid, ICatalogItem, CatalogItem, CatalogItemView>, ICatalogItem
{
    public string Name { get; set; } = string.Empty;

    public override CatalogItem? MapFrom(
        IServiceProvider serviceProvider,
        ICatalogItem model)
    {
        Id = model.Id;
        Name = model.Name.Trim();
        return this;
    }

    public override CatalogItemView MapTo(IServiceProvider serviceProvider)
    {
        return new CatalogItemView { Id = Id, Name = Name };
    }

    public override void Validate()
    {
        var validator = new DomainValidator();
        validator.Assert(!string.IsNullOrWhiteSpace(Name), "Name is required.");
        validator.ThrowIfAny();
    }
}
```

The base mapping methods are intentionally virtual and do not provide a usable default. An entity that participates in the generic edit provider must implement mapping directly or delegate to an `EntityMapperBase`.

## Validation responsibilities

Transport validation checks whether an HTTP request has the required shape. Domain validation protects invariants regardless of the caller. Persistence configuration enforces storage constraints. These checks complement one another and should not be collapsed into a single layer.

`DomainValidator` collects assertion failures and throws a `DomainException` through `ThrowIfAny`. A provider should use repository-backed checks or orchestration rules when validation depends on external state. An entity should not resolve repositories or services from the service provider.

## Aggregate boundaries

An aggregate root controls changes to its children. Expose methods that express valid transitions instead of allowing callers to mutate nested state freely. When an update removes children, the aggregate repository can override `DeleteRemovedAggregates` and use `RemoveAggregate` to mark those children for deletion in the same context.

Transactions can coordinate persistence, but they do not make several aggregates one consistency boundary. Prefer one aggregate per immediate business invariant and introduce an explicit application workflow when a use case spans aggregates.

## Mapping

`MapperBase<TFrom, TTo>` uses Mapster for bidirectional mapping into existing instances. `EntityMapperBase` adds helpers for the shared entity interface. Registration is convention based, so each concrete mapper must implement one closed generic `IMapper<TFrom, TTo>`.

Mapping should translate representations. It should not hide database calls, authorization decisions, or domain rules.

## Other domain utilities

The package also includes pagination DTOs, state-machine contracts, `DomainTracker<TEntity>` for tracking collection changes, logged-user abstractions for auditing, and Unit of Work contracts. Use these utilities when they express an application need; inheriting an entity base does not require every other feature.
