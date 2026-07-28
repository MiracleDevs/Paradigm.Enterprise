# Conventions reference

Paradigm.Enterprise uses conventions where repeated explicit registration would add noise. These conventions are runtime contracts and generation contracts. Breaking one can cause a startup failure, a skipped registration, or incomplete generated output.

## Discovery

Repository and provider implementations must be public, non-abstract, and assignable to the appropriate Enterprise marker. A concrete type must implement an interface named by prefixing its exact class name with `I`. That application interface normally inherits the typed Enterprise contract, but discovery filters the concrete type rather than imposing that inheritance on the exact-name interface itself.

Default discovery begins at the entry assembly and its referenced assemblies. Pass explicit assemblies for modules that are not reachable from that graph.

Mappers must implement one closed generic `IMapper<TFrom, TTo>`. Services discovered through `RegisterServices` are singletons unless ignored and registered explicitly.

## Entity identifiers

Identifiers are value types implementing `IEquatable<TId>`. Carry the same `TId` through `IEntity`, entity bases, repository contracts and bases, provider contracts and bases, and controller bases.

`IsNew` compares the identifier with its default value. A default `Guid` or zero numeric identifier therefore selects the add path in `SaveAsync`.

## Generated files

Database entities and contexts are owned by EF Core Power Tools and T4 templates. Application behavior belongs in partial files. Generated application interfaces are owned by the analyzer. JSON contexts, stored-procedure mappers, and clients are owned by the console generator.

Inspect generated headers and project configuration before editing. Change the source model, partial extension, or owning template instead of patching generated output.

## Search

`ReadRepositoryBase.SearchAsync` delegates to `GetSearchPaginatedFunction`. A repository exposed through a generic search endpoint must override that function or provide its own search implementation.

`SearchPaginatedAsync` is obsolete. New code should call generic `SearchAsync<TParameters>`.

## Commits

Repository write methods only stage changes. Generic edit providers call `IUnitOfWork.CommitChangesAsync`. Custom workflows own their commit and explicit transaction boundaries.

Repositories register their contexts with the Unit of Work when constructed. Resolve participating repositories before creating an explicit transaction.

## Host behavior

Library controller bases carry inherited `AllowAnonymous` metadata. `Authorize` on a derived controller and fallback policies do not override it. Use a controller that does not inherit these bases when an endpoint must be protected.

Endpoint exposure control is disabled until `AddEndpointExposureControl` is called. When enabled, only actions carrying `ExposeEndpoint` remain routable. Exposure is not authorization.

When reflection-based JSON metadata is disabled, every serialized type must be present in a registered `JsonSerializerContext`.
