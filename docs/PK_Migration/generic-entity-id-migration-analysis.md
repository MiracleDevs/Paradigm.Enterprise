# PK Migration — Generic Entity ID (TId) — Technical Impact Summary (2026-04-08)

## Objective
Enable the framework to support multiple primary key types (starting with `int` + `Guid`, extensible) by removing the hardcoded `int` identity from core abstractions and propagating the identity type through entities, repositories, providers, and Web API bases.

Context: existing projects use `int` as PK today; a new project must use `Guid` as PK, and the direction going forward is to support multiple types via generics (`TId`).

This document summarizes *current code evidence*, *impact areas*, *key risks*, and *design decisions to close*, so a Technical Planner can derive a sequential implementation plan.

## Current State (Evidence in repo)
The current framework hardcodes `int` identity across public contracts:

- Identity contract:
  - `IEntity` exposes `int Id`: [src/Paradigm.Enterprise.Interfaces/IEntity.cs](../../src/Paradigm.Enterprise.Interfaces/IEntity.cs)
- Entity base class:
  - `EntityBase` exposes `[Key] public int Id { get; set; }` + `IsNew() => Id == default`: [src/Paradigm.Enterprise.Domain/Entities/EntityBase.cs](../../src/Paradigm.Enterprise.Domain/Entities/EntityBase.cs)
- DTO base class:
  - `DtoBase` exposes `public int Id { get; set; }`: [src/Paradigm.Enterprise.Domain/Dtos/DtoBase.cs](../../src/Paradigm.Enterprise.Domain/Dtos/DtoBase.cs)
- Repository contracts:
  - `IReadRepository<TEntity>` defines `GetByIdAsync(int)`, `GetByIdsAsync(IEnumerable<int>)`: [src/Paradigm.Enterprise.Domain/Repositories/IReadRepository.cs](../../src/Paradigm.Enterprise.Domain/Repositories/IReadRepository.cs)
  - `ReadRepositoryBase<TEntity, TContext>` implements `GetByIdAsync(int)` and uses `ids.Contains(x.Id)`: [src/Paradigm.Enterprise.Data/Repositories/ReadRepositoryBase.cs](../../src/Paradigm.Enterprise.Data/Repositories/ReadRepositoryBase.cs)
- Provider contracts:
  - `IReadProvider<TView>` defines `GetByIdAsync(int)`, `GetByIdsAsync(IEnumerable<int>)`: [src/Paradigm.Enterprise.Providers/IReadProvider.cs](../../src/Paradigm.Enterprise.Providers/IReadProvider.cs)
  - `IEditProvider<TView>` defines `DeleteAsync(int)`/`DeleteAsync(IEnumerable<int>)`: [src/Paradigm.Enterprise.Providers/IEditProvider.cs](../../src/Paradigm.Enterprise.Providers/IEditProvider.cs)
- Web API base controllers:
  - `ReadApiControllerBase` uses `[FromQuery] int id`: [src/Paradigm.Enterprise.WebApi/Controllers/ReadApiControllerBase.cs](../../src/Paradigm.Enterprise.WebApi/Controllers/ReadApiControllerBase.cs)
  - `EditApiControllerBase` uses `[FromQuery] int id` for delete: [src/Paradigm.Enterprise.WebApi/Controllers/EditApiControllerBase.cs](../../src/Paradigm.Enterprise.WebApi/Controllers/EditApiControllerBase.cs)

### Audit & Logged User coupling (critical)
Audit currently assumes user identity is `int`:

- `IAuditableEntity` inherits `IEntity` (thus inherits `int Id`) and also hardcodes:
  - `int? CreatedByUserId`, `int? ModifiedByUserId`: [src/Paradigm.Enterprise.Interfaces/IAuditableEntity.cs](../../src/Paradigm.Enterprise.Interfaces/IAuditableEntity.cs)
- `DbContextBase.SaveChangesAsync`:
  - retrieves `IEntity` (non-generic)
  - calls `AuditEntity(entry.Entity, loggedUser.Id)`
  - `AuditEntity(IAuditableEntity entity, int loggedUserId)`
  - then `entity.Audit(loggedUserId)`
  - See: [src/Paradigm.Enterprise.Data/Context/DbContextBase.cs](../../src/Paradigm.Enterprise.Data/Context/DbContextBase.cs)
- `IAuditableEntityExtensions.Audit(...)` accepts `int? userId`: [src/Paradigm.Enterprise.Domain/Extensions/IAuditableEntityExtensions.cs](../../src/Paradigm.Enterprise.Domain/Extensions/IAuditableEntityExtensions.cs)
- `ILoggedUserService` is typed over `IEntity` (non-generic): [src/Paradigm.Enterprise.Domain/Services/ILoggedUserService.cs](../../src/Paradigm.Enterprise.Domain/Services/ILoggedUserService.cs)

Given the requirement “all Ids should be GUID in the new project” and the strategic direction “support multiple Id types”, the auditing surface must also become generic.

## RFC Alignment vs Current Repo Reality
The RFC (2026-04-07) proposes:
- `IEntity<TId>`
- `EntityBase<TId>`
- `IReadRepository<TEntity, TId>` / `IEditRepository<TEntity, TId>`
- provider bases generic over `TId`

Repo differences / gaps:
- This repo does not include entity-scaffolding templates under `src/`.
- However, EF Power Tools T4 templates are present at repo root:
  - [EntityType.t4](../../EntityType.t4)
  - [DbContext.t4](../../DbContext.t4)

Important RFC inconsistency to resolve:
- The RFC states provider interface (`IReadProvider<TView>`) “stays simple” and hides `TId`, but also expects `GetByIdAsync(TId id)`.
- In C#, if `GetByIdAsync` must be type-safe, **the consumer must see the type** (either `IReadProvider<TView, TId>` or a non-generic interface per id type). Otherwise you lose compile-time safety or must add overloads/adapters.

## Key Design Decisions to Close (before implementation)
### 1) Audit user id type
Decision: use the same `TId` for auditing user ids (no separate `TUserId`).

Implication:
- `CreatedByUserId` / `ModifiedByUserId` will be typed as `TId?`, matching the identity type used by `IEntity<TId>`.
- This assumes the logged user identity type aligns with the entity identity type in the applications that use auditing.

### 2) Public API shape for Providers and Web API
For type safety, either:

- Option A (recommended): Make provider contracts generic by id
  - `IReadProvider<TView, TId>` with `GetByIdAsync(TId id)` etc.
  - Controllers base become generic over `TId` (or remain concrete per controller)

or

- Option B: Keep `IReadProvider<TView>` non-generic but then you must accept one of:
  - losing type safety (e.g., `object id` / `string id`), or
  - duplicating per-id interfaces (e.g., `IReadProviderInt<TView>`, `IReadProviderGuid<TView>`), or
  - adding adapters at boundaries.

Given the objective (“support multiple types going forward” + type safety), Option A matches the RFC intent.

### 3) Generic constraints for TId
RFC suggests `where TId : struct, IEquatable<TId>`.

This is compatible with `int`, `Guid`, `long`, etc.

Notes:
- The RFC text mentions `IComparable` in Open Questions, but the proposed code uses `IEquatable<TId>`. Equality is what repositories need.

### 4) DTO identity
Currently `DtoBase` hardcodes `int`.

If DTOs are used as `TView` in providers/controllers, DTO identity must also be made generic or de-coupled from this base class. Otherwise, GUID entities will reintroduce `int` at the API boundary.

## Impacted Components (What will break)
### Interfaces & Domain
- `IEntity` → `IEntity<TId>` (breaking)
- `EntityBase` → `EntityBase<TId>` (breaking)
- `EntityBase<TInterface, TEntity, TView>` currently inherits the non-generic `EntityBase`; it must be re-based on the generic identity base.
- `DomainTracker<TEntity>` currently constrained to `Interfaces.IEntity` (non-generic): [src/Paradigm.Enterprise.Domain/Entities/DomainTracker.cs](../../src/Paradigm.Enterprise.Domain/Entities/DomainTracker.cs)

### Repositories
- `IReadRepository<TEntity>` / `IEditRepository<TEntity>` → generic over `TId` (breaking)
- `ReadRepositoryBase<TEntity, TContext>` / `EditRepositoryBase<TEntity, TContext>` must propagate `TId` and adjust `GetByIdAsync` signatures.

### Providers
- `IReadProvider<TView>` and `IEditProvider<TView>` currently fix `int` method args.
- Provider base classes use `view.Id` and call repository methods with `int`.

### Web API
- Base controllers currently accept `int` route/query.
- For GUID endpoints, controllers must accept `Guid` and routes should use `{id:guid}`.
- If base controllers are generic over id, they must be updated to `TId`.

### Audit Pipeline (high risk)
This is the most coupled area.

To support `TId` generically:
- `IAuditableEntity` must become generic over `TId` and inherit `IEntity<TId>`.
- `DbContextBase` and audit extensions must accept `TId` (or be generic).
- `ILoggedUserService` must expose a logged user with `IEntity<TId>`.

## EF Core & Query Translation Notes
- `EntityBase<TId> : IEntity<TId>` with `[Key] public TId Id { get; set; }` is supported by EF Core for common scalar types like `int` and `Guid`.
- `GetByIdsAsync` currently uses `ids.Contains(x.Id)`. For `int` and `Guid` this usually translates to SQL `IN (...)`. The RFC suggests rewriting to `Any`, but that may be unnecessary for `Guid` and could be less optimal; translation behavior should be verified in the target EF Core version.

## EF Power Tools T4 Templates — Required Changes
Templates used across projects:
- [EntityType.t4](../../EntityType.t4)
- [DbContext.t4](../../DbContext.t4)

### EntityType.t4
Current behavior relevant to identity:
- Skips scaffolding the `Id` property:
  - `foreach (var property in EntityType.GetProperties().Where(p => p.Name != "Id") ...)`
- Sets base class without id type:
  - Views: `: EntityBase`
  - Entities: `: EntityBase<I{EntityType.Name}, {EntityType.Name}, {EntityType.Name}View>`
- Adds audit interface hardcoded:
  - `Paradigm.Enterprise.Interfaces.IAuditableEntity<DateTimeOffset>`

Needed changes:
- Determine entity PK CLR type from EF metadata:
  - `var key = EntityType.FindPrimaryKey();`
  - get the PK property CLR type (typically `Id`)
- Generate the correct generic base, e.g.:
  - `EntityBase<TId>` for views and entities (or a derived generic base that preserves the mapping hooks)
- Update generated audit interface to include user id type (if the framework changes to `IAuditableEntity<TDate, TUserId>` or similar)
- Review any `==` comparisons on `Id` in mapping logic; safe for `int` + `Guid`, but if supporting arbitrary `struct` types, prefer `Equals`.

### DbContext.t4
Mostly unaffected by entity PK type.

Potential change if `DbContextBase` becomes generic over `TId` to support auditing:
- `public partial class <ContextName> : DbContextBase` would need to become `DbContextBase<TId>` (how `TId` is chosen must be defined at application level).

## Compatibility / Migration Considerations
- The RFC states “breaking change, no compatibility layer”. That matches the current impact: existing projects must be updated.
- Key property: if a consumer is typed as `IReadRepository<TEntity, int>`, it will still call `GetByIdAsync(int)` without adaptation.
- Controllers for `int` entities do not need signature changes *if* they depend on providers typed with `TId=int`.

## Testing Scope (impact hotspots)
Existing tests heavily assume `int` (mocks use `It.IsAny<int>()`, etc.). Expect systematic updates.

Highest-value validations:
- Entity `IsNew()` for `int` and `Guid`.
- Repository `GetByIdAsync`/`GetByIdsAsync` for `int` and `Guid`.
- Provider pass-through correctness for `int` and `Guid`.
- Audit stamping path in `DbContextBase.SaveChangesAsync` for the chosen `TUserId` design.

## Complexity & Main Risks
- Complexity: **High** (cross-cutting breaking change across Interfaces/Domain/Data/Providers/WebApi/Tests).
- Main risks:
  - Audit/logged-user generic design is not covered in the RFC but is mandatory given “all Ids are GUID” and future multi-type support.
  - Provider/WebApi type-safety requires exposing `TId` at the boundary; the RFC’s “hide `TId`” rationale needs adjustment.
  - T4 template updates are required to avoid manual post-scaffold edits.

## Decisions & Remaining Questions

### Clarification: Audit question (why it matters)
The audit question was whether auditing should use a separate generic user id type (`TUserId`) or reuse the same identity generic (`TId`).

Decision taken: reuse `TId`.

Remaining design detail:
- Define the target audit contract shape, e.g. `IAuditableEntity<TId>` / `IAuditableEntity<TDate, TId>` and how `DbContextBase` obtains a logged user id of type `TId`.

### Confirmed decisions (from discussion)
2) Provider contract: use `IReadProvider<TView, TId>` (and corresponding edit provider) to keep type safety end-to-end.

3) DTO identity strategy: introduce `DtoBase<TId>` (or otherwise ensure views used in providers/controllers expose `Id` as `TId`).

4) EF Power Tools output expectations: templates generate Entities and Views.
