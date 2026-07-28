# Domain model

A domain model gives the language and rules of an application a stable home. It should describe what the application means, what changes are allowed, and which facts must remain true. HTTP routes, database tables, and framework types matter to the implementation, but they should not become the vocabulary used to explain the problem.

Paradigm.Enterprise supplies entity bases, mapping hooks, validation helpers, collection tracking, state helpers, auditing contracts, and Unit of Work contracts. These are building blocks. They do not discover business rules or turn a persistence model into a complete domain model automatically.

## Begin with the language

Names are part of the model. An entity, method, state, and exception should use the terms that developers and domain specialists use when discussing the same behavior. This shared, ubiquitous language keeps conversations, requirements, tests, and code aligned. A method named `Approve` communicates more than a public `Status` setter because it identifies an intention and gives the entity a place to enforce the transition.

The shared language also helps define boundaries. If two parts of a system use the same word with different meanings, they may belong to different modules or bounded contexts. Do not force one large model to represent every interpretation. Keep contracts explicit at the boundary and translate between models where necessary.

The framework does not require a particular module layout. Use the existing layer structure inside a module, and introduce additional modules only when language, ownership, lifecycle, or change patterns justify the separation. See [Design principles](design-principles.md) for the broader dependency and modularity guidance.

## Entities and identity

An entity has continuity through change. Two entity instances represent the same conceptual object when they carry the same identity, even if other values differ.

`EntityBase<TId>` provides an `Id` property and implements `IsNew` by comparing that identifier with its default value. This is a mechanical convention, not a lifecycle policy. The application must ensure that its identifier generation strategy agrees with the convention. An assigned non-default identifier makes `IsNew` return `false`, even before the entity has been stored.

The mapped form, `EntityBase<TId, TInterface, TEntity, TView>`, adds `MapFrom`, `MapTo`, mapping hooks, and `Validate`. Its defaults are intentionally minimal: `MapFrom` returns `null`, `MapTo` throws `NotImplementedException`, and `Validate` does nothing. An entity used by the generic edit provider must override the operations its workflow needs or delegate mapping to an `EntityMapperBase`.

A useful entity exposes behavior rather than asking every caller to reproduce its rules. Public setters may still exist on generated persistence-shaped classes, so encapsulation is an application design responsibility. Partial classes can add intention-revealing methods without editing generated files.

```csharp
public partial class CatalogItem
{
    public void Rename(string name)
    {
        var normalizedName = name.Trim();

        if (normalizedName.Length == 0)
            throw new DomainException("A name is required.");

        Name = normalizedName;
    }

    public void Retire(DateTimeOffset retiredAt)
    {
        if (RetiredAt is not null)
            throw new DomainException("The item is already retired.");

        RetiredAt = retiredAt;
    }
}
```

This method-based design does not prevent Entity Framework or mapping code from setting properties. It gives application workflows one well-named path for intentional changes and one place to test the rules around those changes.

## Value objects

A value object represents a descriptive concept whose equality comes from its values rather than an identifier. Examples include a date range, a quantity with its unit, or a normalized code. Value objects are useful when a group of primitive values has rules that should travel together.

Paradigm.Enterprise does not provide a value-object base class. Applications can use records, record structs, or ordinary immutable classes and configure their Entity Framework mapping explicitly. Keep a value object small, validate it when it is created, and avoid giving it persistence or service dependencies.

```csharp
public sealed record ItemCode
{
    public string Value { get; }

    public ItemCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("An item code is required.");

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length is < 3 or > 20)
            throw new DomainException("The item code has an invalid length.");

        Value = normalized;
    }

    public override string ToString() => Value;
}
```

Whether a value object belongs in a generated entity, a hand-written model, or a boundary DTO depends on the database-first mapping and serialization requirements. Do not introduce one merely to wrap every primitive. Use it when the concept has meaning, equality, or rules worth protecting.

## Invariants and validation

An invariant is a condition that must remain true for a valid domain object. Enforce an invariant close to the state it protects so that it is applied regardless of whether a controller, worker, test, or message consumer invokes the use case.

Transport validation, domain validation, application checks, and database constraints protect different boundaries. Model binding can reject an incorrectly shaped HTTP request. An entity protects rules about its own state. A provider performs checks that require repositories, logged-user information, or external services. Database constraints remain the last protection for persisted data. These checks complement one another.

The generic edit provider calls `Validate` after mapping and before staging a repository operation. `DomainValidator` can collect several failures and throw one `DomainException`.

```csharp
public override void Validate()
{
    var validator = new DomainValidator();
    validator.Assert(!string.IsNullOrWhiteSpace(Name), "A name is required.");
    validator.Assert(Name.Length <= 200, "The name is too long.");
    validator.Assert(RetiredAt is null || RetiredAt >= CreatedAt,
        "The retirement date cannot precede creation.");
    validator.ThrowIfAny();
}
```

`Validate` is a no-op until an application overrides it. Invariants that must hold at every transition are often safer inside behavior methods as well as final validation. This prevents an invalid intermediate state from escaping a method and makes failures easier to understand.

Mapping receives an `IServiceProvider` for compatibility with the framework mapping model. Do not use that parameter to resolve repositories or services from an entity. A uniqueness check, permission lookup, or remote policy belongs in a provider or provider hook because it depends on state outside the entity.

## Aggregate boundaries

An aggregate is a consistency boundary led by an aggregate root. Callers address the root, and the root coordinates changes to the entities and value objects inside the boundary. The boundary should contain only the state that must be immediately consistent for the rules being enforced.

Database relationships do not define aggregates by themselves. A foreign key may connect two independent aggregates, while an owned child may be meaningful only through its root. Ask whether a child can change independently, whether it has a separate lifecycle, and whether a rule requires the root and child to change atomically.

Avoid making every connected entity part of one large aggregate. Large object graphs increase contention, make queries and updates harder to reason about, and encourage transactions that cover unrelated changes. When a use case spans aggregates, let a provider coordinate the workflow and make the consistency and failure model explicit.

A technical transaction can include several repository operations, but it does not redefine the domain boundary. The Unit of Work also does not make remote APIs, email, cache, or blob storage part of a database transaction. See [Transactions and commits](guides/transactions.md) for the exact commit behavior.

## Controlled child collections

An aggregate root should expose child operations that preserve its invariants. A read-only collection view prevents routine callers from replacing the collection, while methods such as `AddComponent`, `ChangeComponent`, and `RemoveComponent` express the allowed changes.

`DomainTracker<TEntity>` is a manual ledger with `Added`, `Edited`, and `Removed` collections. Calling `Add`, `Edit`, or `Remove` records an application decision. It does not compare snapshots, observe property setters, synchronize an entity collection, or participate in Entity Framework change tracking automatically.

```csharp
public partial class Catalog
{
    private readonly DomainTracker<CatalogComponent> _componentChanges = new();

    public IReadOnlyCollection<CatalogComponent> RemovedComponents =>
        _componentChanges.Removed;

    public void RemoveComponent(CatalogComponent component)
    {
        if (!Components.Remove(component))
            throw new DomainException("The component does not belong to this catalog.");

        _componentChanges.Remove(component);
    }
}
```

An aggregate repository can override `DeleteRemovedAggregates` and call the protected `RemoveAggregate` helper for children recorded as removed. That repository code must be written explicitly. The tracker and repository base do not infer which navigation properties form an aggregate.

Reset a tracker only when the application has deliberately reconciled or discarded the recorded changes. Clearing it too early loses the information the repository needs to remove children.

## State transitions

When allowed behavior depends strongly on an entity's current state, explicit transition methods make the rules visible. A small conditional may be enough for a simple lifecycle. A state object can be useful when each state permits substantially different behavior.

The Domain package includes `IState<TState>`, `IStateContext<TState>`, `StateFactory`, and `StateTransitionException<TState>`. `StateFactory` uses reflection and a naming convention: a requested state name is resolved as a class named `{StateName}State` in the state interface namespace and assembly, then constructed with the context. These types help organize transitions, but they do not persist state, register types, publish events, or provide a workflow engine.

Use the helpers only when the convention makes the model clearer. Persist the state value through the application's normal entity mapping, reject invalid transitions explicitly, and test every allowed and rejected path. A state name coming from storage must match an available state type or `StateFactory` throws.

## Database-first models and generated code

The Visual Studio template uses database-first generation because the database is an important integration contract for these applications. Reverse engineering creates persistence-shaped entity and context files. The interface analyzer derives application interfaces from recognized entity shapes. Neither process supplies domain meaning.

Generated files are replaceable output. Add mapping, validation, computed behavior, navigation helpers, and state transitions in partial classes that survive regeneration. Do not edit generated interfaces or entity files. When the generated shape makes an invariant difficult to protect, document the compromise and consider a hand-written domain model or a clearer mapping boundary for that area.

Database-first development does not mean modeling the application as tables. The generated class answers how data is stored. The hand-written partial behavior, providers, and contracts answer how the application uses that data. Regeneration should preserve this separation.

Generated interfaces reduce repetitive property contracts. They do not define aggregate roots, decide which members callers may change, or create a stable public API automatically. Review generated contract changes whenever a database scaffold changes.

## Read and write representations

The framework supports a pragmatic separation between editable entities and read views. A write path can load an entity, apply behavior, validate it, and commit it. A read path can query a projection shaped for the caller without loading the write aggregate.

This separation is not a complete CQRS implementation. It does not require separate databases, asynchronous projections, commands, events, or eventual consistency. Begin with the smallest useful separation and add operational complexity only when measured needs justify it.

Mapping translates between representations. It should not hide authorization decisions, database calls, remote service access, or business rules. `MapperBase<TFrom, TTo>` uses Mapster for bidirectional mapping into existing instances. `EntityMapperBase` adds helpers for the shared entity interface, and convention registration requires each concrete mapper to implement a closed `IMapper<TFrom, TTo>`.

An incoming view may be structurally valid but still request an invalid transition. Map the request into the appropriate domain operation or state, validate the result, and return a read view that represents the committed data. Review [Providers](providers.md) for the generic edit lifecycle and its hook ordering.

## Auditing

`IAuditableEntity<TId>` declares creation and modification user identifiers. `IAuditableEntity<TDate, TId>` adds creation and modification timestamps. During `DbContextBase<TId>.SaveChangesAsync`, the context inspects auditable entries and resolves `ILoggedUserService<TId>`. If that service returns an authenticated user for an added or modified entry, the context calls the audit extension before saving.

The current extension mutates only entities implementing `IAuditableEntity<DateTime, TId>` or `IAuditableEntity<DateTimeOffset, TId>`. It uses UTC values and chooses creation or modification fields by calling `IsNew`. An entity implementing only `IAuditableEntity<TId>`, or using another `TDate` type, receives no automatic audit values from this extension.

If there is no authenticated user, the context does not apply audit values through this path. Applications using the automatic audit behavior must register the logged-user service, use one of the two supported timestamped contracts, and verify that identifier generation agrees with `IsNew`.

These fields are technical change metadata, not a complete security audit trail. A system that needs evidence of who performed a sensitive action, what decision was made, or what values changed needs an explicit, protected audit design with appropriate retention and access controls.

## Testing the model

Domain tests should construct entities and value objects directly, invoke behavior, and verify the resulting state. They should cover both valid operations and rejected transitions without starting Entity Framework, ASP.NET Core, or the full dependency container.

Aggregate tests should verify that child changes go through the root, invalid combinations cannot escape, and manual tracker entries match the intended collection changes. State tests should exercise every supported transition and confirm that invalid transitions preserve the prior state.

Provider and repository tests cover the boundaries that a domain test cannot. Use provider tests for checks involving collaborators, mapping, lifecycle hooks, and commits. Use relational integration tests for generated mappings, constraints, transactions, and aggregate deletion. The [Testing applications](tests.md) guide explains the complete test strategy.
