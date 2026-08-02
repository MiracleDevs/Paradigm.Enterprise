# Beacon AR framework CRUD review feedback

## Review result

Changes requested. The solution builds and the existing tests pass, but the newly advertised official provider surface is not behaviorally equivalent to the legacy master-data surface and is not ready for the official controllers planned in Task 4.

## Findings

### P1 - The official edit-provider methods bypass the required master-data lifecycle

Evidence:

- `src/BeaconAr.Providers/MasterData/IProductProvider.cs:7`, `ICustomerProvider.cs:7`, `IAddressProvider.cs:7`, and `ICarrierProvider.cs:7` expose every `IEditProvider<TView, int>` add, update, save, and delete overload.
- `ProductProvider.cs:12-13`, `CustomerProvider.cs:12-13`, `AddressProvider.cs:12-13`, and `CarrierProvider.cs:12-13` inherit `EditProviderBase` but do not override those official operations or any of its lifecycle hooks.
- The safe behavior exists only in the parallel legacy overloads, for example `ProductProvider.cs:44-113`. Those overloads perform normalization, duplicate/reference checks, ETag comparison, audit creation, explicit transaction handling, conflict translation, rollback cleanup, and generated-ID audit saves.
- The inherited base path maps a `*View` directly, calls `Validate()`, stages, and commits once (`src/Paradigm.Enterprise.Providers/EditProviderBase.cs:158-171`, `235-250`, and `398-406`). The generated entities delegate validation to unimplemented partial methods (`Product.cs:49-72`, `Customer.cs:51-74`, `CustomerAddress.cs:61-84`, and `Carrier.cs:43-66`), so this path does not apply the request validators or domain rules used by the legacy operations.

Consequence:

Any application service resolving `IProductProvider` (or the other three contracts) can call `AddAsync(ProductView)`, `UpdateAsync(ProductView)`, `SaveAsync`, or `DeleteAsync(int)` and bypass audit ownership, duplicate/reference checks, the explicit audit transaction, persistence-error translation, and the current optimistic-concurrency protocol. Create can persist default audit values and no audit-log row; update compares no client version; delete accepts only an identifier and bypasses the referenced-delete policy. The bulk overloads have the same problem. An official CRUD controller in Task 4 would call this unsafe surface.

Required fix:

Implement one safe canonical lifecycle for the official view-based contract and make the temporary legacy adapters delegate to it, or explicitly override every unsafe official mutation until that lifecycle exists. Preserve the documented two-save outer transaction for generated-ID audits, current audit/user stamping, duplicate/reference rules, conflict translation, tracked-state cleanup, and a deliberate concurrency policy for view updates/deletes. Add behavioral tests through the `IEditProvider<TView, int>` interface for single and bulk add/update/save/delete, including audit count, commit/rollback behavior, stale versions, referenced deletes, and validation failures. Reflection-only inheritance tests are insufficient.

### P1 - The official read-provider search contract always reaches an unimplemented base hook

Evidence:

- The same four provider interfaces inherit `IReadProvider<TView, int>.SearchAsync<TParameters>` through `IEditProvider`.
- Each generated-view repository defines only a different overload accepting the legacy request plus a cancellation token (`ProductViewRepository.cs:37-56`, `CustomerViewRepository.cs:37-56`, `AddressViewRepository.cs:38-64`, and `CarrierViewRepository.cs:37-56`).
- None overrides `GetSearchPaginatedFunction`; the inherited implementation throws `NotImplementedException` (`src/Paradigm.Enterprise.Data/Repositories/ReadRepositoryBase.cs:156`).
- The new test fakes acknowledge the gap by throwing `NotSupportedException` for the official search members (`tests/BeaconAr.Providers.Tests/ProductProviderTests.cs:72-75` and `109-112`) instead of exercising them.

Consequence:

The typed provider promises a standard search operation that fails at runtime. The official read/edit controllers planned for Task 4 will produce a server error when they use the framework search contract.

Required fix:

Provide an application `PaginationParametersBase` search parameter and override the official repository search hook to execute the existing stored-procedure query, or override the official provider search and adapt it to the canonical generated-view query without leaving the repository contract broken. Test the method through `IReadProvider<TView, int>`/`IEditProvider<TView, int>`, including paging, filters, stable ordering, and not merely the legacy overload.

### P2 - Two-round-trip search hydration can fail or return a page from two database states

Evidence:

- All four view repositories first execute the stored procedure, retain only its IDs, then issue a separate EF query and directly index a dictionary (`ProductViewRepository.cs:42-55`, `CustomerViewRepository.cs:42-55`, `AddressViewRepository.cs:43-63`, and `CarrierViewRepository.cs:42-55`).

Consequence:

If a row is deleted or becomes invisible between the procedure and EF query, `byId[id]` throws `KeyNotFoundException`. If a row changes, filtering/order/count can describe the first database state while returned display fields come from a later state. This regresses the previous single stored-procedure materialization and can surface as an unclassified 500 under ordinary concurrent master-data edits.

Required fix:

Materialize the generated `*View` shape inside the stored-procedure boundary, preferably by paging/filtering against or joining the `{Entity}View` and returning columns that map directly to the generated view. Avoid ID hydration in a second round trip. Add a live integration test that proves ordered generated-view results and defines behavior under concurrent disappearance; at minimum, never directly index a dictionary for an ID that may no longer exist.

### P2 - The custom cancellation token is no longer propagated to identifier reads

Evidence:

- The four legacy provider contracts still accept a `CancellationToken` for `GetByIdAsync` (`IProductProvider.cs:11`, `ICustomerProvider.cs:11`, `IAddressProvider.cs:11`, and `ICarrierProvider.cs:11`).
- Their implementations call the framework `ViewRepository.GetByIdAsync(id)`, which has no cancellation parameter (`ProductProvider.cs:41-42`, `CustomerProvider.cs:39-42`, `AddressProvider.cs:47-50`, and `CarrierProvider.cs:39-42`). Product does not even perform the pre-query cancellation check; the other three only check before starting the uncancellable database operation.

Consequence:

Client disconnect/request cancellation cannot stop the database query or the post-mutation reload. This violates the custom-contract cancellation guidance and can consume work after the caller has gone away.

Required fix:

Retain a cancellation-aware generated-view identifier method on each application view repository and call `SingleOrDefaultAsync(..., cancellationToken)`/`FirstOrDefaultAsync(..., cancellationToken)` from it. Use that method from the legacy adapters and test already-cancelled and in-flight cancellation behavior. Do not claim cancellation reaches the framework generic methods, whose current contracts have no token.

### P2 - The SQL project acquired a conflicting project identity and undocumented scope changes

Evidence:

- `src/database/BeaconAr.Database.sqlproj:9` adds `ProjectGuid` `{A09E027B-4428-49A6-9A92-C43CF35D9206}`, while `src/BeaconAr.sln:28`, its build mappings at `:162-173`, and its solution-folder mapping at `:254` identify the same project as `{36012A89-DEB7-49AF-AA8F-2C0B1C3289BF}`.
- `TargetDatabaseSet` and all nested `Folder` items were also added in this provider/repository task (`BeaconAr.Database.sqlproj:8` and `:44-63`) but are absent from the change summary and decisions. The file also lost its final newline.

Consequence:

The normal CLI build and database validator pass, but the newly conflicting identity is in the exact Visual Studio project-loading area the user asked to repair and can produce inconsistent IDE behavior. Unexplained validator/IDE side effects also make the task history misleading.

Required fix:

Keep and document the explicit nested `Folder` items if Visual Studio verification shows they are required; they are IDE metadata and do not duplicate the existing `Build` globs. Remove the unnecessary `ProjectGuid` property or align it with the solution project identity after verifying Microsoft.Build.Sql/Visual Studio behavior. Retain `TargetDatabaseSet` only if it is demonstrably required, explain it in the Task 1/Task 3 decisions, verify the solution in Visual Studio, and restore the final newline.

### P3 - The sales coordinator is composed manually in three providers

Evidence:

- `QuoteProvider.cs:29-44`, `SalesOrderProvider.cs:29-44`, and `QuoteConversionProvider.cs:26-41` repeat the same six workflow-mechanics dependencies and each calls `new SalesWorkflowCoordinator(...)`.
- The coordinator exposes `ErrorClassifier`, `PersistenceSession`, and `UnitOfWork` publicly (`SalesWorkflowCoordinator.cs:22-26`) even though only code in the Providers assembly consumes them.

Consequence:

The inheritance base is gone, but its constructor coupling is duplicated across every workflow provider and the helper presents a broader public surface than required. Adding one shared workflow dependency still requires editing three providers.

Required fix:

Register and inject one scoped `SalesWorkflowCoordinator`, as already done for `MasterDataMutationCoordinator`, or introduce an equally narrow Providers-owned registration/factory. Narrow members that do not need cross-assembly access. Keep it sealed and do not make it an `IProvider` or a replacement generic provider base.

## Verification performed

- `dotnet build src/BeaconAr.sln --configuration Release --no-restore`: passed, zero warnings/errors.
- `dotnet test tests/BeaconAr.Providers.Tests/BeaconAr.Providers.Tests.csproj --configuration Release --no-build --no-restore`: 16 passed.
- `dotnet test tests/BeaconAr.Architecture.Tests/BeaconAr.Architecture.Tests.csproj --configuration Release --no-build --no-restore`: 19 passed.
- `dotnet tool run paradigm checks run --project src/BeaconAr.Providers/BeaconAr.Providers.csproj`: passed.
- `dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution src/BeaconAr.sln --strict`: passed.
- `git diff --check`: no whitespace errors; line-ending normalization warnings only.

## Residual environment gap

Database-backed integration tests were not run because this review did not have a configured disposable SQL Server connection. Visual Studio project-tree behavior was inspected through project/solution metadata but could not be interactively verified in Visual Studio.

## Second review - 2026-08-02

### Result

Changes requested. Both previous P1 blockers are resolved, and no new blocker was found. One generated-boundary/duplication finding and the remaining least-visibility portion of the previous P3 finding should be corrected before approval.

### Prior finding dispositions

1. **P1 official mutation lifecycle bypass - resolved by fail-closed overrides.** Each of Product, Customer, Address, and Carrier now declares all eight virtual mutation members from `IEditProvider<TView, int>`: single/list add, single/list update, single/enumerable save, and single/enumerable delete (`ProductProvider.cs:107-121`, `CustomerProvider.cs:112-126`, `AddressProvider.cs:137-151`, and `CarrierProvider.cs:112-126`). Every overload throws before repository, Unit of Work, transaction, or audit access. `OfficialMutationSurfaceRejectsEveryUnsafeOverloadWithoutSaving` exercises all eight through `IEditProvider<ProductView, int>` and proves zero saves/audits/deletes; `OfficialMasterDataMutationsAreExplicitlyGuardedUntilTaskFour` proves all eight are declared by every provider. This is an acceptable temporary boundary because the decision document explicitly assigns the concurrency/audit transport design to Task 4.
2. **P1 official generic search failure - resolved for the current official contract.** `MasterDataViewSearchParameters` derives from `PaginationParametersBase`; every provider validates it before delegating, and every generated-view repository overrides `GetSearchPaginatedFunction`. The modern `IReadProvider.SearchAsync<TParameters>` path therefore executes the typed stored procedure and returns generated views. Provider tests exercise interface dispatch and validation, while the live integration test covers all four official searches when SQL Server is configured. The obsolete `SearchPaginatedAsync(FilterTextPaginatedParameters)` member remains unsupported, but the current `ReadApiControllerBase` uses the generic search member and Task 3 does not claim obsolete-controller compatibility.
3. **P2 two-round-trip search hydration - resolved.** The four procedures now select the complete `{Entity}View` shape, their procedure wrappers return `List<ProductView>`, `List<CustomerView>`, `List<CustomerAddressView>`, or `List<CarrierView>`, and repositories return those rows directly. The ID/dictionary EF hydration query is gone. SQL selections and mapper properties align with the generated view properties, including nullable audit/display columns, address reference expansion, `short` payment terms, and `byte[]` row versions.
4. **P2 identifier-read cancellation - resolved.** Each application view-repository contract/implementation has a cancellation-aware `GetByIdAsync(int, CancellationToken)` backed by EF's cancellation-aware materializer, and all four legacy adapters call it. The generic framework read contract still has no cancellation token and is accurately documented as such.
5. **P2 SQL project identity/scope - resolved, then superseded by final Visual Studio evidence.** This task originally removed `ProjectGuid` and `TargetDatabaseSet`. Final validation observed Visual Studio 18 immediately restore both while the project was loaded, so the safe resolution became aligning the solution entry, all configuration mappings, and the solution-folder mapping to the restored project GUID. The nested `Folder` metadata remains non-duplicative, the file ends with a newline, and Release/strict database validation pass. See `docs/beacon-ar-final-validation/decisions.md`.
6. **P3 sales coordinator composition - partially resolved.** `SalesWorkflowCoordinator` is now registered once as scoped and constructor-injected into all three workflow providers; no provider constructs it manually. Its workflow-only members remain unnecessarily public, as described below.

### New P2 - Stored-procedure mapper generation was bypassed and obsolete generated search-row output remains

Evidence:

- The four procedure wrappers now name generated view types as their result arguments (`SearchProductProcedure.cs`, `SearchCustomerProcedure.cs`, `SearchAddressProcedure.cs`, and `SearchCarrierProcedure.cs`).
- The established `StoredProcedureMapperGenerator` discovers those generic result arguments and atomically regenerates both data-reader mappers and `StoreProcedureMappersRegisterer` (`src/BeaconAr.CodeGenerator/Generators/StoredProcedureMapperGenerator.cs:53-59`, `82-149`, and `378-420`).
- Instead of regenerating that owned output, this change adds a parallel handwritten mapper system under `src/BeaconAr.Data/MasterData/Mappers` and calls it from every repository static constructor.
- The old `AddressSearchRow`, `CarrierSearchRow`, `CustomerSearchRow`, and `ProductSearchRow` classes are now unused, but their generated mappers and registrations remain (`src/BeaconAr.Data/Mappers/StoreProcedureMappersRegisterer.cs:20-23`). A future normal `mappers` regeneration would generate the new `*View` mappers and remove those registrations, leaving the handwritten parallel mappers redundant.

Consequence:

The runtime mapping is currently aligned, but the repository now has two mechanisms owning the same kind of mapping and carries four dead result types plus four dead generated mappers. This contradicts the established generated boundary, makes the checked-in generated output stale, and guarantees avoidable churn/duplication on the next normal code-generation run.

Required fix:

Remove the handwritten `MasterData/Mappers` parallel registry/mappers and the four obsolete master-data `*SearchRow` types, then run the existing code generator's `mappers` target so it emits the generated `*ViewDataReaderMapper` files and updates `StoreProcedureMappersRegisterer` atomically. Build and run the generator a second time to prove a clean deterministic result. Update the change summary/decisions to state that stored-procedure result mappers are generator-owned rather than handwritten. Do not hand-edit the generated mapper directory.

### Remaining P3 - Sales workflow mechanics still expose an unnecessary public API

Evidence:

- `SalesWorkflowCoordinator` must be public because WebApi registers it and public providers accept it in their constructors, but its `ErrorClassifier`, `PersistenceSession`, and `UnitOfWork` properties (`SalesWorkflowCoordinator.cs:22-26`) and its operation/helper methods (`:60-123`) are consumed only inside `BeaconAr.Providers`.

Consequence:

The constructor-duplication issue is fixed, but the collaborator still publishes Unit of Work and persistence-classifier mechanics as an application-facing API despite having no cross-assembly caller. This violates the least-visibility convention and makes accidental coupling easier.

Required fix:

Keep the class and constructor public for DI, but make same-assembly properties and methods `internal` (or narrower where possible). Add an architecture assertion for the intended visibility so later workflow changes do not broaden the surface again.

### Second-review verification

- Release solution build: passed with zero warnings/errors.
- Complete solution tests: 153 total, 112 passed, 41 skipped, zero failed. SQL-backed and authenticated acceptance tests were skipped because `ConnectionStrings__DatabaseConnection` is not set.
- Provider tests: 19 passed.
- Architecture tests: 23 passed.
- Paradigm semantic checks for Data and Providers: passed.
- Strict database project validation: passed.
- `git diff --check`: no whitespace errors; line-ending normalization warnings only.

### Remaining environment gap

The SQL selections and mapper implementations were inspected field-by-field, and the configured live test covers all four official search shapes, but that live path could not execute without the disposable SQL Server connection. Visual Studio's project tree also remains a manual verification item.

## Third review - 2026-08-02

### Result

Approved. Both findings from the second review are resolved, every earlier finding remains resolved, and this review found no new issue.

### Second-review finding dispositions

1. **P2 generated mapper ownership and stale artifacts - resolved.** The handwritten `src/BeaconAr.Data/MasterData/Mappers` implementation is absent, and the Product, Customer, Address, and Carrier view repositories register only the central generator-owned `StoreProcedureMappersRegisterer`. The obsolete `ProductSearchRow`, `CustomerSearchRow`, `AddressSearchRow`, and `CarrierSearchRow` source types, data-reader mappers, and registrations are absent. Their procedure wrappers return the generated `ProductView`, `CustomerView`, `CustomerAddressView`, and `CarrierView` types directly. `BeaconAr.CodeGenerator` emits all four `// <auto-generated/>` data-reader mappers and their registrations under `BeaconAr.Data/Mappers`; no parallel owner remains.
2. **P3 sales workflow visibility - resolved.** `SalesWorkflowCoordinator` remains a public sealed constructor-injected type so Web API can register it, but all workflow properties and helper/operation methods are now `internal`. The architecture test proves that the declared public surface contains no property or method and that the intended same-assembly members remain internal. All three sales providers receive the one scoped coordinator through their constructors and none constructs it manually.

### Final consistency checks

- The four master-data search procedures select the complete corresponding generated-view shape. The selected SQL columns align field-for-field and type-for-type with the generated data-reader assignments, including Customer `PaymentTermsDays` as `Int16`, nullable audit/display values, expanded address references, and binary row versions.
- Generator discovery and both mapper registration lists use ordinal ordering while reflected properties preserve metadata/declaration order. The mapper target stages its complete output through `AtomicOutputDirectory` and commits it as one directory replacement.
- Mapper generation was run twice after the Release build. Relative-path/SHA-256 snapshots of all 15 generated files matched the checked-in state after the first run and matched again after the second run.
- `MasterDataProcedureViewMappersHaveOneGeneratedOwner` guards the generated marker, expected registrations, obsolete-type removal, absence of a parallel mapper directory, one configured mapper output, atomic generation, and deterministic procedure ordering.
- `WorkflowCoordinatorIsScopedAndConstructorInjected` guards scoped registration, constructor injection, absence of manual construction, and the coordinator's least-public surface.
- The official mutation fail-closed guard, official generated-view search path, one-round-trip procedure result shape, cancellation-aware application reads, and SQL-project identity/folder fixes from the previous reviews remain present and covered.

### Third-review verification

- `dotnet build src/BeaconAr.sln --configuration Release --no-restore`: passed with zero warnings and zero errors.
- Mapper generation twice with path/SHA-256 comparisons: 15 files, initial equals first run, and first run equals second run.
- Provider tests: 19 passed, zero failed.
- Architecture tests: 24 passed, zero failed.
- Complete solution tests: 154 total, 113 passed, 41 skipped, zero failed. The skipped database integration and authenticated acceptance cases require `ConnectionStrings__DatabaseConnection`, which is not configured in this environment.
- Paradigm semantic checks for Domain, Data, and Providers: passed.
- Strict database project validation: passed.
- `git diff --check`: no whitespace errors; line-ending normalization warnings only.

### Residual environment gap

The SQL-backed integration and authenticated acceptance cases remain environment-skipped rather than failed. Their search-shape assertions were reviewed statically, the database project builds and passes strict validation, and all non-live tests pass. Visual Studio's rendered project tree remains a manual IDE check; its explicit folder and item metadata is covered by architecture tests and the successful SQL project build.
