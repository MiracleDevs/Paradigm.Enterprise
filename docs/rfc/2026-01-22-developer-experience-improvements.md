# RFC: Developer Experience Pain Points Analysis

- **RFC ID**: 2026-01-22-developer-experience-improvements
- **Status**: Draft
- **Author(s)**: Pablo Ordóñez
- **Created**: 2026-01-22
- **Last Updated**: 2026-01-22

## Summary

This RFC is a **problem analysis document** that captures all usability challenges discovered during real-world framework implementation. The goal is to thoroughly document each issue before proposing solutions, allowing the team to discuss and potentially create **individual RFCs for each problem area**.

The framework currently requires developers to have deep knowledge of the source code to effectively extend and customize it. Protected methods, code generation tooling, and inheritance patterns are not discoverable or well-documented, creating a steep learning curve that prevents semi-senior developers from quickly kick-starting new projects.

## Motivation

After implementing the framework in a project from scratch, several issues were discovered that made the development experience unnecessarily difficult. The framework's power and flexibility come at the cost of discoverability and ease of use. The objective is for a semi-senior developer or above to be able to kick-start a project without too many problems, making the experience as simple as possible for daily use.

This RFC documents four major problem areas:

1. **Interface Contracts Require Too Much Hidden Knowledge** - Protected methods and lifecycle hooks are not discoverable without source code access
2. **T4 Code Generation Challenges** - Multiple issues with entity generation, source generators, and template distribution
3. **Coupled Code Generation Processes** - Different generation tasks run together unnecessarily
4. **Undocumented Inheritance Patterns** - Common patterns like Activation and Search are implemented but not documented

Each problem area is documented in detail below to facilitate discussion and potential creation of individual solution RFCs.

## Problem Area 1: Interface Contracts Require Too Much Hidden Knowledge

### The Problem

Protected methods in base classes are not discoverable without reading source code. Documentation is insufficient, and developers must understand implicit contracts to extend the framework correctly. There are plenty of overrideable protected methods that are not necessarily clear from day one, and the documentation is lacking at best, so you must have access to the source code to study it to solve it.

### Evidence from Codebase

**EditProviderBase.cs** (lines 279-378) has **14 protected virtual methods** that form a complex lifecycle:

- `BeforeAddAsync(TView view)` - Executed before the view is mapped to entity
- `BeforeAddAsync(TEntity entity)` - Executed after mapping, before save
- `AfterAddAsync(TEntity entity)` - Executed after entity is added
- `BeforeUpdateAsync(TView view)` - Executed before the view is mapped to entity
- `BeforeUpdateAsync(TEntity entity)` - Executed after mapping, before save
- `AfterUpdateAsync(TEntity entity)` - Executed after entity is updated
- `BeforeSaveAsync(TView view)` - Executed before mapping (for both add and update)
- `BeforeSaveAsync(TEntity entity)` - Executed after mapping (for both add and update)
- `AfterSaveAsync(TEntity entity)` - Executed after save (for both add and update)
- `BeforeDeleteAsync(TEntity entity)` - Executed before entity deletion
- `AfterDeleteAsync(TEntity entity)` - Executed after entity deletion

**ReadRepositoryBase.cs** (line 91):

```csharp
protected virtual Func<PaginationParametersBase, Task<(PaginationInfo, List<TEntity>)>> GetSearchPaginatedFunction(PaginationParametersBase parameters)
    => throw new NotImplementedException();
```

- Throws `NotImplementedException` by default
- Not obvious from the interface that this MUST be overridden for search to work
- No guidance on how to implement it correctly

**EntityBase.cs** (lines 36-65):

- `MapFrom()` returns `default` by default - easy to miss that it needs implementation
- `MapTo()` throws `NotImplementedException` - forces implementation but not discoverable
- `Validate()`, `BeforeMapping()`, `AfterMapping()` - empty by default but part of lifecycle
- No clear documentation on when each method is called or in what order

### Impact

- **Discoverability**: Developers discover these methods only when things break or when they dig into source code
- **Execution Order Confusion**: The relationship between `BeforeAdd` vs `BeforeSave` is unclear - when is each called?
- **IDE Support**: No IDE discoverability without source code access or comprehensive XML documentation
- **Learning Curve**: Steep learning curve for new team members who must study framework internals
- **Trial and Error**: Developers resort to trial and error to understand the lifecycle, leading to bugs

### DDD Design Rule Violations

The protected methods in providers also make it difficult for users to respect Domain-Driven Design (DDD) principles. A lot of developers override `Before` methods to mutate domain entities instead of writing domain code in the domain layer where it belongs.

**The Problem:**

- **Business Logic in Wrong Layer**: Developers are tempted to put business logic in provider hooks rather than in domain entities
- **Entity Mutation in Providers**: `BeforeAddAsync(TEntity entity)` and `BeforeUpdateAsync(TEntity entity)` encourage mutating entities in the application layer
- **Domain Logic Leakage**: Initialization and business rules end up in providers instead of domain entities

**Initialization Logic Challenges:**

It's difficult within the domain to make initialization logic for new entities vs pre-existing entities, executing logic before or after mapping depending on these scenarios. Simple things like:

- Initializing variables like `IsActive` to `true` only when it's a new entity, just after mapping
- Avoiding changing the value of an existing entity during updates
- Different initialization rules for new vs existing entities

**Example Anti-Pattern:**

```csharp
// ❌ Bad: Business logic in provider
public class ProductProvider : EditProviderBase<...>
{
    protected override async Task BeforeAddAsync(Product entity)
    {
        // This should be in the domain entity, not the provider
        entity.IsActive = true;
        entity.CreatedDate = DateTime.UtcNow;
        await base.BeforeAddAsync(entity);
    }
}
```

**What Should Happen:**

```csharp
// ✅ Good: Business logic in domain entity
public class Product : EntityBase<...>
{
    public override Product? MapFrom(IServiceProvider serviceProvider, IProduct model)
    {
        // Initialize only for new entities
        if (this.IsNew())
        {
            this.IsActive = true;
            this.CreatedDate = DateTime.UtcNow;
        }
        // ... rest of mapping
        return this;
    }
}
```

**Current Limitations:**

- No clear way to distinguish "new entity" vs "existing entity" in domain mapping methods
- `BeforeMapping()` and `AfterMapping()` don't provide context about entity state
- Developers resort to provider hooks because domain methods lack necessary context

### Real-World Scenario

A developer wants to add custom validation before saving an entity. They need to know:

- Should they override `BeforeSaveAsync(TView)` or `BeforeSaveAsync(TEntity)`?
- What's the difference between `BeforeAddAsync` and `BeforeSaveAsync`?
- When is validation called relative to these hooks?
- Without source code access, they cannot answer these questions.

Additionally, they need to initialize `IsActive = true` for new products but not change it for existing ones. The current pattern encourages doing this in `BeforeAddAsync(TEntity)`, which violates DDD principles by putting business logic in the application layer instead of the domain layer.

---

## Problem Area 2: T4 Code Generation Challenges

### Problem 2a: Generates All Aggregates/Relationships Unconditionally

T4 templates generate ALL aggregates and associations (relationships) even if you don't actually want them. This results in:

- **DTOs filled with empty `List<T>` properties** - Every relationship generates a collection property
- **Unnecessary data transfer overhead** - Empty collections are serialized and sent over the wire
- **Confusing API contracts** - API consumers see properties that are always null or empty
- **No configuration option** - There's no way to tell the T4 templates "skip this relationship" or "only generate these aggregates"

**Example Impact:**

```csharp
// Generated DTO - includes all relationships even if not needed
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<OrderLineDto> OrderLines { get; set; } = new(); // Always empty, never used
    public List<CategoryDto> Categories { get; set; } = new();    // Always empty, never used
    public List<ReviewDto> Reviews { get; set; } = new();        // Always empty, never used
}
```

It would be awesome to maybe be able to tell the T4 templates what and what not to generate.

### Problem 2b: Source Generator Compilation Dependency

The domain interfaces are being generated with source generators, and it's annoying because if something does not compile, the interfaces won't generate, and you start having issues with references all across the solution.

**The Chicken-and-Egg Problem:**

1. Developer writes entity code that doesn't compile (syntax error, missing using, etc.)
2. Source generator runs during compilation
3. Source generator can't analyze non-compiling code
4. Interfaces don't generate
5. Other parts of solution that depend on interfaces fail to compile
6. Cascading compilation failures make debugging difficult
7. IDE IntelliSense breaks during development

**Current Location:** Source generators are in `src/Paradigm.Enterprise.CodeGenerator/Generators/`

**Ideal Scenario:** Generate the domain interfaces when generating the domain entities and the context, all together. Either extending the EF Tools, or by creating a custom method. This would:

- Generate interfaces as part of the design-time tooling, not compile-time
- Allow interfaces to be generated even if current code doesn't compile
- Provide a clear separation between generation and compilation phases

### Problem 2c: Templates Not Part of Library Distribution

The source generators from the `@src/Paradigm.Enterprise.CodeGenerator/Generators/` and the T4 templates must be manually copied when something new is added, and they are not necessarily part of the library. At least not in the other project that bootstraps the generation of source code.

**Current Issues:**

- **Manual Copying Required**: When starting a new project, developers must manually copy T4 templates and source generator code
- **Not in NuGet Packages**: Templates are not distributed as part of the library packages
- **No Versioning**: No mechanism to ensure template versions match library versions
- **Update Challenges**: When framework updates, templates may not be updated in existing projects
- **Bootstrap Project Gap**: The bootstrap/example project doesn't include the latest templates

**Impact:**

- Projects can have mismatched template versions
- New features in templates aren't available to existing projects
- Inconsistent code generation across projects
- Maintenance burden on framework maintainers to keep templates in sync

---

## Problem Area 3: Coupled Code Generation Processes

### The Problem

The CodeGeneration code is executed at the same time as the API proxies for the client, and sometimes that can be messy, or unnecessarily slower, as these are two different processes.

**Current Implementation** (`Application.cs` lines 35-39):

```csharp
public async Task ExecuteAsync()
{
    _serviceProvider.GetRequiredService<JsonContextGenerator>().GenerateCode();
    _serviceProvider.GetRequiredService<StoredProcedureMapperGenerator>().GenerateCode();
    await _serviceProvider.GetRequiredService<ProxiesGenerator>().GenerateCodeAsync();
}
```

All generators run together in a single execution:

- **JsonContextGenerator** - Backend serialization contexts (design-time, no runtime dependency)
- **StoredProcedureMapperGenerator** - Database mappers (design-time, no runtime dependency)
- **ProxiesGenerator** - Frontend TypeScript API clients (requires running API server with Swagger)

### Impact

- **Different Cadences**: Backend changes don't always require new client proxies
- **Deployment Dependency**: Proxies require a running API server (SwaggerUrl) - adds deployment complexity
- **Slower Iteration**: When only changing backend code, regenerating proxies is unnecessary overhead
- **Unnecessary Regeneration**: Unchanged artifacts are regenerated, wasting time
- **Process Coupling**: Two fundamentally different processes (design-time generation vs. runtime API introspection) are coupled

### Real-World Scenario

A developer makes a small change to a backend entity property. They need to:

1. Regenerate JSON contexts (fast, local)
2. Regenerate stored procedure mappers (fast, local)
3. Start the API server (slow, requires database connection)
4. Wait for Swagger to be available
5. Generate TypeScript proxies (slow, network call)

Steps 3-5 are unnecessary if only backend changed, but the current architecture forces them to run together.

---

## Problem Area 4: Undocumented Inheritance Patterns and Inheritance Hell

### The Problem

There are plenty of inheritance or implementation patterns not documented for Activation, Search, etc. that makes the use more difficult. The idea is to try to "solve" cases that are super common and repeatable, but it makes difficult to customize or escalate. And if you don't have the proper generations, the code won't compile.

Additionally, the framework is heading toward "inheritance hell" with too many base class combinations that force unnecessary complexity and cause decision paralysis.

### Specific Undocumented Patterns

**Search/Pagination Pattern:**

- Must override `GetSearchPaginatedFunction` in `ReadRepositoryBase`
- **Forces search implementation even when not necessary** - Not all entities need search functionality
- **Adds generic parameters everywhere** - Base classes and interfaces require search parameter types even if unused
- No documentation on:
  - How to implement the function correctly
  - What parameters are available
  - How to handle filtering, sorting, pagination
  - Examples of common implementations
- Default implementation throws `NotImplementedException` - not discoverable

**Inheritance Hell Problem:**

The current architecture forces developers to choose from multiple inheritance combinations:

- `ReadRepositoryBase<TEntity, TContext>` - For read-only operations
- `EditRepositoryBase<TEntity, TContext>` - For write operations (inherits from ReadRepositoryBase)
- `ReadRepositoryBase<TEntity, TContext>` with search - Requires implementing `GetSearchPaginatedFunction`
- Controllers with similar issues - `ReadApiControllerBase`, `EditApiControllerBase`, each requiring search parameters

**The Combinatorial Explosion:**

```
ReadRepository (no search)
ReadRepository (with search) → adds TParameters generic
EditRepository (no search) → inherits ReadRepository
EditRepository (with search) → inherits ReadRepository + adds TParameters
ReadController (no search)
ReadController (with search) → adds TParameters generic
EditController (no search) → inherits ReadController
EditController (with search) → inherits ReadController + adds TParameters
```

This creates **decision paralysis**:

- Do I need search? (Maybe not now, but maybe later?)
- Should I add search parameters now to avoid refactoring later?
- Which base class should I inherit from?
- What if I need search later but didn't plan for it?

**Similar Issue with Controllers:**

Controllers suffer from the same problem - they require search parameter types as generics even when search isn't needed, forcing unnecessary complexity:

```csharp
// Forces TParameters even if search is never used
public abstract class ReadApiControllerBase<TProvider, TView, TParameters>
    where TParameters : PaginationParametersBase
{
    // ...
}
```

**Composition Over Inheritance:**

The framework should consider moving to a **composition-based approach** rather than deep inheritance hierarchies:

- **Current (Inheritance)**: `EditRepositoryBase` → `ReadRepositoryBase` → `RepositoryBase`
- **Proposed (Composition)**: Repository with pluggable capabilities (Read, Write, Search, etc.)

This would allow:

- Entities without search don't pay the cost of search generics
- Easy to add capabilities later without refactoring
- Clear, explicit dependencies instead of hidden inheritance chains
- Avoid "inheritance hell" with exponential combinations

**Activation Patterns:**

- Presumably for soft-delete or status management
- No documentation found in framework docs
- Pattern exists but usage is unclear
- How to enable/disable activation for an entity?

**Custom Filtering:**

- Extension points exist in the repository pattern
- No guide on how to extend filtering behavior
- How to add custom query logic?

**Entity Mapping Lifecycle:**

- `BeforeMapping()`, `AfterMapping()`, `Validate()` methods exist
- Execution order not documented
- When to use each method?

### Impact

- **Framework Intent vs. Reality**: Framework tries to solve common, repeatable cases, but customization/escalation is difficult without documentation
- **Compilation Failures**: Missing proper code generation → code won't compile
- **Workarounds**: Developers resort to workarounds instead of using intended patterns
- **Inconsistent Implementations**: Without documentation, each developer implements patterns differently
- **Knowledge Gaps**: Patterns that could save time are unknown to most developers
- **Decision Paralysis**: Too many inheritance combinations make it unclear which base class to use
- **Unnecessary Complexity**: Entities without search are forced to include search parameter generics
- **Refactoring Burden**: Adding search later requires changing base classes and generic parameters throughout the codebase
- **Inheritance Hell**: Exponential growth of base class combinations as features are added

### Real-World Scenario

A developer needs to implement soft-delete functionality. They:

1. Don't know if "Activation" pattern is what they need
2. Can't find documentation on how to use it
3. Implement their own solution (reinventing the wheel)
4. Later discover the framework had a pattern for this, but it's too late to refactor

---

## Key Files Reference

| File | Relevance |
|------|-----------|
| `src/Paradigm.Enterprise.Providers/EditProviderBase.cs` | 14 protected lifecycle methods (lines 279-378) |
| `src/Paradigm.Enterprise.Providers/ReadProviderBase.cs` | Search delegation pattern |
| `src/Paradigm.Enterprise.Data/Repositories/ReadRepositoryBase.cs` | `GetSearchPaginatedFunction` throws by default (line 91) |
| `src/Paradigm.Enterprise.Domain/Entities/EntityBase.cs` | Mapping contract and lifecycle methods |
| `src/Paradigm.Enterprise.CodeGenerator/Application.cs` | Coupled generation execution (lines 35-39) |
| `src/Paradigm.Enterprise.CodeGenerator/Generators/` | Source generators location |

---

## Open Questions for Discussion

Before proposing solutions, the team should discuss:

1. **Scope of Changes**: Should we address these as one large refactor or individual focused RFCs for each problem area?

2. **Breaking Changes**: What is the acceptable level of breaking changes for a v2.0 release? Can we maintain backward compatibility?

3. **Generation Approach**: Should T4 be replaced entirely with a different generation approach (e.g., dotnet tool, Roslyn analyzers, or design-time code generation)?

4. **Convention vs. Configuration**: How do we balance "convention over configuration" (easier for common cases) with explicit discoverability (easier to understand and customize)?

5. **Pattern Complexity**: Is the current Provider/Repository/Entity/View pattern too complex for most use cases? Should we provide simpler alternatives?

6. **Composition vs. Inheritance**: Should we move from deep inheritance hierarchies to composition-based capabilities? How would this affect existing code?

7. **Documentation Strategy**: Should documentation be:
   - Inline XML comments with examples?
   - Separate documentation site with guides?
   - Both?

8. **Template Distribution**: How should templates be distributed?
   - As a NuGet package?
   - As a dotnet tool?
   - As part of project templates?

9. **Generation Decoupling**: Should generation processes be:
   - Separate CLI commands?
   - Configuration flags to enable/disable each?
   - Both?

10. **DDD Alignment**: How can we better support DDD principles? Should domain entities have better hooks for new vs existing entity initialization?

---

## Next Steps

After team discussion and consensus on approach, create individual RFCs for approved solution areas:

- **RFC: Interface Contract Simplification** - Address Problem Area 1 with better discoverability and documentation
- **RFC: Function-Oriented Composition** - Address Problem Areas 1 and 4 with composition-based endpoint modules and function-oriented providers/repositories → [2026-02-11-function-oriented-composition.md](./2026-02-11-function-oriented-composition.md)
- **RFC: Code Generation Architecture v2** - Address Problem Area 2 with improved T4 templates, source generator alternatives, and template distribution
- **RFC: Decoupled Generation Commands** - Address Problem Area 3 with separate, configurable generation processes
- **RFC: Framework Documentation Overhaul** - Address Problem Area 4 with comprehensive guides for all patterns

Each individual RFC should include:

- Detailed design proposals
- Migration strategies
- Backward compatibility considerations
- Testing strategies
- Rollout plans

---

## References

- [EditProviderBase.cs](../../src/Paradigm.Enterprise.Providers/EditProviderBase.cs) - Protected lifecycle methods
- [ReadRepositoryBase.cs](../../src/Paradigm.Enterprise.Data/Repositories/ReadRepositoryBase.cs) - Search pattern implementation
- [EntityBase.cs](../../src/Paradigm.Enterprise.Domain/Entities/EntityBase.cs) - Entity mapping contract
- [Application.cs](../../src/Paradigm.Enterprise.CodeGenerator/Application.cs) - Code generation orchestration
- [RFC: Protected Aggregate Deletion Methods](./implemented/2025-01-15-protected-aggregate-deletion-methods.md) - Example of addressing framework usability issues
- [RFC: Join View Repository](./2025-05-12-join-view-repository.md) - Example of simplifying framework patterns
