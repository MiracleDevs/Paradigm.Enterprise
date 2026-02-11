# RFC: Shift from Entity-Oriented Inheritance to Function-Oriented Composition

- **RFC ID**: 2026-02-11-function-oriented-composition
- **Status**: Draft
- **Author(s)**: Pablo Ordóñez
- **Created**: 2026-02-11
- **Last Updated**: 2026-02-11

## Summary

This RFC proposes shifting the Web API enterprise library from **entity-oriented inheritance** (base controllers, providers, and repositories per capability) to **function-oriented composition**. Controllers would be replaced by modular endpoint registration (`MapCrud`, `MapSearch`, `MapActivable`); providers and repositories would become function-oriented types (`CrudProvider<TEntity>`, `SearchProvider<TEntity, TFilter>`, etc.) composed explicitly rather than inherited. This addresses the inheritance hell, override callback complexity, and tight coupling described in [RFC 2026-01-22 (Developer Experience Pain Points)](./2026-01-22-developer-experience-improvements.md), specifically **Problem Area 1** (interface contracts and hidden knowledge) and **Problem Area 4** (undocumented inheritance patterns and inheritance hell). It does not address Problem Areas 2 (T4 Code Generation) or 3 (Coupled Code Generation Processes).

## Motivation

The current framework is built around entity-oriented base classes: `ApiControllerBase`, `SearchApiControllerBase`, `ActivableApiControllerBase`, `CrudProviderBase`, `SearchRepositoryBase`, and similar. Over time this has led to:

- Inheritance trees that are difficult to reason about
- Reduced composability and increased verbosity
- Tight coupling between entities and capabilities (Search, Activable, etc.)
- Incentives to introduce state in providers
- Difficulty extending functionality without modifying base classes
- Protected override callbacks that are undiscoverable and encourage business logic in the wrong layer (see Problem Area 1 in the parent RFC)
- Combinatorial explosion of base class choices and decision paralysis (see Problem Area 4 in the parent RFC)

Moving to function-oriented composition makes capabilities explicit, keeps providers stateless, and allows the framework to evolve by adding modules rather than changing base classes.

## Detailed Design

### 1. Controller Layer: Endpoint Modules + Explicit Composition

Instead of inheriting from base controllers, we introduce a modular endpoint registration pattern.

For each entity, we define an endpoint module, e.g. `[Entity]Endpoints.cs`, and explicitly compose functionality:

```csharp
public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
                       .WithTags("Products");

        group.MapCrud<Product, Guid>();
        group.MapSearch<Product, ProductFilter>();
        group.MapActivable<Product, Guid>();

        // Custom endpoints
        group.MapGet("/{id:guid}/price", ...);
        group.MapPost("/{id:guid}/recalculate", ...);

        return group;
    }
}
```

**Why this is better:**

- No inheritance required.
- Capabilities are explicitly composed.
- Routing becomes programmatic and predictable.
- Custom endpoints are first-class citizens.
- No "magic" MVC behavior.
- Easier debugging and OpenAPI generation.
- Encourages vertical slice thinking.

Capabilities such as `MapCrud`, `MapSearch`, `MapActivable` are implemented as reusable endpoint modules in the shared library.

### 2. Providers and Repositories: From Entity-Oriented to Function-Oriented

Instead of entity-bound hierarchies such as:

```csharp
ProductProvider : SearchProviderBase<Product>, ActivableProviderBase<Product>, ...
```

we move toward:

- `CrudProvider<TEntity>`
- `SearchProvider<TEntity, TFilter>`
- `CrudRepository<TEntity>`
- `SearchRepository<TEntity>`

These are **function-oriented classes**, not entity-bound hierarchies. Entity-specific logic is implemented only when needed:

- `ProductProvider` — custom use-case logic
- `ProductRepository` — custom data logic

Generic functionality lives in reusable function-oriented components.

**Composition example:** Instead of `ProductProvider` inheriting `SearchProviderBase<Product>`, we compose `SearchProvider<Product, ProductFilter>` and `CrudProvider<Product>` and inject them into endpoint modules when necessary. This prevents inheritance trees, keeps providers stateless, encourages single-responsibility classes, and avoids "God Providers" with many methods.

> [!NOTE] Note
> This also prevents users from sharing state at the provider instance level between different methods, which is wrong. Now, different functionalities will live in actual different instances. SearchProvider won't reach the ActivableProvider nor the UserRoleProvider.

> [!NOTE] Note
> This approach also highly discourage the use of hidden overrideable methods forcing programmers to solve life cycle events at the domain level, not the provider level.

### 3. Evolution Strategy

When repeated patterns emerge, promote them into:

- An endpoint module (`MapX`)
- A function-oriented Provider
- A function-oriented Repository

This allows the framework to evolve incrementally without breaking changes. We no longer modify base classes, we add modules.

### 4. Architectural Benefits

- **Reduced inheritance hell:** Inheritance is replaced with composition and explicit module registration.
- **Better separation of concerns:** Endpoint modules handle HTTP; providers handle application orchestration; repositories handle persistence. No class needs to know about everything.
- **Stateless by design:** Function-oriented providers discourage accidental state.
- **Explicit over magic:** No dynamic route injection, reflection-heavy conventions, or hidden MVC behaviors. Everything is registered explicitly.
- **Vertical slice architecture:** Capabilities are implemented as independent slices rather than extensions of entity hierarchies.

### 5. Design Principles

This approach aligns with:

- Lean Architecture
- DDD (application services separated from infrastructure)
- Composition over Inheritance
- Explicit configuration over implicit convention
- Evolvability over rigidity

## Alternatives Considered

**Status quo (inheritance-based model):** Keep entity-oriented base classes and document override contracts and patterns better. Advantages: no migration, familiar to current users. Disadvantages: does not fix combinatorial explosion, tight coupling, or the structural incentive to put logic in provider overrides; documentation alone cannot make deep inheritance and hidden contracts easy to reason about. Rejected in favor of a compositional design that addresses the root causes.

**Decorator Pattern for Providers and Repository:** This generates a much verbose code and it doesn't truly solves the issue. If I need all functions, I'll need a decorator wrapping the entire sets of features, which is pretty much the problem that we have today.

**Extensions Methods:** We also explored the idea of using C# extension methods to resolve the Provider/Repository issue. Having something like SearchProviderExtensions or ActivableProviderExtensions. But this makes us expose certain repository or base provider methods as public to be accessed by the extensions, which is something we don't want.

## Trade-offs

| Previous Model            | Proposed Model        |
|---------------------------|------------------------|
| Inheritance-driven        | Composition-driven    |
| Entity-oriented           | Function-oriented     |
| Centralized base classes  | Modular capabilities  |
| Hard to extend            | Easy to extend        |
| Hidden coupling           | Explicit dependencies |

The trade-off is slightly more explicit wiring, but the gain in clarity, flexibility, and long-term maintainability outweighs this cost.

## Testing Strategy

To be defined during implementation planning. Expected elements:

- Unit tests for endpoint modules and function-oriented providers/repositories.
- Integration tests for composed endpoints (CRUD, Search, Activable) against a test API.
- Verification that OpenAPI/Swagger output remains correct for composed routes.
- Manual verification of migration path from existing controller/provider/repository implementations.

## Rollout Plan

This would be a major version change. Considerations:

- **Backward compatibility:** Determine whether to support a compatibility layer (e.g. legacy base controllers delegating to new endpoint modules) or a clean break with migration guidance.
- **Phased deployment:** Optionally introduce endpoint modules and function-oriented types alongside existing types, deprecate old bases, then remove in a later major version.
- **Documentation:** Migration guide from current controller/provider/repository patterns to endpoint modules and function-oriented composition.
- **User communication:** Announce in release notes and point to the migration guide and this RFC.

## Dependencies

- **Other RFCs:** [RFC 2026-01-22: Developer Experience Pain Points Analysis](./2026-01-22-developer-experience-improvements.md) — this RFC is a proposed solution for Problem Areas 1 and 4 only.
- **Codebase:** Existing controller, provider, and repository base classes and their usages; minimal endpoint (MapGroup) usage if any.
- **Timeline:** To be aligned with product roadmap and any other DX-related RFCs (e.g. code generation, documentation).

## Open Questions

1. **Migration path:** Full breaking change vs. compatibility shim and deprecation period?
2. **Incremental adoption:** Can projects adopt endpoint modules and function-oriented providers/repositories per entity or per capability, or is an all-or-nothing migration preferred?
3. **Naming and namespaces:** Final names for `MapCrud`, `MapSearch`, `MapActivable` and for `CrudProvider<T>`, `SearchProvider<T, TFilter>`, etc.
4. **Override callbacks:** How to handle existing use cases that rely on `BeforeAddAsync`, `AfterSaveAsync`, etc. — domain-level hooks, middleware, or small application services that wrap function-oriented providers?

## References

- [RFC 2026-01-22: Developer Experience Pain Points Analysis](./2026-01-22-developer-experience-improvements.md) — parent problem analysis; this RFC addresses Problem Areas 1 and 4.
- [EditProviderBase.cs](../../src/Paradigm.Enterprise.Providers/EditProviderBase.cs) — current protected lifecycle methods.
- [ReadRepositoryBase.cs](../../src/Paradigm.Enterprise.Data/Repositories/ReadRepositoryBase.cs) — current search pattern.
- [RFC: Join View Repository](./2025-05-12-join-view-repository.md) — example of simplifying framework patterns.
