# Beacon AR database view conventions implementation plan

> **Historical persistence note (superseded 2026-08-03):** This document records an earlier implementation stage. Current persistence ownership is defined by docs/beacon-ar-context-boundaries: four disjoint Access, MasterData, Operations, and Sales contexts/configurations; there are no Receivables or Reporting application roots.

## Objective and boundaries

Keep Beacon AR on SQL Server and make its database/read-model boundary unambiguous:

- every SQL view object, file, generated type, and EF mapping ends in `View`, including helper and reporting views;
- every table in the database `Operations` capability has a schema-bound, one-row-per-record view;
- database-first output is regenerated from the selected live SQL Server schema through T4 templates derived from the official `Paradigm.Web.ApiTemplate` source, never hand-edited;
- the new operational projections remain internal persistence shapes and are not exposed through repositories, Providers, controllers, JSON contexts, OpenAPI, or the generated client without a concrete use case;
- the task leaves the current single `ReceivablesDbContext` generated-output folders in place. The Access/MasterData/Operations/Sales context and folder partition is Task 3 and must consume the schema and generator boundary established here.

This task does not replace the SQL Server project, add a compatibility solution, change table names, create Operations CRUD endpoints, move partial behavior files, move enums, or repair the handwritten SQL in `IdempotencyRepository`; those belong to later scoped tasks.

## Evidence reviewed

- Paradigm database, domain-model, repository, review, and guidance-evolution skills and their required references.
- `examples/beacon-ar/src/database/BeaconAr.Database.sqlproj`, every SQL table/view/routine/deployment/verification file, EFPT configuration, T4 templates, generated Domain/context output, repositories, Providers, controllers, tests, OpenAPI artifact, client artifact, README, and prior task records.
- The capability-separated contexts and `*View` naming in `C:/Repositories/github/microsoft/rdx-dms-mvp/src/api/Microsoft.DemoManagementSystem.Data`.
- Official template source at `C:/Repositories/github/Paradigm.Web.ApiTemplate`, remote `https://github.com/MiracleDevs/Paradigm.Web.ApiTemplate`, revision `522906b9151d566a5dddd2609b66da36726368a7`.

The database is an SDK-style `Microsoft.Build.Sql` SQL Server 2022 project (`Sql160`). It already uses one object per file, explicit model item groups, schema binding on all current views, and a repository-owned finite Aspire/DACPAC publication path.

## Current view inventory and reference impact

No `CREATE VIEW` statements exist outside `src/database/views`. The current 11 objects are:

| Current object and file | SQL consumers | Generated/application consumers | Action |
| --- | --- | --- | --- |
| `dbo.ApplicationUserView` — `views/Access/ApplicationUserView.sql` | `FoundationSmoke.sql` | EFPT config; generated type/context; shared interface, architecture/live tests | Keep unchanged; regression-test. |
| `dbo.CarrierView` — `views/MasterData/CarrierView.sql` | `SearchCarrier.sql`, `FoundationSmoke.sql` | generated type/context, view repository, Provider/controller/OpenAPI/client/tests | Keep unchanged; regression-test. |
| `dbo.CustomerAddressView` — `views/MasterData/CustomerAddressView.sql` | `SearchAddress.sql`, `FoundationSmoke.sql` | generated type/context, view repository, Provider/controller/OpenAPI/client/tests | Keep unchanged; regression-test. |
| `dbo.CustomerView` — `views/MasterData/CustomerView.sql` | `SearchCustomer.sql`, `FoundationSmoke.sql` | generated type/context, view repository, Provider/controller/OpenAPI/client/tests | Keep unchanged; regression-test. |
| `dbo.ProductView` — `views/MasterData/ProductView.sql` | `SearchProduct.sql`, `FoundationSmoke.sql` | generated type/context, view repository, Provider/controller/OpenAPI/client/tests | Keep unchanged; regression-test. |
| `dbo.QuoteLineView` — `views/Sales/QuoteLineView.sql` | `FoundationSmoke.sql` | generated type/context, entity mapping and architecture/live tests | Keep unchanged; regression-test. |
| pre-convention pricing helper | joined only by `QuoteView.sql` | EFPT config, generated shape, `ReceivablesDbContext`, regeneration expected-file list, README/prior docs | Canonicalize everywhere as `QuotePricingView`; it remains an internal keyless aggregation view. |
| `dbo.QuoteView` — `views/Sales/QuoteView.sql` | `SearchQuote.sql`, `FoundationSmoke.sql`; joins pricing helper | generated type/context, repositories/Providers/controllers/tests | Keep public object stable; change only its helper join to `dbo.QuotePricingView`. |
| `dbo.SalesOrderLineView` — `views/Sales/SalesOrderLineView.sql` | `FoundationSmoke.sql` | generated type/context, entity mapping and architecture/live tests | Keep unchanged; regression-test. |
| pre-convention order-pricing helper | joined only by `SalesOrderView.sql` | EFPT config, generated shape, `ReceivablesDbContext`, regeneration expected-file list, README/prior docs | Canonicalize everywhere as `SalesOrderPricingView`; it remains an internal keyless aggregation view. |
| `dbo.SalesOrderView` — `views/Sales/SalesOrderView.sql` | `SearchSalesOrder.sql`, `FoundationSmoke.sql`; joins pricing helper | generated type/context, repositories/Providers/controllers/tests | Keep public object stable; change only its helper join to `dbo.SalesOrderPricingView`. |

The two pricing helpers are the complete noncompliant set. Their only runtime SQL consumers are the stable parent projections, so no stored procedure signature, repository contract, Provider, controller, JSON context, OpenAPI schema, or TypeScript client name changes are required.

## Operations table inventory and required projections

The explicit requirement overrides the normal rule that an internal table receives a view only for a concrete consumer. Add `src/database/views/Operations` and these three objects:

| Base table | Required view | Columns and joins | Cardinality/security boundary |
| --- | --- | --- | --- |
| `dbo.AuditLog` (`BIGINT Id`) | `dbo.AuditLogView` | Retain `Id`, `ResourceType`, `ResourceId`, `Action`, `UserId`, `RecordedAt`, `CorrelationId`, status-code fields, and `MetadataJson`; add `UserDisplayName` from `ApplicationUser`. | `WITH SCHEMABINDING`; required actor join is many-to-one and preserves one row per audit fact. `MetadataJson` is internal and must not become an API response. |
| `dbo.IdempotencyRequest` (`BIGINT Id`) | `dbo.IdempotencyRequestView` | Retain every base mapping column, including `UserId`, `Operation`, `KeyHash`, `RequestHash`, `StateId`, resource/response metadata, audit fields, completion, and expiration; add user/state/audit display fields (`UserDisplayName`, `StateCode`, `StateDisplayName`, `CreatedByUserDisplayName`, `ModifiedByUserDisplayName`). | `WITH SCHEMABINDING`; inner joins for required user/state, left joins for nullable audit users; one row per request. Hashes are operational replay controls, not credentials, but remain internal. The schema has no response-body column and none is to be introduced. |
| `dbo.IdempotencyState` (`INT Id`, assigned seed) | `dbo.IdempotencyStateView` | Retain `Id`, `Code`, `DisplayName`, and `IsActive`; no multiplicative join. | `WITH SCHEMABINDING`; one row per seeded state. It is not a new public enum/API contract. |

Do not add read/edit repositories, generic Providers, controllers, endpoints, or serializer registrations for these views. Generation may create public CLR persistence shapes and paired getter-only interfaces, but reachability through an HTTP action—not CLR visibility—governs API exposure. Add architecture/OpenAPI assertions so that the three operational view types and their raw hash/metadata fields cannot silently enter the public contract.

## Exact implementation sequence

### 1. Establish deterministic framework and template controls first

1. Add a database validator policy diagnostic `PEDB112` in `src/Paradigm.Enterprise.Cli/Database/DatabaseProjectValidator.cs`. Parse the created object kind as well as its qualified name and report every SQL Server `VIEW` and PostgreSQL `VIEW` whose object name does not end in `View`. Use `Policy` so legacy audit mode produces a warning and `--strict` produces an error. Keep deterministic code/location/message ordering. Existing `PEDB100` continues to require the filename to equal the object name, so a valid view necessarily has a `*View.sql` file.
2. Extend `DatabaseProjectValidatorTests` with SQL Server and PostgreSQL positive cases, non-suffixed negative cases, non-strict/strict severity assertions, and deterministic repeated-result/order coverage. Do not add a fixer or a suppression-free exception for helpers/reporting views.
3. Update `docs/cli.md`, `CHANGELOG.md`, `.agents/references/database-practices.md`, the database skill and SQL Server reference, and review guidance. State one durable distinction: helper/reporting views need not be named `{Entity}View`, but every view name/file still ends in `View` (for example `QuotePricingView`). Preserve the existing judgment-based rule for whether internal/status/history/audit/idempotency tables need a view; Beacon's all-Operations coverage is an explicit application decision, not a universal CLI rule.
4. Update `docs/tutorials/database-first.md` to require the official `Paradigm.Web.ApiTemplate` T4 files as the provenance source, explicit documented compatibility adaptations when installed APIs have advanced, no application type-name lists inside T4, generated ownership metadata, and regeneration/build/diff review.

### 2. Rename the two helper views atomically

1. Canonicalize the quote-pricing file and object as `QuotePricingView.sql` / `dbo.QuotePricingView` without changing its `QuoteId` grouping or totals.
2. Canonicalize the sales-order-pricing file and object as `SalesOrderPricingView.sql` / `dbo.SalesOrderPricingView` without changing its `SalesOrderId` grouping or totals.
3. Update the schema-bound joins in `QuoteView.sql` and `SalesOrderView.sql`. Keep both public parent view names and result columns stable.
4. Do not retain aliases under the old names: they would violate the stated convention. No pre/pre-pre/post-deployment script is required because views contain no data and the DACPAC can order the dependent parent-view alterations with helper drop/create. Review the generated deployment report for unexpected external dependencies or destructive table work before publication.

### 3. Add all three Operations views

1. Create one SQL file per object under `views/Operations` using the column/join contract above, explicit `[dbo]` qualifications, and `WITH SCHEMABINDING`.
2. Add `views/Operations/` to the SQL project's visible folder metadata; the existing `views/**/*.sql` build include owns the model items, so do not add duplicate explicit files.
3. Extend `FoundationSmoke.sql` to require all 14 final view names (the nine existing entity projections, two renamed pricing helpers, and three Operations projections), require schema binding for all of them, and check one-row cardinality. Use `Id` for Operations entity views, `QuoteId`/`SalesOrderId` for helpers, and base/view count parity for each Operations projection.

### 4. Replace the invented T4 boundary and regenerate, without hand edits

The sibling official template is read-only. Its current revision is the provenance baseline, but it targets the older non-generic Paradigm API: it emits `EntityBase<IEntity, Entity, EntityView>`, `IAuditableEntity<DateTimeOffset>`, and non-generic `DbContextBase`, while installed 1.1 APIs require generic identifiers. A byte-for-byte copy cannot compile.

1. Replace both Beacon T4 files from the official revision, then apply only reusable compatibility adaptations required by installed Paradigm/EFPT contracts:
   - infer each entity/view identifier CLR value type and emit `EntityBase<TId>` / `EntityBase<TId,TInterface,TEntity,TView>` / `EntityMapperBase<TId,...>` only when an `Id` and paired `{Entity}View` support that mapping;
   - infer audit actor ID from `CreatedByUserId`/`ModifiedByUserId` rather than assuming it equals the entity key (`IdempotencyRequest` and `AuditLog` have `long` entity keys but `int` actor keys);
   - emit the current `DbContextBase<int>` application actor boundary and constructor;
   - detect database views and entity/view pairs from EF metadata/naming, not a Beacon-specific whitelist;
   - preserve the exact early EFPT ownership marker and compiled `GeneratedCode("EFCorePowerTools", "10.1.1386")` attribute;
   - preserve official template mapping/partial hooks unless a current generic signature requires a mechanical adaptation.
2. Remove `publicEntityViewNames`, `BeaconAr`, `Quote`, `SalesOrder`, `CustomerAddress`, aggregate-name checks, and any other application-specific type-name logic from T4. Put application-specific behavior/relationship corrections in partials or EF configuration, not in the reusable generator template.
3. Add a provenance record beside the templates containing official remote, exact revision, original upstream SHA-256 hashes (`EntityType.t4`: `0EE9962D4D4757EB4DD7C3234597577A511EFD8610341078ED4C3408251BB8BE`; `DbContext.t4`: `70E703CC0E043B0C8441A1B8B9E126FD961E69E352DCE31F27D2E3B2AAC78049`), the bounded compatibility adaptations, and the future resynchronization procedure. The source repository remains read-only.
4. Extend architecture tests to require provenance markers, current generic signatures, ownership metadata, and absence of application-specific names/whitelists in T4. This is the deterministic guard against another bespoke Beacon template.
5. Update `efcpt-config.json` to select `QuotePricingView`, `SalesOrderPricingView`, `AuditLogView`, `IdempotencyRequestView`, and `IdempotencyStateView`. Keep SQL Server, explicit object lists, the pinned EFPT CLI, and the single-context namespace/output paths for this task.
6. Update `build/regenerate-persistence.ps1` expected generated files before invoking EFPT: add the three Operations `*View.cs` files and require the canonical `QuotePricingView.cs`/`SalesOrderPricingView.cs` files. Keep its guarded backup/restore, redaction, exact output-set, and generated-metadata checks.
7. Publish the built DACPAC to a fresh disposable SQL Server database and run `build/regenerate-persistence.ps1`. Accept all entity/view/context/interface changes only as generated output. The expected context mappings are `ToView("QuotePricingView")`, `ToView("SalesOrderPricingView")`, and the three new Operations view mappings; old names must disappear. Run generation twice and compare the complete owned file set byte-for-byte.
8. Review every generated change for key type, nullability, interface pairing, mapper signatures, navigation cardinality, aggregate trackers, audit actor ID, keyless `ToView` configuration, and `GeneratedCodeAttribute`. If an official-derived template creates invalid non-aggregate trackers or relationship shapes, move the correction to the supported partial/configuration seam or document a generic upstream adaptation; do not reintroduce a Beacon-name list.

### 5. Update downstream tests and active documentation

1. Update `LayerBoundaryTests` and `GeneratedPersistenceTests` expected file/type/DbSet/keyless counts and type names from the regenerated output. Add metadata checks for all five changed/new view types, including `long` IDs on `AuditLogView` and `IdempotencyRequestView`.
2. Extend `GeneratedViewLiveTests` with an Operations fixture and query all three views through EF. Prove user/state descriptive expansion, exact hash bytes, nullable audit-user behavior, and one result per base record inside a rolled-back transaction.
3. Add an architecture/OpenAPI check that no public controller action, serializer context, checked OpenAPI component, or generated TypeScript contract exposes `AuditLogView`, `IdempotencyRequestView`, `IdempotencyStateView`, `KeyHash`, `RequestHash`, or `MetadataJson`.
4. Regenerate OpenAPI/client only to prove it is byte-stable. Any new Operations schema is a defect unless a separately approved endpoint creates a concrete public consumer.
5. Update the active Beacon README: all views use the suffix; pricing helper names; all Operations views exist but remain internal; official-template provenance/adaptation; final EFPT selected/generated counts; Task 3 owns four-context partitioning.
6. Replace stale pre-convention helper references throughout Beacon's prior task records with canonical `*View` names or add an explicit superseding note where historical wording must remain. The final static search must have no live old object/type reference.

## Compatibility and dependency review

- `QuoteView` and `SalesOrderView` retain their names and result contracts, so search stored procedures, repositories, Providers, controllers, OpenAPI, and clients do not change.
- The renamed helpers were documented and implemented as internal database dependencies; no repository or endpoint queries them. Still inspect the DACPAC deployment report for unknown external consumers. Do not add old-name aliases.
- Operations tables, write repositories, transaction ownership, seed data, and the `IdempotencyState` numeric enum remain unchanged. The new views are read-only projections.
- No routine result set changes are planned. If EFPT or compilation reveals a stored-procedure mapper dependency, regenerate its single owning mapper and verify result-set order; do not add a parallel mapper.
- Keep current table mappings alongside view mappings in the single context so `AuditLogRepository` and `IdempotencyRepository` continue staging writes. Task 3 must partition them into `OperationsDbContext` without changing the view contract.

## Validation and acceptance

Run from `examples/beacon-ar` unless a repository-level path is shown:

```powershell
dotnet test ../../src/Paradigm.Enterprise.Cli.Tests/Paradigm.Enterprise.Cli.Tests.csproj --configuration Release
dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution BeaconAr.slnx --strict --format json
dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release
dotnet restore BeaconAr.slnx
dotnet build BeaconAr.slnx --configuration Release
dotnet test --solution BeaconAr.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 1
dotnet tool run paradigm doctor --project BeaconAr.slnx
dotnet tool run paradigm packages check --project BeaconAr.slnx
dotnet tool run paradigm validate --project BeaconAr.slnx
dotnet tool run paradigm checks run --project src/BeaconAr.Domain/BeaconAr.Domain.csproj
dotnet tool run paradigm checks run --project src/BeaconAr.Data/BeaconAr.Data.csproj
dotnet tool run paradigm checks run --project src/BeaconAr.Providers/BeaconAr.Providers.csproj
./build/regenerate-persistence.ps1 -TestFailureRecovery -TestRedaction
```

With a freshly published disposable SQL Server database and `ConnectionStrings__DatabaseConnection` set:

```powershell
./build/regenerate-persistence.ps1
./build/regenerate-persistence.ps1
dotnet test --project tests/BeaconAr.Database.IntegrationTests/BeaconAr.Database.IntegrationTests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 1
./scripts/generate-openapi.ps1
dotnet run --project src/BeaconAr.CodeGenerator/BeaconAr.CodeGenerator.csproj --configuration Release -- openapi-typescript ../../artifacts/openapi/beacon-ar-v1.json ../../tests/BeaconAr.ClientContract/generated/beacon-ar-v1.ts
npm ci --prefix tests/BeaconAr.ClientContract
npm run check --prefix tests/BeaconAr.ClientContract
```

Also verify deterministically:

- parsing every database `CREATE VIEW` yields exactly 14 objects and every object/file ends in `View`;
- a static search for the pre-convention helper names returns no stale live reference;
- the five new/renamed views are schema-bound and one-row-per-key on live SQL Server;
- EFPT's second run produces no generated diff;
- the checked OpenAPI and TypeScript client contain none of the Operations view/sensitive field names;
- the DACPAC second publication is idempotent and its deployment report contains no table data loss.

Any unavailable disposable SQL Server, Docker daemon, Node/npm, or authenticated acceptance environment must be recorded as an unverified gap; it must not be reported as passed.

## Task 3 handoff

Task 3 starts from the fully regenerated, compiling schema boundary above. It must split EFPT configuration/output into `AccessDbContext`, `MasterDataDbContext`, `OperationsDbContext`, and `SalesDbContext`; align Data/Domain/Interfaces/Providers folders and namespaces; co-locate partial behavior with generated types; remove repository SQL strings in favor of EF or stored routines; and preserve the 14 canonical view object/type names. It must not revert to `Receivables`, old helper names, or Beacon-specific T4 whitelists.
