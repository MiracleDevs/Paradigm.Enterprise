# PK Migration — Task Plan (Generic `TId`) — 2026-04-08

This plan assumes the following decisions are already locked:
- Identity becomes generic via `TId` everywhere (`int`, `Guid`, extensible).
- Auditing uses the same `TId` (no separate `TUserId`).
- Providers become `IReadProvider<TView, TId>` / `IEditProvider<TView, TId>`.
- Views/DTOs expose identity via `DtoBase<TId>` (or equivalent), so API surface remains type-safe.
- EF Power Tools templates generate Entities and Views and must be updated.

## Backend

### 0) Preparation / guardrails
- Add a short build checklist section to this folder (optional) stating how to build/test locally (solution command + test command).
- Identify the “canonical” projects to update first (Interfaces → Domain → Data → Providers → WebApi → Tests).

### 1) Interfaces project — introduce generic identity contracts
- Add a non-generic marker `IEntity` (keeps `IsNew()` only) to avoid spreading `TId` into types that don’t need it (e.g. `DomainTracker`).
- Update `IEntity` to `IEntity<TId> : IEntity` with constraints (`where TId : struct, IEquatable<TId>`).
- Update all interfaces depending on `IEntity` to the generic form.
  - `IAuditableEntity` becomes `IAuditableEntity<TId> : IEntity<TId>`.
  - `IAuditableEntity<TDate>` becomes `IAuditableEntity<TDate, TId> : IAuditableEntity<TId>` (or equivalent chosen shape).
- Update any other interface constraints currently written as `where ... : IEntity`.

Verification:
- Interfaces project compiles.

### 2) Domain project — generic entity base + mapping base wiring
- Introduce a non-generic marker `EntityBase : IEntity` (abstract `IsNew()`), then implement `EntityBase<TId> : EntityBase, IEntity<TId>` (generic key + `IsNew()` using `Id.Equals(default)`).
- Rebase `EntityBase<TInterface, TEntity, TView>` on the new identity base.
  - Decide whether it becomes `EntityBase<TId, TInterface, TEntity, TView>` or similar to keep `TId` accessible.
  - Ensure mapping methods (`MapFrom`, `MapTo`) remain consistent.
- Keep `DomainTracker<TEntity>` as a single generic using the marker `IEntity` (no `TId` needed).
- Update `EntityMapperBase<...>` constraints to the new generic `IEntity<TId>` (note: mapper base becomes `EntityMapperBase<TId, ...>`).

Verification:
- Domain project compiles.

### 3) Domain DTOs — generic identity on views
- Introduce `DtoBase<TId>` (or equivalent) and migrate DTO/view types that are used as provider/controller `TView`.
- Ensure `IsNew()` logic is generic (`Id.Equals(default)`), not `Id == 0`.

Verification:
- Domain project compiles.

### 4) Data layer — generic repositories
- Update repository contracts:
  - `IReadRepository<TEntity, TId>` / `IEditRepository<TEntity, TId>`.
- Update base implementations:
  - `ReadRepositoryBase<TEntity, TContext, TId>`
  - `EditRepositoryBase<TEntity, TContext, TId>`
- Ensure `GetByIdAsync(TId id)` query remains EF-translatable.
- Decide on `GetByIdsAsync` implementation:
  - Prefer `ids.Contains(x.Id)` for `int`/`Guid` unless proven problematic.

Verification:
- Data projects compile (`Paradigm.Enterprise.Data`, plus provider-specific Data projects if they reference these bases).

### 5) Providers — generic providers + bases
- Update provider contracts:
  - `IReadProvider<TView, TId>` / `IEditProvider<TView, TId>` (and any existing derived provider interfaces).
- Update provider bases:
  - `ReadProviderBase<..., TId>`
  - `EditProviderBase<..., TId>`
- Ensure mapping calls that use `view.Id` remain correct with generic `TId`.

Verification:
- Providers project compiles.

### 6) WebApi — generic base controllers
- Update base controllers to accept `TId`:
  - `ReadApiControllerBase<TProvider, TView, TParameters, TId>`
  - `EditApiControllerBase<... , TId>`
- For route-based controllers in consuming apps:
  - `int` controllers keep `{id:int}` and `int id`.
  - `Guid` controllers use `{id:guid}` and `Guid id`.

Verification:
- WebApi project compiles.

### 7) Audit pipeline — make it generic on `TId`
- Update `IAuditableEntityExtensions` to accept `TId?` and target `IAuditableEntity<TDate, TId>`.
- Implement audit stamping via a generic context:
  - Keep `DbContextBase` as a non-generic base (no audit logic).
  - Add `DbContextBase<TId> : DbContextBase` that overrides `SaveChangesAsync` and audits `IAuditableEntity<TId>`.
- Make logged-user service generic:
  - Replace `ILoggedUserService` with `ILoggedUserService<TId>`.
  - Replace `LoggedUserService` with `LoggedUserService<TId>`.
  - Register as open-generic in DI: `ILoggedUserService<>` → `LoggedUserService<>`.

Verification:
- Data + Domain compile; audit stamping compiles end-to-end.

### 8) EF Power Tools templates — update scaffolding output
- Update Entity template (repo root):
  - Modify [EntityType.t4](../../../EntityType.t4) to:
    - Detect PK CLR type via EF metadata (`EntityType.FindPrimaryKey()`).
    - Generate entities/views inheriting from the new generic base (`EntityBase<...>` with concrete `TId`).
    - Emit the correct generic auditable interface (`IAuditableEntity<... , TId>`).
- Update DbContext template if needed:
  - Modify [DbContext.t4](../../../DbContext.t4) only if `DbContextBase` becomes generic and must be referenced as such.

Verification:
- Scaffold a sample model with `int` PK and ensure generated code compiles.
- Scaffold a sample model with `Guid` PK and ensure generated code compiles.

### 9) Update internal usages and constraints across the solution
- Search and replace remaining `GetByIdAsync(int)`, `GetByIdsAsync(IEnumerable<int>)`, `DeleteAsync(int)` usages in framework projects.
- Update constraints `where ... : IEntity` and `where ... : EntityBase` to their generic equivalents.

Verification:
- Entire solution compiles.

## Database
- No framework DB migration steps in this repo.
- For consuming applications:
  - Update schema PK types to `uniqueidentifier` where required.
  - Update FK columns matching `CreatedByUserId` / `ModifiedByUserId` to the chosen type.
  - Validate EF model snapshots/migrations reflect the new PK type.

## Frontend
- N/A in this repository.

## Technical testing

### 1) Update unit tests to generic ids
- Update mocks and verifications that use `It.IsAny<int>()` to use generic `TId` or concrete tests per type.
- Add/adjust tests for:
  - `IsNew()` for `int` and `Guid`.
  - Repository `GetByIdAsync` / `GetByIdsAsync` for `int` and `Guid`.
  - Provider path for `int` and `Guid`.
  - Audit stamping path (one test per identity type, if feasible).

### 2) Add a minimal “dual-id” test matrix (if feasible)
- Create two test entity types:
  - one with `int` id
  - one with `Guid` id
- Ensure both compile and run within the same test project.

### 3) Build & test gates
- Run solution build.
- Run `Paradigm.Enterprise.Tests`.

## Deliverables
- Updated framework abstractions supporting `int` + `Guid` via `TId`.
- Updated EF Power Tools T4 templates to scaffold compliant Entities/Views.
- Updated tests demonstrating both id types.
