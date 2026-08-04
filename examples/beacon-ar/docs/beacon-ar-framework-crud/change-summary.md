# Beacon AR framework CRUD change summary

> **Historical persistence note (superseded 2026-08-03):** This document records an earlier implementation stage. Current persistence ownership is defined by docs/beacon-ar-context-boundaries: four disjoint Access, MasterData, Operations, and Sales contexts/configurations; there are no Receivables or Reporting application roots.

## Master-data repositories

- Product, Customer, Address (`CustomerAddress`), and Carrier edit repository contracts now inherit `IEditRepository<TEntity, int>`.
- Their concrete edit repositories now inherit `EditRepositoryBase<TEntity, ReceivablesDbContext, int>`. Repository writes only stage changes; repositories do not map API DTOs, validate requests, or commit the unit of work.
- The four view repository contracts now inherit `IReadRepository<TGeneratedView, int>`, and their implementations inherit `ReadRepositoryBase<TGeneratedView, ReceivablesDbContext, int>`.
- Standard identifier and full-list reads use the generated `ProductView`, `CustomerView`, `CustomerAddressView`, and `CarrierView` DbSets through an `AsNoTracking` query.
- Existing stored-procedure searches remain responsible for filtering, deterministic ordering, and pagination. They now query the corresponding database view and materialize the generated `*View` shape directly through generator-owned data-reader mappers in one database round trip.
- The atomic `BeaconAr.CodeGenerator` `mappers` target now owns the exact `ProductView`, `CustomerView`, `CustomerAddressView`, and `CarrierView` result mappers plus their central registrations under `BeaconAr.Data/Mappers`. The obsolete master-data `*SearchRow` types, generated mappers, registrations, and the parallel handwritten `MasterData/Mappers` registry were removed.
- Application view repositories retain cancellation-aware identifier reads for the temporary controller contracts and implement the official `PaginationParametersBase` search hook with `MasterDataViewSearchParameters`.

## Master-data providers

- The four provider contracts now inherit `IEditProvider<TGeneratedView, int>`.
- The providers now inherit the official `EditProviderBase<TInterface, TEntity, TView, TRepository, TViewRepository, int>` specialization.
- `MasterDataProviderBase` was removed. Transaction/error translation, audit creation, and concurrency comparison used by the temporary legacy controller adapters are composed through the sealed `MasterDataMutationCoordinator`.
- Legacy request and DTO mapping now occurs only in Providers. The existing controllers therefore retain their Task 3 HTTP contracts while generated views are canonical at the repository and official provider boundaries.
- Existing validation, duplicate checks, optimistic concurrency, referenced-delete rules, audit actions, rollback/discard behavior, and transaction ownership remain provider concerns.
- Creates retain two participant saves inside one explicit outer transaction: the first obtains the database-generated resource identifier, and the second persists the audit that references that identifier. Updates and deletes retain one participant save.
- Official generated-view search is enabled and validated. All eight official single/bulk mutation overloads are explicitly rejected until Task 4 defines a view transport that can preserve Beacon AR's ETag, audit, duplicate, and referenced-delete protocol; they cannot silently fall through to the unsafe generic lifecycle.

## Sales workflows

- `SalesProviderBase` was removed.
- Quote, SalesOrder, and QuoteConversion remain explicit named workflow providers implementing their exact-name `IProvider` contracts directly.
- Shared transaction, audit, concurrency, persistence-error translation, and tracked-change cleanup are composed through one scoped, constructor-injected sealed `SalesWorkflowCoordinator`; it is not another provider base. Only its class and constructor are public for cross-assembly DI registration; all workflow mechanics are internal to Providers.
- Workflow transaction boundaries, two-save generated-ID audits, histories, snapshots, status transitions, soft deletion, conversion idempotency, and conversion conflict recovery were preserved.

## Registration and tests

- `MasterDataMutationCoordinator` and `SalesWorkflowCoordinator` are registered as scoped in the Web API host. Existing repository/provider convention discovery remains unchanged.
- Architecture tests prove the official typed repository/provider inheritance, generated-view DbSet reads, provider-owned legacy DTO mapping, absence of local provider bases, generated mapper ownership/stale-artifact removal, and the narrow custom sales-provider boundary.
- Provider tests retain commit-count, rollback, concurrency, validation, referenced-delete, and audit assertions, and exercise official search plus every guarded mutation overload through `IEditProvider`. Existing sales workflow regression tests continue to exercise transaction and conversion behavior.
- The SQL project retains explicit nested `Folder` items so Visual Studio shows its complete logical tree. Final validation observed Visual Studio restoring `TargetDatabaseSet` and its project GUID; the solution entry and every mapping are now aligned to that single IDE-owned GUID. See the final-validation decision record for the superseding evidence.

## Verification

- Release solution build: passed with zero warnings and zero errors.
- Solution tests: 154 total, 113 passed, 41 skipped, zero failed. The skipped database integration and authenticated acceptance cases require `ConnectionStrings__DatabaseConnection`, which was not configured in this environment.
- Mapper generation completed twice; path and SHA-256 comparisons proved all 15 generated files were byte-for-byte identical after the second atomic run.
- `paradigm doctor`, `paradigm packages check`, `paradigm validate`, and database project validation: passed.
- Built-in semantic checks passed for Domain, Data, and Providers. Solution-wide checking could not semantically compile Aspire's generated `Projects` namespace, and Web API-only checking could not enable the OpenAPI source generator's interceptor namespace; the normal compiler and complete non-live test run succeeded.
- Package audit completed with existing outdated-package warnings only; no package versions were changed by this task.

## Guidance promoted

- The provider skill now treats typed provider inheritance as an executable contract: every single/bulk
  operation must be behavior-tested, and incomplete migrations must override the entire unsupported surface
  to fail closed.
- Repository guidance now requires one-command stored-procedure projection materialization and atomic,
  repeatable mapper regeneration with stale result artifacts removed.
- Review guidance now checks inherited provider behavior and generated mapper ownership instead of accepting
  inheritance or checked-in generated files as sufficient evidence.
- The deterministic skill and plugin validators pass after these guidance changes.
