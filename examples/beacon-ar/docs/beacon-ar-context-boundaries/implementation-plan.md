# Beacon AR context boundaries — implementation plan

## Goal and scope

Replace the misleading `ReceivablesDbContext` model with exactly four SQL Server persistence contexts whose names, folders, namespaces, generated entities, repositories, routines, and Providers agree with the database capabilities:

- `AccessDbContext`
- `MasterDataDbContext`
- `OperationsDbContext`
- `SalesDbContext`

The implementation must remove the `Receivables` and `Reporting` application namespaces, keep every database table and view in exactly one generated EF model, co-locate handwritten partial entities with their generated halves, and remove handwritten SQL command/query text from production repositories. This task preserves the current public HTTP contract and business behavior. The entity-validation, system-enum, manual-DTO, generic-Provider, and controller-query refactors belong to the later Domain/Provider/API tasks except for namespace changes required to compile.

SQL Server remains the only database engine for this example.

## Governing evidence

- Apply `paradigm-setup-project`, `paradigm-model-domain`, `paradigm-build-repository`, `paradigm-build-provider`, `paradigm-build-database`, `paradigm-review-change`, and `paradigm-evolve-guidance` with their linked coding, solution, database, repository, model, transaction, and SQL Server references.
- Follow the bounded-context layout demonstrated by `C:\Repositories\github\microsoft\rdx-dms-mvp\src\api`: one EFPT configuration and `Data/{Capability}/Context/{Capability}DbContext.cs` per capability; repositories and stored procedures remain under that capability; the host registers every required context against the same named connection.
- Preserve the official-derived generic T4 boundary in `src/BeaconAr.Data/CodeTemplates/EFCore`. `PROVENANCE.md` remains authoritative. Do not add Beacon entity names, relationship names, context names, or application whitelists to either template.
- Preserve the canonical solution classification completed by the solution-layout task. Context folders are physical source ownership; solution folders remain responsibility metadata.

## Target source layout

Business source in Data, Domain, and Providers has only the four capability roots below. Infrastructure such as `CodeTemplates`, project files, and generator plumbing may remain outside those roots.

```text
src/BeaconAr.Data/
  Access/{Context,Repositories,StoredProcedures,Mappers}/
  MasterData/{Context,Repositories,StoredProcedures,Mappers}/
  Operations/{Context,Repositories,StoredProcedures,Mappers}/
  Sales/{Context,Repositories,StoredProcedures,Mappers}/
  CodeTemplates/EFCore/

src/BeaconAr.Domain/
  Access/{Application,Contracts,Entities,Repositories}/
  MasterData/{Application,Contracts,Entities,Repositories,Validation}/
  Operations/{Contracts,Entities,Repositories}/
  Sales/{Application,Contracts,Entities,Repositories,Validation}/

src/BeaconAr.Providers/
  Access/
  MasterData/
  Operations/
  Sales/
```

`BeaconAr.Interfaces` continues to own its Roslyn generator. After the entity namespaces split, its compiler output must be `BeaconAr.Interfaces.Access.Entities`, `.MasterData.Entities`, `.Operations.Entities`, and `.Sales.Entities`. Do not add empty placeholder folders. The later enum task will add handwritten cross-application types to these capability namespaces.

The Web API is currently organized by transport responsibility (`Controllers`, `Http`, `Security`, `Serialization`) rather than by business module. Do not create four partial controller trees in this task. Keep `WebApi/Access` as authentication/request-context infrastructure and change only imports, registrations, serializer metadata, and tests needed by the new Domain/Data namespaces.

## Complete persistence inventory and ownership

Each row below is selected by one and only one EFPT configuration. Database foreign keys and schema-bound views remain in the single SQL Server database even when their principal tables belong to another EF model.

| Context | Tables / generated entities | Views / generated read shapes | Existing routines owned after the move |
| --- | --- | --- | --- |
| Access | `ApplicationUser` | `ApplicationUserView` | none |
| MasterData | `AddressType`, `Carrier`, `Customer`, `CustomerAddress`, `Product` | `CarrierView`, `CustomerView`, `CustomerAddressView`, `ProductView` | `SearchAddress`, `SearchCarrier`, `SearchCustomer`, `SearchProduct` |
| Operations | `AuditLog`, `IdempotencyRequest`, `IdempotencyState` | `AuditLogView`, `IdempotencyRequestView`, `IdempotencyStateView` | move `Reporting/GetDashboardSummary.sql` to `Operations/GetDashboardSummary.sql` |
| Sales | `Quote`, `QuoteLine`, `QuoteStatus`, `QuoteStatusHistory`, `SalesOrder`, `SalesOrderLine`, `SalesOrderStatus`, `SalesOrderStatusHistory` | `QuoteView`, `QuoteLineView`, `QuotePricingView`, `SalesOrderView`, `SalesOrderLineView`, `SalesOrderPricingView` | `SearchQuote`, `SearchSalesOrder`; `QuoteNumberSequence` and `SalesOrderNumberSequence` remain Sales database objects |

This is exact parity with all 17 current tables, all 14 current views, all seven current routines, and both sequences. `AddressType`, status, history, audit, and idempotency decisions remain as already documented; this task does not invent public DTOs or new views.

### Generated output sets

Replace `efcpt-config.json` with four checked-in configs:

| Config | Context output | Domain output | Exact generated entity/view filenames |
| --- | --- | --- | --- |
| `efcpt-access-config.json` | `Access/Context/AccessDbContext.cs` | `../BeaconAr.Domain/Access/Entities` | `ApplicationUser.cs`, `ApplicationUserView.cs` |
| `efcpt-masterdata-config.json` | `MasterData/Context/MasterDataDbContext.cs` | `../BeaconAr.Domain/MasterData/Entities` | `AddressType.cs`, `Carrier.cs`, `CarrierView.cs`, `Customer.cs`, `CustomerAddress.cs`, `CustomerAddressView.cs`, `CustomerView.cs`, `Product.cs`, `ProductView.cs` |
| `efcpt-operations-config.json` | `Operations/Context/OperationsDbContext.cs` | `../BeaconAr.Domain/Operations/Entities` | `AuditLog.cs`, `AuditLogView.cs`, `IdempotencyRequest.cs`, `IdempotencyRequestView.cs`, `IdempotencyState.cs`, `IdempotencyStateView.cs` |
| `efcpt-sales-config.json` | `Sales/Context/SalesDbContext.cs` | `../BeaconAr.Domain/Sales/Entities` | `Quote.cs`, `QuoteLine.cs`, `QuoteLineView.cs`, `QuotePricingView.cs`, `QuoteStatus.cs`, `QuoteStatusHistory.cs`, `QuoteView.cs`, `SalesOrder.cs`, `SalesOrderLine.cs`, `SalesOrderLineView.cs`, `SalesOrderPricingView.cs`, `SalesOrderStatus.cs`, `SalesOrderStatusHistory.cs`, `SalesOrderView.cs` |

Every config uses the pinned EF Core Power Tools CLI, T4 enabled at the existing template root, nullable reference types, database names, fluent configuration, explicit object selection, `refresh-object-lists: false`, and no connection string. No selected table/view may occur in more than one config. Stored procedures and functions remain excluded from EFPT because Beacon's typed routine boundary and mapper generator own them.

The existing generic T4 logic can pair an entity and `{Entity}View` because each pair remains in one config. It must infer `DbContextBase<int>` independently for all four contexts. Add template fixtures proving the Access, MasterData, Operations (mixed `long` entity and `int` actor IDs), and Sales metadata shapes without embedding those names in the template.

## Cross-context relationship ownership

Keep every SQL Server FK unchanged. EF models include navigations only when both ends belong to the same context:

- Access keeps only the `ApplicationUser` self-audit relationships.
- MasterData keeps `CustomerAddress -> Customer` and `CustomerAddress -> AddressType`. Move `ReceivablesDbContext.Relationships.cs` to `MasterData/Context/MasterDataDbContext.Relationships.cs`, change its namespace/type, and retain the reviewed one-to-many correction.
- Operations keeps `IdempotencyRequest -> IdempotencyState`.
- Sales keeps Quote/QuoteLine/status/history and SalesOrder/line/status/history/SourceQuote relationships.
- Audit links from MasterData, Operations, and Sales to `ApplicationUser`, and Sales links to Customer, CustomerAddress, Product, and Carrier remain scalar FK properties enforced by SQL Server. Do not duplicate `ApplicationUser` or MasterData entities in those contexts merely to obtain navigations.

Expected cross-context navigations to disappear include the large `ApplicationUser` inverse collection catalog, MasterData-to-Sales collections, Sales-to-MasterData principals, and Operations-to-Access principals. Behavior partials use scalar IDs and same-context aggregate children, so no domain rule requires those persistence navigations.

Sales resolves current Customer/Address/Product/Carrier data through the existing `ISalesReferenceRepository` anti-corruption contract and named stored routines described below. Sales views may continue joining Access/MasterData tables in SQL because those schema-bound projections are Sales-owned read contracts, not EF entity ownership.

## Production repository SQL inventory and replacement

No production repository may contain `FromSql*`, `ExecuteSql*`, `SqlQuery*`, `DbCommand.CommandText`, a SQL verb string, or a provider command constructed from handwritten query text. Tests and the finite database bootstrap remain separate, explicit SQL boundaries and are not repository exceptions.

| Current method | Current SQL use | Replacement |
| --- | --- | --- |
| `Access/ApplicationUserRepository.GetOrCreateAsync` | interpolated transaction, range lock, and insert | add `dbo.GetOrCreateApplicationUser` under `routines/Access`; typed parameters return the user ID; execute through `ResultStoredProcedureBase`, then load the tracked `ApplicationUser` with ordinary EF |
| `Operations/IdempotencyRepository.FindForUpdateAsync` | `FromSqlInterpolated` with `UPDLOCK, HOLDLOCK` | add `dbo.LockIdempotencyRequest` under `routines/Operations`; return the matching `long` ID or zero while retaining the serializable range lock in the caller transaction, then load by ID with EF |
| `Sales/QuoteRepository.AllocateNumberAsync` | raw `NEXT VALUE FOR QuoteNumberSequence` command | add `dbo.AllocateQuoteNumber` under `routines/Sales`; return `BIGINT`, then keep `SalesNumberFormatter.Quote` in C# |
| `Sales/SalesOrderRepository.AllocateNumberAsync` | raw `NEXT VALUE FOR SalesOrderNumberSequence` command | add `dbo.AllocateSalesOrderNumber`; return `BIGINT`, then keep `SalesNumberFormatter.Order` in C# |
| `Sales/QuoteRepository.LockForConversionAsync` | `FromSqlInterpolated` lock query plus `Include` | add `dbo.LockQuoteForConversion`; return ID/zero under `UPDLOCK, HOLDLOCK`, then use tracked EF `Include(QuoteLines)` inside the same active transaction |
| `Sales/SalesReferenceRepository.GetCustomerAsync` | locked raw Customer query | add `dbo.GetCustomerSalesReferenceForUpdate`; return a typed Sales-owned row and map it to `CustomerSalesReference` |
| `Sales/SalesReferenceRepository.GetAddressAsync` | locked raw CustomerAddress query | add `dbo.GetAddressSalesReferenceForUpdate`; return the AddressType code in the same bounded result |
| `Sales/SalesReferenceRepository.GetProductsAsync` | one locked raw Product query per ordered ID | add `dbo.GetProductSalesReferenceForUpdate`; retain deterministic distinct-ID iteration and execute one typed routine call per ID |
| `Sales/SalesReferenceRepository.GetCarrierAsync` | locked raw Carrier query | add `dbo.GetCarrierSalesReferenceForUpdate` |
| `MasterData/AddressRepository.LockCustomersAsync` | raw locked Customer scalar query per ID | add `dbo.LockCustomerForAddressMutation`; retain sorted distinct-ID calls inside the Provider transaction |
| `MasterData/AddressRepository.ReparentAsync` | raw optimistic `UPDATE ... OUTPUT RowVersion` | remove the custom repository method; after the context split, `CustomerId` is no longer an EF alternate key caused by the Sales composite FK, so call entity `Replace` and the normal tracked framework update/commit path; verify row-version refresh and conflict translation |

The context split also breaks cross-context LINQ in `AddressRepository.HasReferencesAsync`, `CarrierRepository.HasReferencesAsync`, `CustomerRepository.HasReferencesAsync`, and `ProductRepository.HasReferencesAsync`. Preserve their contracts and add four explicit MasterData routines: `HasAddressReferences`, `HasCarrierReferences`, `HasCustomerReferences`, and `HasProductReferences`. Each returns one `BIT`, includes every current same- and cross-context reference check, and avoids a generic entity-name switch or dynamic SQL.

All 14 new routines have one SQL object per file, deterministic result sets, explicit `dbo` names, a 30-second reviewed timeout, cancellation at the repository boundary where the installed routine API permits it, and SQL integration tests. Add them to `BeaconAr.Database.sqlproj` under their owning capability and to database validation/hash evidence.

Create one typed parameter class and, where non-primitive data is returned, one internal mutable result-row class per routine under `Data/{Capability}/StoredProcedures`; wrappers inherit the installed SQL Server `StoredProcedureBase`/`ResultStoredProcedureBase` signature. Do not handwrite parameter/data-reader mappers. Extend the existing mapper generator to group output into `Data/{Capability}/Mappers/{SqlParameters,DataReaders}` and generate `AccessStoredProcedureMappersRegisterer`, `MasterDataStoredProcedureMappersRegisterer`, `OperationsStoredProcedureMappersRegisterer`, and `SalesStoredProcedureMappersRegisterer`. The capability comes from the stored-procedure type namespace, not a type-name list. Generate into guarded staging directories, replace all four owned mapper bundles atomically with rollback, run twice, and compare the complete path/content set byte-for-byte. Remove the old top-level `Data/Mappers` output and misspelled `StoreProcedureMappersRegisterer` only after the four replacements compile.

Task 6 owns the reusable CLI semantic diagnostic. Hand it the exact rule: production classes assignable to `IRepository` must not contain EF raw-SQL invocations, provider command-text assignments, or SQL query/command literals; positive fixtures must allow typed stored-procedure wrappers, test projects, SQL database source, and finite bootstrap tooling. This task still adds an example architecture regression test so Beacon cannot regress before the CLI rule lands.

## Source move inventory

Use `git mv`-equivalent moves; do not copy types and leave compatibility duplicates.

### Data

- Move each existing repository into `Data/{owner}/Repositories` and update namespaces. The complete set is: Access `ApplicationUserRepository`; MasterData `AddressRepository`, `AddressViewRepository`, `CarrierRepository`, `CarrierViewRepository`, `CustomerRepository`, `CustomerViewRepository`, `ProductRepository`, `ProductViewRepository`; Operations `AuditLogRepository`, `IdempotencyRepository`, and former Reporting `DashboardRepository`; Sales `QuoteRepository`, `QuoteViewRepository`, `SalesOrderRepository`, `SalesOrderViewRepository`, `SalesReferenceRepository`.
- Keep `MasterDataPersistenceErrorClassifier`, `PageResultFactory`, and the four MasterData search procedure contracts under MasterData; keep `IdempotencyPersistenceErrorClassifier` and `PersistenceSession` under Operations; keep `SalesPersistenceErrorClassifier` and `SalesNumberFormatter` under Sales. Move namespaces with paths.
- Move `Reporting/StoredProcedures/{DashboardParameters,DashboardRow,GetDashboardSummaryProcedure}` to `Operations/StoredProcedures`.
- Re-home the complete current generated mapper inventory by capability: MasterData data readers `CarrierView`, `CustomerAddressView`, `CustomerView`, `ProductView` and parameter mappers `AddressSearchParameters`, `CarrierSearchParameters`, `CustomerSearchParameters`, `ProductSearchParameters`; Operations data reader `DashboardRow` and `DashboardParameters` mapper; Sales data readers `QuoteSearchRow`, `SalesOrderSearchRow` and parameter mappers `QuoteSearchParameters`, `SalesOrderSearchParameters`. Replace the one top-level registrar with the four capability registrars. New routine result/parameter mappers join these generated sets.
- Replace `Receivables/Context/ReceivablesDbContext.cs` with the four generated contexts and move the relationship partial as described above. Delete the empty `Receivables` and `Reporting` trees after all consumers compile.
- Update `SalesOrderViewRepository`'s detail header query to use the Sales-owned `SalesOrderView` projection for `CarrierName` instead of joining the now-unmapped `Carrier` entity. Keep the existing DTO/route shape for this task. `QuoteViewRepository` can retain its Sales-only Quote/Pricing/SalesOrder query, although using the equivalent Sales view is acceptable if tests prove byte-identical transport behavior.

### Domain

- Move all 31 generated files from `Domain/Receivables/Entities` to their exact capability `Entities` output sets above and regenerate them with their new namespaces; never edit them manually.
- Co-locate every handwritten entity partial with its generated half: Access `ApplicationUser.Behavior.cs`; MasterData `Carrier.Behavior.cs`, `Customer.Behavior.cs`, `CustomerAddress.Behavior.cs`, `Product.Behavior.cs`; Operations `IdempotencyRequest.Behavior.cs`; Sales `Quote.Behavior.cs`, `QuoteLine.Behavior.cs`, `QuoteStatusHistory.Behavior.cs`, `SalesOrder.Behavior.cs`, `SalesOrderLine.Behavior.cs`, `SalesOrderStatusHistory.Behavior.cs`.
- Move `Domain/Reporting/Contracts/DashboardSummaryDto.cs` to `Domain/Operations/Contracts` and `Domain/Reporting/Repositories/IDashboardRepository.cs` to `Domain/Operations/Repositories`; change namespaces and all imports.
- Keep all other Access/MasterData/Operations/Sales application, contract, repository, validation, and behavior types in their current capability. Do not refactor `SalesDomainValidation`, status enums, request DTOs, Provider validation, or CRUD overrides in this task.
- Update `EntityInterfaceGenerator` tests and emitted namespaces for all generated entity pairs. Generated interface ownership stays in the analyzer and generated entity/view mapping stays in T4.

### Providers and host

- Move `Providers/Reporting/{DashboardProvider,IDashboardProvider}` to `Providers/Operations`; update `DashboardController` imports only.
- Keep every other Provider in its current Access/MasterData/Operations/Sales capability and update entity/interface namespaces. Do not change validation/CRUD behavior beyond what the context/repository split requires.
- In `WebApi/Program.cs`, register the four contexts with the same exact `DatabaseConnection` name and the one scoped `SqlServerDbContextConnectionProvider`, then register one scoped `IUnitOfWork`. Replace the Domain assembly root type with a stable generated type in a new namespace. Repository/provider discovery remains explicit by assembly.
- Change `PersistenceSession` to clear all four context change trackers after rollback. It must not commit, create transactions, or become a general data context.

## Unit of Work and transaction acceptance

`SqlServerDbContextConnectionProvider` caches one `SqlConnection` per scoped connection-string name. Registering all four contexts with the identical `DatabaseConnection` name therefore gives `DbContextTransaction` a compatible connection for cross-context enlistment. Do not create four connection names or four independent providers.

`UnitOfWork.CommitChangesAsync` saves registered contexts sequentially and is not atomic by itself. Every workflow that stages more than one context must resolve all known repositories before `CreateTransaction`, create the explicit shared transaction, save, and commit it. A repository resolved after transaction creation is automatically offered for enlistment, but tests must cover that path rather than relying on it accidentally.

Required transaction cases:

- MasterData mutations stage MasterData plus Operations audit rows.
- Sales mutations stage Sales plus Operations audit rows; Sales reference routines use the same active transaction/connection.
- idempotent creation stages Operations, then later MasterData or Sales work in the same transaction.
- Access user provisioning is Access-only; its routine owns its short concurrency-safe get-or-create operation.
- Dashboard is an Operations-owned read routine and does not enlist write contexts.

Add integration tests that force the second context save to fail and prove the first save rolls back; prove all four contexts share the exact scoped connection instance; prove `PersistenceSession` clears every tracker; prove locks/range locks prevent duplicate user/idempotency creation and quote conversion races. Do not claim atomicity for a different database or connection string.

## Deterministic regeneration changes

Rewrite `build/regenerate-persistence.ps1` around a four-boundary manifest:

1. Validate the four configs, the pinned EFPT tool/version, official-derived T4 files/provenance, no credentials, disjoint object selection, exact parity with all database tables/views, expected namespaces/paths, and existing generated ownership markers.
2. Because partials now live beside generated files, treat only the exact generated filename manifests above as replaceable. Never clear a whole `Entities` or `Context` directory. Preserve `*.Behavior.cs` and `MasterDataDbContext.Relationships.cs` byte-for-byte.
3. Back up all 35 generated outputs (31 Domain types plus four contexts) to one guarded temporary root before changing any. On failure in any of the four EFPT runs, restore all four boundaries, not only the failing context.
4. Invoke the four configs in Access, MasterData, Operations, Sales order against one disposable published SQL Server database; guard/remove only the known EFPT readme byproduct.
5. Validate exact generated filename sets, first-line ownership markers, `GeneratedCode` metadata, context bases/constructors, keyless `ToView` mappings, no duplicated CLR/database object ownership, and absence of unexpected auto-generated files.
6. Build, run the focused tests, regenerate a second time, and compare the complete generated path/hash manifest. A second-run difference fails the workflow.
7. Update `README.md`, `docs/tutorials/database-first.md`, and prior superseding ownership notes so no instruction still names `ReceivablesDbContext`, one config, or the old generated boundary as current.

## Test inventory and required updates

Every existing test file remains in scope; the following are the concrete changes.

- `BeaconAr.Architecture.Tests/GeneratedPersistenceTests.cs`: assert four configs/contexts, exact disjoint 31-file ownership, mixed generated/partial directory safety, four interface namespaces, expected same-context navigations, forbidden cross-context navigations, mapper generation determinism, and regeneration failure recovery across all boundaries.
- `BeaconAr.Architecture.Tests/LayerBoundaryTests.cs`: replace Receivables/Reporting namespace assumptions; assert only Access/MasterData/Operations/Sales business roots in Data/Domain/Providers; assert no repository contains the forbidden raw-SQL APIs/literals; retain official T4/provenance and generated metadata checks.
- `BeaconAr.Database.IntegrationTests/FoundationSchemaTests.cs`: retain schema/view assertions and add exact routine existence/signature/result-order checks for the 14 new routines and moved dashboard routine.
- `BeaconAr.Database.IntegrationTests/GeneratedViewLiveTests.cs`: instantiate/query each of the four contexts and all 14 views through their owning context; verify keyless mappings and joined values.
- `BeaconAr.Database.IntegrationTests/MasterDataLiveTests.cs`: register MasterData and Operations contexts; cover EF reparenting with row-version refresh, default-address locks, all four reference routines, cross-context audit atomicity, and failure rollback.
- `BeaconAr.Database.IntegrationTests/SalesWorkflowLiveTests.cs`: register Sales and Operations contexts; cover allocation/lock/reference routines, scalar-only Sales foreign keys, conversion/idempotency concurrency, shared-connection enlistment, audit rollback, and all Sales views.
- `BeaconAr.Domain.Tests/{FoundationValuesTests,GeneratedMapperTests,MasterDataValidationTests,SalesWorkflowTests}.cs`: update generated entity/interface namespaces only; keep behavior assertions unchanged.
- `BeaconAr.Providers.Tests/{OtherMasterDataProviderTests,ProductProviderTests,SalesProviderTests}.cs`: update namespaces and mocks; add commit/rollback/clear assertions affected by multiple contexts without beginning the later Provider refactor.
- `BeaconAr.WebApi.Tests/BeaconArSecurityTestHost.cs` and acceptance/contract/hardening/security/transport/discovery/generated-mapping/OpenAPI tests: update service replacements and namespaces; verify the real host resolves all four contexts and generated mappers while routes and serialized contracts remain unchanged.
- `BeaconAr.DatabaseBootstrap.Tests/BootstrapInfrastructureTests.cs` and the TypeScript client contract remain behaviorally unchanged but must pass in the full suite.

## Implementation sequence

1. Record a clean Release restore/build/test/CLI/database-validation baseline and a generated hash snapshot.
2. Add and build the 14 SQL Server routines, move the dashboard routine, update the SQL project, and test the routine contracts against a disposable database.
3. Add the four EFPT configs and rewrite the regeneration safeguards; regenerate the four disjoint models twice and review the complete diff.
4. Move/co-locate Domain partials and all other Domain types; update generated interfaces and tests.
5. Move Data repositories/helpers/routines/mappers; replace every raw-SQL method according to the table above; regenerate mapper bundles twice.
6. Move Reporting Provider types into Operations and update Provider/host consumers.
7. Register all four contexts on the same scoped named SQL connection, implement multi-context tracker clearing, and run transaction/concurrency integration tests.
8. Update documentation and only then promote demonstrated durable guidance. Hand the semantic CLI rule and fixtures to Task 6.
9. Run the complete validation matrix and inspect `git diff --check`, generated hashes, database source, project membership, namespace search, and forbidden SQL search before review.

## Validation commands

Run from `examples/beacon-ar` unless noted:

```powershell
dotnet restore BeaconAr.slnx --locked-mode
dotnet build BeaconAr.slnx --configuration Release --no-restore
dotnet test BeaconAr.slnx --configuration Release --no-build
dotnet tool run paradigm doctor --project BeaconAr.slnx
dotnet tool run paradigm packages check --project BeaconAr.slnx
dotnet tool run paradigm packages audit --project BeaconAr.slnx
dotnet tool run paradigm validate --project BeaconAr.slnx
dotnet tool run paradigm checks run --project BeaconAr.slnx
dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution BeaconAr.slnx --strict
dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release
./start.sh doctor
dotnet tool run aspire restore
```

Then publish the DACPAC to a disposable SQL Server through the owned bootstrap, run all live integration tests, run persistence generation twice and compare hashes, run mapper generation twice and compare paths/hashes, regenerate OpenAPI/client artifacts only if namespace metadata changes their bytes, and run `git diff --check` plus a repository-only forbidden-SQL scan.

## Guidance promotion after proof

- Amend `paradigm-build-repository` to state the evidenced hard rule: no handwritten SQL query/command strings in repositories; use EF for simple bounded CRUD/query work and typed stored procedures for locking, complex multi-entity, pagination/reporting, or performance-sensitive work.
- Amend setup/review guidance and the centralized solution/context reference with the demonstrated multi-config EFPT ownership rule: one selected database object and generated CLR type belongs to exactly one context/config; cross-context FKs remain database constraints/scalars unless an explicit read contract is introduced; mixed partial/generated directories require exact-file cleanup.
- Add deterministic positive/negative/exception/order fixtures to Task 6's CLI check rather than implementing a source-text preference only in prose. Exempt test SQL, database projects, bootstrap checks, and typed routine-name constants; do not exempt arbitrary Data classes.
- Update Beacon README/tutorial/runbooks to show the four-context generation and shared-transaction constraints. Mark the earlier Receivables ownership documents as historical/superseded instead of silently contradicting them.

## Explicit non-goals

- No PostgreSQL path, shared/general DbContext, duplicated generated entity, database split, or distributed transaction.
- No broad Domain validation rewrite, enum relocation, DTO removal, Provider CRUD redesign, controller validation/query DTO redesign, or public API route/schema change.
- No handwritten edits to EFPT or mapper output, no new runtime NuGet dependency, no connection string in source, and no database publication outside a disposable environment.
