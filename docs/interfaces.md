# Contracts and identities

`Paradigm.Enterprise.Interfaces` contains the smallest shared entity contracts. It targets `netstandard2.0` so domain contracts can be consumed without pulling in the rest of the framework.

`IEntity` is a marker. `IEntity<TId>` adds a strongly typed identifier, where `TId` is a value type implementing `IEquatable<TId>`. The generic identifier is carried through entities, repositories, providers, and controllers, preventing a repository for one identifier type from being paired accidentally with another.

```csharp
using Paradigm.Enterprise.Interfaces;

public interface ICatalogItem : IEntity<Guid>
{
    string Name { get; set; }
}
```

`IAuditableEntity<TId>` adds the creation and modification user identifiers. `IAuditableEntity<TDate, TId>` adds creation and modification timestamps using the selected date type. Implementing either contract makes an entity visible to the audit scan in `DbContextBase<TId>`, but the current audit extension mutates only `IAuditableEntity<DateTime, TId>` and `IAuditableEntity<DateTimeOffset, TId>`. The base user-identifier contract by itself, and timestamped contracts using another date type, receive no automatic values from that extension. Automatic auditing also requires the registered logged-user service to return an authenticated user.

## Generated application interfaces

The Visual Studio template includes an analyzer project that inspects domain entities and generates application interfaces. The Domain project references that generator as an analyzer, which explains the unusual dependency from Domain toward the Interfaces project.

Generated interfaces reduce repetitive contracts but make generation rules part of the development model. Entity names, base types, scalar properties, navigation collections, and identifier types influence the generated result. Do not edit generated output. Add behavior and mapping in partial classes, then regenerate from the authoritative domain and database model.

The generator defaults to `int` only when it cannot infer another identifier from the supported entity base forms. New documentation and new application code should make the identifier type explicit.
