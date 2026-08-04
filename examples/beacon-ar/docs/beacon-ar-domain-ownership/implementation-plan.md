# Beacon AR domain ownership — implementation plan

## Outcome

Move intrinsic rules into the objects that own the state, make the generated SQL Server views the normal read contracts, and move the four seeded catalog enums into `BeaconAr.Interfaces`. Preserve the database-first/generated boundary and the existing write-request boundary so this task does not create an overposting path while preparing the later Provider/API simplification task.

This plan was prepared from the post-context-boundary `beacon-ar-domain-ownership` branch. It applies the Paradigm model-domain, Provider, Web API, database, review, and guidance-evolution skills; the good coding and database practices; and the current Beacon AR source/tests. It does not edit generated EF/T4 output directly.

## Scope boundaries

- In scope: Domain ownership and tests, catalog enum ownership/parity, canonical read-shape cleanup, the minimum Data/Provider/WebApi changes required to compile and preserve behavior, deterministic generation/architecture checks, and durable framework guidance learned from the change.
- In scope: SQL Server routine result-shape changes needed to return generated `QuoteView` and `SalesOrderView` from searches.
- Deferred to the Provider/API task: selecting/removing generic CRUD bases, replacing the existing fail-closed inherited edit methods, lifecycle-hook simplification, and consolidating controller query parameters into `[FromQuery]` parameter objects.
- Not in scope: changing persistence contexts, physical project locations, SQL Server as the database engine, workflow transitions, route names, authorization policies, or adding a new database table/view merely to avoid a justified composite response contract.

## Audited current state

### Broad validators and helpers

| Current type | Current responsibility | Planned disposition |
| --- | --- | --- |
| `MasterDataRequestValidator` | Normalizes and validates four entity families plus two search parameter families | Remove. Move Product, Customer, CustomerAddress, and Carrier rules into their co-located partials. Put query-only checks on the request/parameter instances that own those values. |
| `ValidationErrorBuilder` | MasterData-wide field-error accumulator | Remove with the broad validator. Entity `ValidateEntity` hooks use `DomainValidator`; transport/query contracts may build their own stable field errors without containing entity rules. |
| `SafeUrlValidator` | Product thumbnail and Carrier tracking-template rules | Remove. These are different intrinsic rules and become private entity-owned checks in `Product.Behavior.cs` and `Carrier.Behavior.cs`. |
| `SalesDomainValidation` | Quote/Order header, reference, line, pricing, normalization, and aggregate rules | Remove. Quote owns quote dates/notes/aggregate/reference rules, SalesOrder owns carrier/tracking/order aggregate rules, and each line entity owns its pricing input rules. |
| `SalesRequestValidator` | Write/search validation plus rowversion encoding/decoding | Remove. Entity behavior owns write invariants; search contracts own query checks; callers use the single Operations `VersionTokenCodec`. |
| MasterData `VersionTokenCodec` | Capability-specific wrapper around SQL Server rowversion validation | Remove. Use the existing Operations codec as the one narrow concurrency-protocol helper. |
| Operations `VersionTokenCodec` | Canonical eight-byte SQL Server rowversion protocol | Retain as a documented narrow transport/concurrency helper, not an entity validator. |
| `MonetaryRounding` | SQL Server-compatible monetary rounding policy | Retain as a documented narrow value policy used by both Sales line entities and parity tests. |
| `AddressTypeCodes` | Stable catalog machine-code constants | Replace with catalog metadata owned beside `AddressType` in Interfaces; do not leave a Domain-wide static code catalog. |

### Generated model and handwritten partial coverage

Generated `.cs` files remain replaceable. All handwritten behavior stays in the same capability/entity directory and namespace as its generated half.

| Context | Generated entities/views | Current handwritten behavior | Plan |
| --- | --- | --- | --- |
| Access | `ApplicationUser`, `ApplicationUserView` | `ApplicationUser.Behavior.cs`; generated `Validate` hook is currently empty | Add normalization and intrinsic identity/display/email validation to the partial; make create/synchronize validate before mutation. Keep the view read-only. |
| MasterData | `AddressType`; `Carrier`, `Customer`, `CustomerAddress`, `Product` and their four `*View` types | Four co-located behavior partials delegate to `MasterDataRequestValidator`; catalog has none | Implement each entity's own proposed-state normalization, atomic create/replace, `ValidateEntity`, and `BeforeMap`/`AfterMap` hooks where generic mapping can reach it. Keep the seeded catalog and all views read-only. |
| Operations | `AuditLog`, `IdempotencyRequest`, `IdempotencyState` and their three `*View` types | Only `IdempotencyRequest.Behavior.cs`; generated validation hooks are empty | Add an `AuditLog` factory/validation partial, validate idempotency creation/completion and legal state, and keep the seeded state catalog/views read-only. Coordinators call entity factories rather than populate facts with object initializers. |
| Sales | `Quote`, `QuoteLine`, `QuoteStatus`, `QuoteStatusHistory`, `SalesOrder`, `SalesOrderLine`, `SalesOrderStatus`, `SalesOrderStatusHistory` plus six generated views | Six co-located behavior partials; aggregate partials delegate to `SalesDomainValidation`; line `ValidateEntity` hooks are empty | Put header/transition/aggregate rules on Quote and SalesOrder, line/pricing rules on QuoteLine and SalesOrderLine, and fact validation on both histories. Keep seeded status entities and all views read-only. |

Views do not gain write behavior or validation merely because they are partial. They are database-generated read shapes. Catalog entities are deployment-owned and must not acquire application mutation APIs. Audit/status-history facts receive construction validation but no update/delete behavior.

The generated-hook inventory is explicit:

- Generated mapped entities with `Validate`, `MapFrom`, `MapTo`, `ValidateEntity`, `BeforeMap`, and `AfterMap` hooks are `ApplicationUser`, `Product`, `Customer`, `CustomerAddress`, `Carrier`, `AuditLog`, `IdempotencyRequest`, `IdempotencyState`, `Quote`, `QuoteLine`, `SalesOrder`, and `SalesOrderLine`. Today only Quote and SalesOrder implement `ValidateEntity`, and those implementations validate children rather than the complete root; the other hooks are unimplemented apart from generated mapper configuration.
- Generated entities without the mapped entity/view base are `AddressType`, `QuoteStatus`, `QuoteStatusHistory`, `SalesOrderStatus`, and `SalesOrderStatusHistory`. The two history types have construction partials; the three catalogs have no handwritten behavior and stay deployment-owned.
- The complete generated view inventory is `ApplicationUserView`; `CarrierView`, `CustomerView`, `CustomerAddressView`, `ProductView`; `AuditLogView`, `IdempotencyRequestView`, `IdempotencyStateView`; and `QuoteView`, `QuoteLineView`, `QuotePricingView`, `SalesOrderView`, `SalesOrderLineView`, `SalesOrderPricingView`. None currently has a handwritten partial, and none needs one for this task.
- The complete current behavior-partial inventory is `ApplicationUser.Behavior.cs`; `Carrier.Behavior.cs`, `Customer.Behavior.cs`, `CustomerAddress.Behavior.cs`, `Product.Behavior.cs`; `IdempotencyRequest.Behavior.cs`; and `Quote.Behavior.cs`, `QuoteLine.Behavior.cs`, `QuoteStatusHistory.Behavior.cs`, `SalesOrder.Behavior.cs`, `SalesOrderLine.Behavior.cs`, `SalesOrderStatusHistory.Behavior.cs`. Add `AuditLog.Behavior.cs`; do not create behavior files for read-only catalogs/views.

### Seeded system catalogs

The complete seeded catalog inventory is:

| SQL table/seed | Interface enum | Required numeric/code pairs |
| --- | --- | --- |
| `AddressTypeData.sql` | `BeaconAr.Interfaces.MasterData.Enums.AddressType` | `Billing=1/billing`, `Shipping=2/shipping`, `Both=3/both` |
| `IdempotencyStateData.sql` | `BeaconAr.Interfaces.Operations.Enums.IdempotencyState` | `InProgress=1/in_progress`, `Completed=2/completed`, `Failed=3/failed` |
| `QuoteStatusData.sql` | `BeaconAr.Interfaces.Sales.Enums.QuoteStatus` | `Draft=1/draft`, `Sent=2/sent`, `Accepted=3/accepted`, `Rejected=4/rejected`, `Expired=5/expired` |
| `SalesOrderStatusData.sql` | `BeaconAr.Interfaces.Sales.Enums.SalesOrderStatus` | `Draft=1/draft`, `Confirmed=2/confirmed`, `Processing=3/processing`, `Shipped=4/shipped`, `Completed=5/completed`, `Cancelled=6/cancelled` |

The enum declarations move out of Domain. Give each member explicit stable machine-code metadata so tests compare both ID and code, not only the final row or count. Generated EF catalog entity classes with the same simple names remain in `BeaconAr.Domain.<Capability>.Entities`; use explicit namespaces/aliases at collision points.

### Manual contract disposition

| Contract(s) | Decision and reason |
| --- | --- |
| `QuoteSummaryDto`, `SalesOrderSummaryDto` | Remove. They duplicate generated `QuoteView`/`SalesOrderView`; searches must return `PageResult<QuoteView>` / `PageResult<SalesOrderView>`. |
| `QuoteSearchRow`, `SalesOrderSearchRow` and their generated data-reader mappers | Remove after the SQL routines return the exact canonical view column sets and the existing generated view mappers are used. These are transitional parallel read models. |
| `QuoteDto`, `SalesOrderDto`, `SalesLineDto` | Retain for this version as one explicitly named external compatibility contract: detail/create/update/transition responses are flattened snapshots with nested lines and a stable `Status`/`Version` surface, while no one-row database view can represent the nested collection without duplicating the header. Repositories must source them only from `QuoteView` + `QuoteLineView` or `SalesOrderView` + `SalesOrderLineView`, not persistence entities or parallel projections. Removal trigger: a versioned client contract accepts canonical header/line view composition. |
| `CurrentUserDto` | Retain. It combines an ApplicationUser projection with evaluated authorization policies; no database view can own the policy result. |
| `DashboardSummaryDto` | Retain. It is the single snapshot result of `GetDashboardSummary`, not an entity projection and has no database view. |
| `QuoteConversionResult`, `CreationResult<T>`, `PageResult<T>` | Retain as operation/pagination envelopes, not parallel entity DTOs. |
| Create/update/status-transition requests and `SalesLineRequest` | Retain as purpose-built write commands. They exclude identity, audit, snapshots, workflow-owned state, deletion, computed totals, and unrestricted rowversion fields, preventing overposting. |
| Search requests, `MasterDataViewSearchParameters`, `AddressUsage`, `SortDirection` | Retain as query/transport contracts. Move validation to instance behavior; Task 5 may consolidate the two MasterData search surfaces with the generic Provider/API work. These enums are not seeded system catalogs. |
| `AuthenticatedIdentity` | Retain as a verified identity input contract, not a persistence DTO. |
| Stored-procedure parameter/result types for reference locks, dashboard, and other routines | Retain inside Data because they are private typed database boundaries, not public application DTOs. Remove only the two Sales search rows that exactly duplicate generated views. |

## Implementation sequence

1. **Protect the generated boundary and baseline.** Record a clean Release build/test baseline. Capture hashes for generated entities, views, contexts, mapper output, EFPT configurations, and official T4 templates. Add/adjust architecture manifests before regeneration so an implementation cannot silently edit generated source or lose a co-located partial.

2. **Move the four catalog enums to Interfaces.** Add one enum per file under capability `Enums` folders in `BeaconAr.Interfaces`, with explicit IDs and stable code metadata. Update Domain, Providers, WebApi serialization, Data, tests, and aliases to consume the Interface types. Delete the Domain enum declarations and `AddressTypeCodes`. Keep the generated catalog entity classes unchanged. Add exact seed-to-enum parity tests that parse all rows, fail for a missing/extra/renumbered/re-coded value, and verify enum assembly/namespace ownership.

3. **Make entity mapping and mutation atomic.** For Product, Customer, CustomerAddress, Carrier, and any mapped mutable entity reached through `MapFrom`, build and validate a complete normalized proposed state before changing tracked properties. Implement `ValidateEntity`; use the generated partial mapping hooks to prevalidate and normalize without resolving repositories/services. Do not edit generated mapper/entity files. Preserve server-owned ID, audit, snapshot, deletion, workflow, and concurrency fields. Add a failed-map test proving the tracked entity is byte-for-byte/state-equivalent after rejection.

4. **Move MasterData rules to their entities.** Fold each branch of `MasterDataRequestValidator` into the owning partial and delete the broad/static validator classes. Product owns SKU/name/category/price/stock/thumbnail rules; Customer owns account/name/email/phone/credit/payment-term rules; CustomerAddress owns customer/type/default/address/country rules using the stable `AddressType` IDs; Carrier owns code/name/service/tracking-template rules. Entity factories and replacement methods normalize once, validate once, and only then assign. Provider uniqueness/reference checks remain Provider-owned. Search parameter objects get instance validation containing only paging/filter/sort rules.

5. **Move Access and Operations rules to their entities.** Normalize and validate ApplicationUser before create/synchronize. Add an `AuditLog.Create(...)` factory and intrinsic validation; change the two coordinators to use it. Validate IdempotencyRequest creation, completion, state transitions, hashes, timestamps, and response status in its partial. Retain repository-, identity-, and transaction-dependent checks in Providers. Do not expose mutation APIs for AuditLog, histories, or catalog entities.

6. **Move Sales rules to aggregates and lines.** Delete `SalesDomainValidation` and `SalesRequestValidator`. Quote owns dates, note normalization, customer/shipping eligibility, product uniqueness, aggregate amount bounds, completeness, transitions, tombstoning, and atomic replacement. SalesOrder owns customer/shipping/carrier rules, tracking normalization, aggregate bounds, source-quote invariants, completeness, transitions, tombstoning, and atomic replacement. QuoteLine/SalesOrderLine own product ID, snapshot lengths, quantity, unit-price scale/range, discount scale/range, and calculated amount bounds through `ValidateEntity`. Histories validate their root/status/actor/time construction. Search requests validate themselves; Provider concurrency code uses the Operations version codec directly.

7. **Make generated views canonical for Sales reads.** Change `SearchQuote.sql` and `SearchSalesOrder.sql` so their second result set exactly matches `QuoteView` and `SalesOrderView`, retaining server-side filtering, paging, allow-listed ordering, deleted-row exclusion, and metadata-first ordering. Change stored-procedure wrappers to return generated view types and use their existing generated reader mappers; remove the two search row types/mappers and summary DTOs. Refactor detail repositories to read `QuoteView`/`QuoteLineView` and `SalesOrderView`/`SalesOrderLineView`, then map only at the named external compatibility boundary. Update Provider contracts/controllers/JSON context/OpenAPI artifact/client fixture for the search type changes without doing Task 5's query-binding or CRUD-base redesign.

8. **Add regression coverage at the cheapest boundary.** Expand Domain tests with direct construction for every valid/invalid boundary, normalization, state transition, aggregate duplicate/overflow, reference-dependent behavior, and failed replacement/map atomicity. Add architecture tests for enum ownership, complete seed numeric/code parity, no Domain `*RequestValidator`/`*DomainValidation` classes, co-location/namespace parity for every handwritten partial, a reviewed allow-list of retained manual public read DTOs with justification, Provider search contracts returning generated views, and no persistence entity/view accepted as a public write command. Update Provider/WebApi/database integration tests only where contract types change.

9. **Evolve durable guidance.** Update the central good-coding/domain guidance to say that rules decidable from an entity/view state live in its partial behavior/`Validate`, while query/transport validation stays on the request and repository/identity checks stay in Providers. Update database/model guidance to prefer capability enums in an Interfaces project when one exists and to test complete ID/code parity. Update Provider/review guidance to reject parallel read DTOs when a generated view is canonical and to require a named compatibility contract plus removal trigger for exceptions. Keep judgment-based classification in skills; promote only the deterministic Beacon checks supported by source/seed evidence.

10. **Verify and document.** Build the SQL project, run strict database validation, regenerate persistence/mappers twice and require byte stability, build/test the solution, run Paradigm doctor/packages/validate/checks/audit, regenerate OpenAPI/client output, and run live SQL Server tests against a disposable published database. Record high-level changes, review findings, remediation cycles, validation evidence, and any further implementation decisions in this task folder.

## Required verification

Run from `examples/beacon-ar` (using the repository's configured local package feed and a disposable SQL Server connection for live suites):

```powershell
dotnet restore BeaconAr.slnx --locked-mode
dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release
dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution BeaconAr.slnx --strict --format json
./build/regenerate-persistence.ps1
dotnet build BeaconAr.slnx --configuration Release --no-restore
dotnet test --solution BeaconAr.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 1
dotnet tool run paradigm doctor --project BeaconAr.slnx
dotnet tool run paradigm packages check --project BeaconAr.slnx
dotnet tool run paradigm packages audit --project BeaconAr.slnx
dotnet tool run paradigm validate --project BeaconAr.slnx
dotnet tool run paradigm checks run --project BeaconAr.slnx
./scripts/generate-openapi.ps1
```

Also run the persistence/mapper generator a second time and require no diff; run the complete live database and Web API integration suites against the freshly published SQL Server database. If an installed CLI defect prevents a whole-solution command, run the repository-source CLI plus each consuming application project and record the exact gap rather than claiming the command passed.

## Acceptance checklist

- No intrinsic Product, Customer, CustomerAddress, Carrier, ApplicationUser, AuditLog, IdempotencyRequest, Quote, SalesOrder, line, or history rule is implemented in a broad static validation class, Provider, or controller.
- Every mutable generated entity invokes its co-located partial validation; failure-prone mapping/replacement prevalidates before mutation.
- All four seeded catalog enums are public Interface types and match every SQL seed ID/code exactly; Domain contains no duplicate catalog enum.
- Generated views remain read-only generated persistence shapes and are the normal Provider read contracts.
- Quote/Order search summaries and internal parallel search rows are removed. Every retained manual public DTO is listed above with a concrete non-view/client-contract reason and removal trigger where applicable.
- Purpose-built write requests remain the mutation boundary; no API accepts a persistence entity or broad read view for writes.
- Generated files and official T4 templates are not hand-edited, regeneration is byte-stable, and handwritten partials survive regeneration.
- Domain, Provider, WebApi, architecture, database, live SQL Server, OpenAPI/client, and CLI checks pass or have an explicitly evidenced tool/environment gap.
