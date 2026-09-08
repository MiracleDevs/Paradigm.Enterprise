# Beacon AR context boundaries — decisions

## Four contexts, no shared context

The database capabilities `Access`, `MasterData`, `Operations`, and `Sales` are the application persistence boundaries. `Receivables` is not a database capability and is removed. `Reporting` is a read responsibility rather than a fifth module; the dashboard contract, repository, Provider, routine, and mapper move to Operations.

No `SharedDbContext` is introduced. `ApplicationUser` is Access-owned even though its IDs are referenced for audit across the database. Audit ownership does not justify duplicating the entity in every EF model or creating an application-wide context.

## Cross-context foreign keys and reads

SQL Server remains the authoritative owner of all cross-context FK constraints. EF models expose scalar IDs for cross-context relationships and omit those navigations. Same-context aggregate/navigation relationships remain generated. This prevents conflicting tracking models and duplicate generated CLR types.

Sales owns snapshot creation and therefore retains `ISalesReferenceRepository` as an anti-corruption read contract. Typed Sales stored procedures read and lock the Access/MasterData-backed records required for a Sales operation. They return only the bounded scalar reference shapes; they do not expose MasterData entities or `IQueryable` across the boundary.

MasterData reference checks that cross into Sales use four explicit stored procedures. A generic routine accepting table/entity names was rejected because it would require branching/dynamic SQL and hide ownership.

Schema-bound Sales and MasterData views may join tables from another capability within the same database. A projection's SQL dependency does not transfer entity/context ownership.

## EF versus stored procedures

Ordinary tracked reads, creates, updates, deletes, includes, bounded uniqueness checks, and the address reparent update use EF. The context split removes the Sales-derived alternate-key mapping that forced the old address reparent SQL workaround; row-version concurrency is verified through EF.

Concurrency range locks, sequence allocation, atomic get-or-create, cross-context reference checks, cross-capability snapshot reads, pagination, and dashboard reporting use typed SQL Server stored procedures. Repositories contain routine names/wrappers and EF expressions, never handwritten SQL query/command text.

Tests and the finite database bootstrap may contain SQL because they are explicit verification/deployment boundaries, not application repositories. Task 6 must encode that false-positive boundary.

## Generation ownership

The four EFPT configs are the single generation source of truth. Their table/view selections are disjoint and together exactly equal the database selection. The existing official-derived generic T4 templates are reused unchanged unless a metadata-driven multi-context fixture exposes a truly generic defect.

Handwritten partial entity files live in the same capability `Entities` directory as their generated halves, matching the requested discoverability rule and the DMS-style capability layout. Therefore a directory is no longer wholly generated. Only the explicit 31 generated filenames and four generated context filenames are replaceable; cleanup, backup, recovery, and hash validation operate on exact files and ownership markers. Behavior partials and the MasterData context relationship partial are never deleted by regeneration.

Stored-procedure parameter/result types are explicit typed Data contracts because they define reviewed database result order and meaning. Their parameter/data-reader mappers and capability registrars are generated atomically. No parallel handwritten mapper registry is permitted.

## Unit of Work transaction model

All four contexts register with the same scoped `SqlServerDbContextConnectionProvider` and identical `DatabaseConnection` name. The provider returns the same `SqlConnection` instance, making the framework `DbContextTransaction` enlistment compatible across the contexts.

`CommitChangesAsync` remains sequential. Atomic cross-context workflows require an explicit Unit of Work transaction; resolving repositories alone does not make sequential saves atomic. Tests must prove rollback when a later context save fails and must cover repositories registered after transaction creation. This guarantee applies only to these contexts on the same SQL Server connection; it is not a distributed-transaction claim.

`PersistenceSession` remains a narrow rollback cleanup abstraction and clears all four change trackers. It does not become a context facade, repository locator, commit service, or transaction owner.

## Interfaces and later tasks

The existing analyzer continues generating getter-only entity interfaces, now in capability-aligned namespaces inferred from the Domain entity namespace. No empty physical Interface folders or compatibility interfaces are created.

This task intentionally does not move system enums to Interfaces, dismantle `SalesDomainValidation`, replace manual special-case DTOs, remove Provider validation/CRUD overrides, or create controller query DTOs. Those changes require their own behavioral review and belong to the following Domain and Provider/API tasks. Only namespace and compile-required changes are allowed here.

## Guidance and CLI ownership

This task promotes proven repository/context/generation lessons into concise skills and references. The reusable semantic enforcement belongs to Task 6: it must inspect production repository source for raw EF SQL APIs, command text, and SQL literals while allowing typed stored-procedure boundaries and explicit test/database/bootstrap SQL. Beacon also keeps a local architecture regression test until that CLI diagnostic is available.

## Reference-project usage

The DMS repository is precedent for capability folders, one context/config per capability, context-specific repositories/routines, and registering multiple contexts on one named connection. Its legacy EFPT schema and older handwritten/generated conventions are not copied blindly; Beacon retains its pinned current CLI schema, official-derived generic T4 provenance, generated metadata, guarded regeneration, and current generic identifier contracts.

## Verification tooling limitations

The example's pinned Paradigm CLI package `1.1.0` scans the SQL project's freshly built DACPAC as though it were a managed metadata assembly when `doctor` or whole-solution `validate` receives the mixed `.slnx`; it reports `PE1002 Unknown file format`. That older validation path also folds `DbContextBase<int>`'s audit actor type into capability identifier consistency and reports the intentionally `long` `AuditLog` and `IdempotencyRequest` IDs as mixed `int`/`long` (`PE3002`). The CLI built from the current repository source passes both commands, sees only the 15 managed projects for metadata, ignores the DACPAC, and reports both Operations IDs as `long`. Strict database validation and project-scoped Domain/Data/Provider checks also pass. This task does not weaken the deliberate `long` ledger IDs or remove the canonical SQL project to accommodate lagging installed-package heuristics.

Whole-solution `checks run` from both CLI builds still semantically compiles the Aspire AppHost without its SDK-generated `Projects` symbols. The consuming Domain, Data, and Provider projects are therefore checked directly until that command understands Aspire generated references. This is a CLI follow-up, not a context-boundary exception.
