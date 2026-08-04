# Beacon AR Provider and API conventions - implementation plan

## Outcome

Make the Beacon AR application boundary honest and small: Providers coordinate collaborators and transactions, Domain/request objects own their own rules, and each multi-field search binds one existing request object from the query string. Remove the misleading generic MasterData edit surface instead of exposing generated views for writes or maintaining eight fail-closed overloads.

This plan was prepared on `beacon-ar-provider-api-conventions` after inspecting all Beacon AR Providers, Provider contracts, controllers, request contracts, Provider/WebApi tests, the current Paradigm Provider and controller bases, the official `Paradigm.Web.ApiTemplate`, and the relevant Provider/query-object examples under `C:\Repositories\github\microsoft\rdx-dms-mvp\src\api`. It applies the Provider, Web API, domain-model, review, and guidance-evolution skills and their linked coding, transaction, model, and controller references.

## Boundaries and non-goals

- Preserve SQL Server, the four established bounded contexts, route templates, operation IDs, status codes, authorization policies, ETag and idempotency behavior, Problem Details codes, OpenAPI schemas, and generated TypeScript client signatures unless a reviewed incompatibility is recorded before implementation.
- Keep purpose-built create/update/transition request types. Generated persistence views remain output-only and must not become write inputs merely to use `EditProviderBase`.
- Keep HTTP concerns in WebApi. Providers remain reusable outside HTTP and do not return MVC results or inspect headers.
- Keep database work behind repositories and typed routine wrappers. This task does not add repository SQL or redesign the stored procedures completed by earlier tasks.
- Do not derive protected Beacon controllers from Paradigm `ApiControllerBase`, `ReadApiControllerBase`, or `EditApiControllerBase`: those bases inherit `[AllowAnonymous]`, and `[Authorize]` does not override it.
- Do not add a NuGet package. The required binding, validation, and testing facilities already exist in ASP.NET Core and the approved test projects.

## Framework API decision

The exact current framework contracts were inspected in:

- `src/Paradigm.Enterprise.Providers/EditProviderBase.cs`
- `src/Paradigm.Enterprise.Providers/ReadProviderBase.cs`
- `src/Paradigm.Enterprise.Providers/IEditProvider.cs`
- `src/Paradigm.Enterprise.WebApi/Controllers/ApiControllerBase.cs`
- `src/Paradigm.Enterprise.WebApi/Controllers/ReadApiControllerBase.cs`
- `src/Paradigm.Enterprise.WebApi/Controllers/EditApiControllerBase.cs`

`EditProviderBase` is correct for conventional CRUD where a generated `TView` is the accepted write model. Beacon MasterData is not that case. Its public write protocol uses allow-listed command records, a required expected row version, current-user/time audit stamping, duplicate/reference checks, Operations audit rows, and a transaction spanning the participating contexts. The generic update API has no cancellation or expected-version argument, maps every `IProduct`/equivalent view member, and exposes single and bulk Add/Update/Save/Delete overloads. Enabling it would create overposting and concurrency bypasses; overriding all eight overloads to throw is not a valid permanent use of the framework.

Therefore this task removes `IEditProvider<TView,int>`/`EditProviderBase<...>` from the four MasterData Providers and keeps their explicitly safe command operations on exact-name `IProvider` contracts. It does not replace them with an unused `ReadProviderBase`: the public reads use cancellation-aware repository overloads, typed stored-procedure paging, `PageResult<T>`, and established application exceptions that the generic read surface does not carry. The repositories continue to use the official Paradigm read/edit repository bases.

If Paradigm later gains a command-based edit base with explicit concurrency, server-owned-field mapping, cancellation, transaction/audit participation, and safe bulk semantics, migrating these four Providers is a separate framework feature. Do not create a Beacon-only base class or pretend the current generic base satisfies that contract.

## Complete Provider inventory and disposition

| Provider/service | Current role | Implementation disposition |
| --- | --- | --- |
| `CurrentUserProvider` / `ICurrentUserProvider` | Resolves a verified actor, provisions or synchronizes ApplicationUser, checks active state, commits changes | Retain as a custom `IProvider` workflow. Move the private intrinsic shape check to instance behavior on `AuthenticatedIdentity`; the Provider invokes that contract, then keeps repository-, identity-, activity-, time-, and commit-dependent orchestration. Test no data call for invalid identity and no sync for inactive users. |
| `ProductProvider` / `IProductProvider` | Product search plus command-based create/update/delete, uniqueness, ETag concurrency, audit, transaction | Remove `IEditProvider` inheritance, `EditProviderBase`, and the fail-closed generic overloads. Constructor-inject `IProductRepository`, `IProductViewRepository`, `IUnitOfWork`, and `MasterDataMutationCoordinator`; replace `Repository`, `ViewRepository`, and `UnitOfWork` base properties with the typed collaborators. Retain the safe request-based operations because this is not conventional view CRUD. |
| `CustomerProvider` / `ICustomerProvider` | Customer search plus command-based create/update/delete, uniqueness, ETag concurrency, audit, transaction | Same disposition as Product, using the Customer repositories. Account-number uniqueness and reference checks remain Provider-owned; intrinsic Customer rules remain in `Customer.Behavior.cs`. |
| `CarrierProvider` / `ICarrierProvider` | Carrier search plus command-based create/update/delete, uniqueness/reference checks, ETag concurrency, audit | Same disposition as Product, using the Carrier repositories. Carrier intrinsic URL/state rules remain in `Carrier.Behavior.cs`. |
| `AddressProvider` / `IAddressProvider` | Address search plus customer/type lookup, locked default reassignment, reference protection, ETag concurrency, audit | Remove the misleading generic edit surface but retain the custom workflow. It is a multi-entity operation and cannot be expressed by a single CRUD hook. Constructor-inject both repositories, `IUnitOfWork`, and the coordinator. Keep customer/type availability and reference checks here; keep address/default compatibility in `CustomerAddress` behavior. |
| `CreationIdempotencyProvider` / `ICreationIdempotencyProvider` | Durable cross-host creation deduplication and replay | Retain. This is an Operations infrastructure/application service coordinating a repository, UoW transaction, persistence classification, current actor, clock, and caller delegates. It is not entity CRUD and belongs in Providers. |
| `DashboardProvider` / `IDashboardProvider` | One cancellation-aware dashboard snapshot query | Retain as the narrow query Provider. It exposes the use case and prevents a controller from depending on a repository; no extra validation or mapping is added. |
| `QuoteProvider` / `IQuoteProvider` | Quote aggregate creation/edit/deletion/transition plus reference resolution, histories, audit, concurrency | Retain as a custom aggregate workflow. It already implements `IProvider` directly and uses entity behavior for intrinsic rules. Keep only collaborator-dependent reference resolution in the Provider; remove local positive-ID branches where the request/entity contract can reject before data access, without moving rules into a broad helper. |
| `SalesOrderProvider` / `ISalesOrderProvider` | SalesOrder aggregate creation/edit/deletion/transition plus reference resolution, histories, audit, concurrency | Retain as a custom aggregate workflow for the same reason as Quote. Keep carrier/customer/address/product lookups and orchestration; intrinsic state/transition/line rules remain on SalesOrder and its children. |
| `QuoteConversionProvider` / `IQuoteConversionProvider` | Locked, idempotent Quote-to-SalesOrder conversion across aggregates | Retain unchanged in architectural role. The quote identifier is an application-operation precondition; conversion eligibility remains on Quote, while locking, singleton conflict recovery, transaction, audit, and reload remain Provider-owned. |
| `MasterDataMutationCoordinator` | Shared MasterData transaction, audit, actor/time, concurrency, persistence-error translation | Retain in Providers as a custom infrastructure/orchestration service. It must not gain entity rules or become a local Provider base. Keep its explicit transaction ownership and tracked-state discard behavior. |
| `SalesWorkflowCoordinator` | Shared Sales transaction, audit, actor/time, concurrency, persistence-error translation | Retain for the same reason. Do not move it to Domain or Data and do not turn it into service-locator inheritance. |
| `OfficialMasterDataMutationGuard` | Factory for exceptions thrown by the eight inherited generic edit overloads | Delete after the four Providers and contracts no longer expose `IEditProvider`. |

All ten public concrete Providers continue to implement their exact-name interfaces and `IProvider`, preserving convention discovery. The two coordinators remain services rather than discoverable Providers.

## Complete controller inventory and disposition

| Controller | Disposition |
| --- | --- |
| `ProductsController` | Replace the six primitive search arguments and object construction with `Search([FromQuery] ProductSearchRequest request, CancellationToken cancellationToken)`. Pass the same instance to the Provider. Keep protected `ControllerBase`, routes, policies, request limits, idempotency, ETags, and response codes. |
| `CustomersController` | Bind one `CustomerSearchRequest` from query; otherwise preserve behavior. |
| `CarriersController` | Bind one `CarrierSearchRequest` from query; otherwise preserve behavior. |
| `AddressesController` | Bind one `AddressSearchRequest` from query, covering all eight related filters/paging values; otherwise preserve behavior. |
| `QuotesController` | Bind one `QuoteSearchRequest` from query. Make that contract reliably complex-type-bindable without adding MVC references to Domain. Preserve detail compatibility DTOs and all mutation/transition/conversion routes. |
| `SalesOrdersController` | Bind one `SalesOrderSearchRequest` from query. Make that contract reliably complex-type-bindable and preserve all other behavior. |
| `DashboardController` | Retain. It is already a thin, protected one-call controller with no manual domain validation. |
| `MeController` | Retain. It returns the request-scoped resolved user and contains no persistence/domain validation. |

The existing Domain search request is the DTO; do not create a second WebApi query model with the same fields. `[FromQuery]` belongs on the action parameter, so Domain remains independent of ASP.NET Core. Property names already map to the established camel-case query names. If binding tests show positional record construction is not reliable, convert only `QuoteSearchRequest` and `SalesOrderSearchRequest` from positional records to one-type-per-file sealed classes with bindable `init` properties and the same defaults; update constructor call sites. Do not change their serialized names or validation behavior.

`ApiContract.EnsurePositiveId` and `ETagCodec.ParseRequired` are transport validation, not duplicated entity rules. Retain them unless the implementation introduces a centralized transport mechanism that demonstrably preserves the exact `invalid_query`, `invalid_if_match`, and `precondition_required` responses. Controllers must not call entity `Validate`, contain string/decimal/business rules, resolve repositories, or repeat request `Validate`; Providers invoke request-owned query validation so worker callers receive the same rule.

## Exact source changes

1. **MasterData Provider contracts and implementations**
   - Edit `IProductProvider.cs`, `ICustomerProvider.cs`, `ICarrierProvider.cs`, and `IAddressProvider.cs` to inherit `IProvider` rather than `IEditProvider<TView,int>`; retain only the cancellation-aware Search/Get/Create/Update/Delete methods actually supported by the application contract.
   - Edit the corresponding four Provider classes to implement the interfaces directly, inject typed repositories and `IUnitOfWork`, and replace inherited property access. Delete every generic Add/Update/Save/Delete override and the generic `SearchAsync<TParameters>` override.
   - Delete `OfficialMasterDataMutationGuard.cs`. Keep `MasterDataViewSearchParameters` only as the repository-base adapter required by `ReadRepositoryBase`; do not expose it through a Provider or controller.
   - Keep the current transaction and commit sequence until tests prove audit rows and primary mutations remain atomic. Generic edit operations already commit, but these Providers no longer call them, so each custom workflow retains explicit UoW saves owned by its transaction.

2. **Validation ownership cleanup**
   - Add instance validation behavior to `Domain/Access/Contracts/AuthenticatedIdentity.cs` (or an adjacent one-type partial if the record is made partial) and replace `CurrentUserProvider.Validate` with that call. Keep authenticated-actor completeness distinct from ApplicationUser persistence validation.
   - Audit every Provider source for intrinsic length/range/format/state logic. Move only rules decidable without collaborators to the owning Domain/request type and add direct tests. Keep uniqueness, existence, active-reference, identity/policy, concurrency, persistence classification, and remote/cross-aggregate checks in Providers.
   - In Sales, remove redundant positive-reference branches only when the owning request/entity validation is executed before repository access and preserves stable field errors. Do not introduce `SalesRequestValidator`, `*DomainValidation`, or another broad static owner.

3. **Query-object binding**
   - Edit the six search controllers listed above to accept one `[FromQuery]` request object plus cancellation.
   - If required for ASP.NET binding, edit `QuoteSearchRequest.cs` and `SalesOrderSearchRequest.cs` into bindable sealed classes with the exact existing properties/defaults and their existing instance `Validate` logic. The four MasterData search classes are already bindable.
   - Keep all query-shape rules (paging, search length, enum validity, filter identifiers, and sort allow-lists) on those request types. The controller only binds; the Provider invokes validation and delegates.
   - Confirm `BeaconArApiJsonContext` still lists every request/response/nested type. Do not add duplicate JSON metadata for a second query DTO.

4. **Tests and deterministic architecture fixtures**
   - Update `EstablishedLayerBoundaryTests.cs`: assert the four MasterData contracts implement `IProvider` but not `IEditProvider<,>`; assert their Provider base type is `object`; delete the obsolete fail-closed-overload assertion; keep exact-name discovery and repository-base assertions; assert generated views never appear in public Provider mutation parameters.
   - Add a source/metadata architecture test proving each of the six search actions has exactly one complex `[FromQuery]` request parameter and no parallel primitive query parameter list. Also prove protected controllers continue to derive from `ControllerBase` and carry the exact policies.
   - Update `ProductProviderTests.cs` construction to supply explicit repositories/UoW, remove `OfficialMutationSurfaceRejectsEveryUnsafeOverloadWithoutSaving`, and add an assertion that the unsupported generic edit surface is absent. Preserve tests for transaction, audit, stale concurrency, failed persistence cleanup, reference conflicts, cancellation, and query validation.
   - Extend `OtherMasterDataProviderTests.cs` as needed so Customer, Carrier, and Address cover the same explicit contract and the Address locked-default workflow.
   - Keep/extend `SalesProviderTests.cs` for collaborator-dependent checks and prove invalid intrinsic inputs fail before repository calls after any ownership move.
   - Add hosted WebApi binding cases for every search request: omitted defaults, every optional filter, invalid enum, invalid paging, invalid positive filter ID, sort allow-list, and cancellation. Compare returned Problem Details to the existing contract.
   - Keep the exact 36-operation authorization matrix and conditional/idempotent mutation tests unchanged.

5. **OpenAPI and client compatibility**
   - Generate the OpenAPI document after the controller refactor and compare the six search operations field-for-field: names, `in: query`, required flags, defaults, enum values, bounds/descriptions, ordering policy, response schemas, security, and operation IDs.
   - Regenerate the NSwag Angular client twice. `consumer.ts` must continue compiling with the current flat method argument signatures; a generated signature change is a compatibility defect unless separately documented and reviewed.
   - Update the checked OpenAPI hash only for canonical byte changes that are explained. Prefer no semantic OpenAPI/client diff.

6. **Durable guidance**
   - Update `.agents/skills/paradigm-build-provider/SKILL.md` to state that an incompatible command/concurrency/overposting contract must not inherit `IEditProvider` merely to throw every generic mutation; use the smallest honest Provider contract and record what future framework capability would enable migration.
   - Update `.agents/skills/paradigm-build-web-api/SKILL.md` to prefer one `[FromQuery]` request object when an action has several related query values, while keeping the Domain request MVC-independent and query rules on the request.
   - Update `.agents/skills/paradigm-review-change/SKILL.md` with deterministic review checks for primitive query-parameter explosions and permanent fail-closed inherited Provider surfaces. Link to existing central guidance rather than copying it.
   - Add Beacon-specific architecture fixtures for the exact controllers/contracts. Do not add a universal CLI diagnostic for this judgment-heavy base-selection decision in this task.

7. **Task documentation**
   - Write `change-summary.md` after implementation, and maintain `review-feedback.md` through review/fix cycles. Add any newly ambiguous framework decision to `decisions.md` rather than hiding it in code comments.

## Verification

Run from `examples/beacon-ar`:

```powershell
dotnet restore BeaconAr.slnx --locked-mode
dotnet build BeaconAr.slnx --configuration Release --no-restore
dotnet test --solution BeaconAr.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 1
dotnet tool run paradigm doctor --project BeaconAr.slnx
dotnet tool run paradigm packages check --project BeaconAr.slnx
dotnet tool run paradigm packages audit --project BeaconAr.slnx
dotnet tool run paradigm validate --project BeaconAr.slnx
dotnet tool run paradigm checks run --project BeaconAr.slnx
./scripts/generate-openapi.ps1
dotnet run --project src/BeaconAr.CodeGenerator/BeaconAr.CodeGenerator.csproj --configuration Release --no-build -- openapi-typescript ../../artifacts/openapi/beacon-ar-v1.json ../../tests/BeaconAr.ClientContract/generated/beacon-ar-v1.ts
npm --prefix tests/BeaconAr.ClientContract ci
npm --prefix tests/BeaconAr.ClientContract run check
git diff --check
```

Generate OpenAPI and the TypeScript client a second time and require byte stability. Run the live SQL Server database and WebApi suites against a freshly published disposable database because Provider transaction, concurrency, audit, reference, default-address, idempotency, and route behavior cannot be proved only with mocks. Run repository-source CLI or per-application commands and record exact gaps if the known whole-solution SQL-project inspection defect remains.

## Acceptance checklist

- Every Provider/controller in the inventories above has the stated disposition; no discoverable implementation or endpoint is skipped.
- The four MasterData Provider contracts expose only the supported request-based surface, do not inherit `IEditProvider`, and no fail-closed overload guard remains.
- No generated entity/view is accepted by a public mutation method; concurrency, audit, idempotency, and transaction behavior remains intact.
- Provider code contains orchestration and collaborator-dependent checks, not entity rule implementations or broad validators.
- All six multi-parameter searches bind one `[FromQuery]` request object and retain their existing query names/defaults/results.
- Controllers retain only routing, binding, authorization, HTTP preconditions/headers/statuses, idempotency integration, and Provider calls.
- Routes, operation IDs, security, statuses, Problem Details, OpenAPI semantics, and strict generated-client consumption are unchanged or have an explicit reviewed decision.
- Guidance records both durable lessons: do not maintain a permanently disabled inherited CRUD surface, and bind related query values as one request object.

