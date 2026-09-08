# Beacon AR sales-workflows implementation plan

> **Historical persistence note (superseded 2026-08-03):** This document records an earlier implementation stage. Current persistence ownership is defined by docs/beacon-ar-context-boundaries: four disjoint Access, MasterData, Operations, and Sales contexts/configurations; there are no Receivables or Reporting application roots.

## Objective

Implement the complete release-one Quote, accepted-quote conversion, direct Sales Order, fulfillment, and Dashboard application capabilities through `BeaconAr.Domain`, `BeaconAr.Data`, `BeaconAr.Providers`, and the SQL Server database project. Deliver authoritative snapshots and pricing, bounded server-side searches, draft replacement and tombstoning, both lifecycle state machines with append-only histories, optimistic concurrency, transactional audit, exactly-once quote conversion, and one internally consistent database-side dashboard aggregate.

This task stops at the transport-neutral Provider boundary. The final `beacon-ar-api-security-contract` task owns `/api/v1` controllers, route/status semantics, JWT policies, ETag/`If-Match` parsing, Problem Details translation, JSON/OpenAPI registration, and the `201` versus `200` HTTP response for quote conversion. Provider results created here must carry enough information for that task without referencing ASP.NET Core.

## Inspected baseline and fixed decisions

- The branch starts after the foundation and master-data work. `ReceivablesDbContext : DbContextBase<int>` is the one release-one consistency boundary, and repositories register this context with the scoped Paradigm Unit of Work.
- Generated files under `Receivables/Generated` are replaceable. Add behavior in handwritten partials and regenerate only after database source builds; never edit generated entities, context, or mapper output directly.
- The database already contains `Quote`, `QuoteLine`, `QuoteStatusHistory`, `SalesOrder`, `SalesOrderLine`, `SalesOrderStatusHistory`, the two status catalogs, number sequences, pricing views, audit storage, all non-cascading FKs, line-product uniqueness, order-source-quote uniqueness, tombstone columns, and root `rowversion` tokens.
- Status IDs are already published and must remain aligned with the .NET enums: Quote `Draft=1`, `Sent=2`, `Accepted=3`, `Rejected=4`, `Expired=5`; Sales Order `Draft=1`, `Confirmed=2`, `Processing=3`, `Shipped=4`, `Completed=5`, `Cancelled=6`.
- Pricing is fixed by the foundation: inputs use `decimal`, each line subtotal and discount is rounded to two decimals midpoint-away-from-zero, line total is rounded subtotal minus rounded discount, and aggregate totals sum persisted rounded line values. The existing schema-bound pricing views remain authoritative.
- Quote and order deletion is a domain-specific tombstone, not a general soft-delete convention. Lines and histories remain stored; ordinary searches/details exclude a tombstoned root.
- Number sequences format immutable `Q-00000001` and `SO-00000001` values. Gaps caused by rollback are valid; uniqueness and immutability matter, not contiguity.
- Existing master-data Providers use handwritten `IProvider` implementations, `RepositoryBase<ReceivablesDbContext,int>`, explicit `IUnitOfWork` transactions, `TimeProvider`, a request-scoped actor/correlation context, safe audit facts, canonical Base64 row versions, and persistence-conflict classification. Sales workflows extend these conventions rather than introducing a second transaction/audit abstraction.
- Before implementation, promote genuinely shared operation contracts currently under `MasterData` (`IApplicationOperationContext`, audit repository, persistence-session reset, version codec, and common conflict primitives) into capability-neutral `Operations` namespaces and update existing consumers without behavioral changes. Keep sales-only validation, exceptions, and SQL constraint classification in `Sales`.

## Scope

### Included

- Quote and Sales Order create, detail, search, draft replace, and eligible tombstone operations.
- Quote and order line reconciliation as aggregate-root behavior; callers never edit a line independently.
- Authoritative product, customer, and address snapshots, deterministic pricing, and inactive-reference rules.
- All allowed and rejected Quote and Sales Order status transitions, including shipping carrier/tracking assignment.
- Initial and transition history rows, mutation audit facts, and concurrency-conflict translation.
- Direct draft-order creation and accepted-quote singleton conversion with transactional and concurrent idempotence.
- Dashboard summary counts calculated in the database in one serializable operation.
- Domain, Provider, architecture/discovery, database-object, and exhaustive live SQL Server tests.

### Deferred

- Controllers and every HTTP concern: authentication/authorization, routes, model binding, policy attributes, response codes, ETags, idempotency headers, Problem Details, OpenAPI, NSwag, CORS, HTTPS, `/me`, and health-route exposure.
- The general `IdempotencyRequest` HTTP-key workflow for POST create requests. Quote conversion is naturally keyed by `QuoteId` and the unique `SalesOrder.SourceQuoteId`; do not require a client idempotency key for it.
- Invoicing, taxes, payments, inventory reservation, automatic quote expiration, shipping integrations, tracking-URL fetching, notifications, and external side effects.
- Physical purge of tombstoned transactions or mutation/deletion endpoints for status-history and audit rows.
- Dashboard caching or a reporting replica. Release one reads the authoritative receivables database.

## Application contracts

Place transport-neutral request/response types under `BeaconAr.Domain/Sales/Contracts` and reporting types under `BeaconAr.Domain/Reporting/Contracts`. Reuse the existing application-owned `PageResult<T>` and `SortDirection`; do not expose EF entities, navigation graphs, database connections, or ASP.NET types.

### Commands and search inputs

1. `SalesLineRequest(ProductId, Quantity, UnitPrice, DiscountPercent)` is the only client-written line shape. It contains no line ID, snapshot text, or derived amount.
2. `QuoteCreateRequest` contains customer/address IDs, `QuoteDate`, `ValidUntil`, optional notes, and at least one line. `QuoteUpdateRequest` contains the same editable fields but no ID, number, status, audit fields, totals, or version.
3. `SalesOrderCreateRequest` contains customer/address IDs, optional requested ship date, optional carrier ID and tracking number, and at least one line. It always creates a direct order; there is no client-written `SourceQuoteId`. `SalesOrderUpdateRequest` exposes the same draft-editable fields only.
4. `QuoteStatusTransitionRequest` contains only the requested status. `SalesOrderStatusTransitionRequest` contains requested status plus optional `CarrierId` and `TrackingNumber`; those two values are consumed only by the transition to `Shipped` and are applied atomically with it.
5. `QuoteSearchRequest` and `SalesOrderSearchRequest` reuse page defaults `1/10`, maximum page size `100`, trimmed search text, and validated sort direction. Quote filters are nullable status and customer ID; allowed sorts are `quoteNumber|quoteDate|validUntil|status`. Order filters are nullable status, customer ID, and source quote ID; allowed sorts are `orderNumber|status|requestedShipDate`. Add stable `Id` tie-breaking to every non-ID sort.
6. Reject unknown enum values, nonpositive filter IDs, invalid paging, and unknown sort fields before Data is called. Database procedures still validate their sort allow-lists defensively.

### Read models

- `SalesLineDto` returns product ID, authoritative SKU/name snapshots, quantity, unit price, discount percent, and the three non-null two-decimal derived amounts.
- `QuoteSummaryDto` returns ID, immutable quote number, customer ID and identifying snapshot, dates, status, aggregate totals, conversion order ID when present, audit metadata, and canonical version.
- `QuoteDto` adds all customer/shipping snapshots, notes, deterministic lines, aggregate totals, and conversion link. It does not expose mutable catalog navigation objects.
- `SalesOrderSummaryDto` returns ID, immutable order number, nullable source quote ID, customer identifying snapshot, status, requested ship date, carrier/tracking summary, totals, audit metadata, and canonical version.
- `SalesOrderDto` adds all customer/shipping snapshots, carrier ID, tracking number, deterministic lines, totals, and source quote link.
- `QuoteConversionResult(SalesOrderDto SalesOrder, bool Created)` distinguishes the first creation from a replay so the later controller can emit `201` or `200`.
- `DashboardSummaryDto` uses 64-bit counts for `Products`, `Customers`, `Carriers`, `OpenQuotes`, and `ActiveOrders`, plus one `DateTimeOffset AsOf` value generated by SQL Server in UTC.
- Status history remains an internal immutable fact in release one; detail responses need not expose it. Its repository has append-only staging only, and tests query it directly.

### Application failures and versions

- Add field-addressable `SalesValidationException` and a `SalesException` carrying stable safe codes: `not_found`, `concurrency_conflict`, `invalid_quote_transition`, `invalid_sales_order_transition`, `quote_not_accepted`, `reference_inactive`, `invalid_shipping_address`, and reviewed duplicate/integrity codes.
- Draft update, tombstone, and status-transition Provider methods take the path-authoritative integer ID plus a separate expected-version string. Validate canonical eight-byte rowversion syntax before opening a transaction, compare it before mutation, and retain EF's rowversion predicate as the final race guard.
- Conversion takes the quote ID and expected quote version. Check for an existing order first and return it regardless of a replayed token; only the first conversion validates the token and accepted state. The source Quote row is not mutated by conversion, so a successful first call does not make an immediate retry stale.
- HTTP mapping of these failures and the decision about requiring `If-Match` on each route remain in the final API task.

## Domain model and validation

### Aggregate boundaries and atomic mapping

- Add handwritten `Quote.Behavior.cs`, `QuoteLine.Behavior.cs`, `SalesOrder.Behavior.cs`, and `SalesOrderLine.Behavior.cs` partials outside generated folders. Public generated setters remain the documented database-first exception; Providers use intention-revealing factories/behavior only.
- Quote and Sales Order are aggregate roots. Lines are reachable and reconciled only through their root. Repository contracts never expose independent line create/update/delete methods.
- Normalize and validate a complete proposed header and line set before changing a tracked aggregate. Rejecting a request must leave the entity and its children unchanged. Reconcile existing lines only after validation and reference resolution succeeds.
- A successful draft replacement stamps the root modification audit fields even when only lines changed, ensuring the root `rowversion` advances. Omitted children are explicitly staged for removal by the aggregate repository because database FKs do not cascade.
- Line response order is deterministic by line `Id`; new lines use their generated ID. Release one does not invent user-controlled line numbering.

### Shared line and pricing rules

- Require one or more lines, positive product IDs, no repeated product ID, positive integer quantity, nonnegative `decimal(19,4)` unit price, and discount percent from `0` through `100` with at most the schema's two-decimal scale.
- Resolve all distinct product IDs in one bounded query. New/reselected products must exist and be active. Set `SkuSnapshot` and `ProductNameSnapshot` from the resolved Product; never accept them from a command.
- On draft replacement, an existing inactive-product line may remain only when product ID, quantity, unit price, and discount percent are unchanged. Preserve its original snapshots. Adding, reselecting, or changing any value on an inactive-product line returns a field-addressable validation error.
- Use `MonetaryRounding` to calculate expected line values in domain tests, but do not persist or trust application-calculated totals. Reload DTOs from the computed columns/pricing views after commit.

### Quote behavior

- `CreateDraft` validates dates (`ValidUntil >= QuoteDate`), notes after trim/null normalization with maximum length 1,000, and lines. It receives an allocated number, authoritative customer/address snapshots, actor, and UTC time; it sets Draft and records no caller-supplied immutable field.
- Creation requires an existing active customer and an address belonging to that customer whose current type is `shipping` or `both`. Snapshot account/name/email/phone and the full shipping address plus transported address-type code from those rows.
- A draft replacement may change customer/address. A changed reference must meet the same active-customer, ownership, and shipping-usage rules and refresh all customer/address snapshots together. Unchanged references retain their stored snapshots so later master-data edits never rewrite transaction history.
- Only Draft can be replaced or tombstoned. Tombstoning sets `DeletionDate`, `DeletedByUserId`, and modification audit fields; it preserves lines, histories, number, and snapshots. Deleted quotes are treated as absent by ordinary application reads and cannot transition or convert.
- Encode exactly: `Draft -> Sent`; `Sent -> Accepted|Rejected|Expired`; terminal states have no outgoing transition. Every invalid, skipped, repeated, or transition-on-deleted request produces `invalid_quote_transition` without mutation.
- Sending performs full intrinsic validation of the stored aggregate. It does not recalculate snapshots from current master data and does not reject a previously captured inactive product solely because it later became inactive.

### Sales Order behavior

- `CreateDirectDraft` allocates an immutable order number, forces `SourceQuoteId = null`, validates requested ship date and lines, and resolves active customer/address/product selections. Capture customer and address snapshots on creation; refresh them together only if a draft edit selects another eligible customer/address.
- A direct draft may carry an optional carrier/tracking pair for later use, but a newly assigned carrier must exist and be active and blank tracking text normalizes to null. Changing to another carrier repeats the active check. An unchanged historical carrier remains readable if later deactivated.
- `CreateFromQuote` is a separate factory. It accepts only the accepted source aggregate, copies customer/address and line snapshots and the three line pricing inputs exactly, assigns Draft, and never resolves current master-data text or price. Its `SourceQuoteId` is immutable and is absent from all edit requests.
- Only Draft can be replaced or tombstoned. Tombstoning preserves the complete transaction history. Converted and direct orders use the same state machine and concurrency protocol.
- Encode exactly: `Draft -> Confirmed|Cancelled`; `Confirmed -> Processing|Cancelled`; `Processing -> Shipped|Cancelled`; `Shipped -> Completed`; Completed and Cancelled are terminal. Reject skipped/repeated transitions with `invalid_sales_order_transition` and leave header/history/audit unchanged.
- Confirmation validates the complete stored order and requires all customer/address snapshot fields to be populated. Shipping requires a supplied or already assigned active carrier and a nonblank tracking number. Apply carrier/tracking changes, current status, modification audit fields, history, and audit in the same transaction. Carrier/tracking values on a non-shipping transition are rejected rather than silently ignored.

## Data and database work

### Repository contracts and discovery

Define application repository contracts in Domain inheriting `IRepository`, with one public sealed exact-name Data implementation for each interface. Use `RepositoryBase<ReceivablesDbContext,int>` because these workflows need aggregate loads, projections, locking, stored procedures, and explicit staged writes.

- `IQuoteRepository` / `QuoteRepository`: allocate the next formatted number; load a nondeleted aggregate with lines for update; stage root, initial/transition history, removed children, and tombstone changes; acquire the conversion lock described below.
- `IQuoteViewRepository` / `QuoteViewRepository`: no-tracking detail projection and stored-procedure-backed paged search, including pricing and the optional conversion link.
- `ISalesOrderRepository` / `SalesOrderRepository`: allocate number; load a nondeleted aggregate with lines for update; find by source quote; stage root, history, and removed children.
- `ISalesOrderViewRepository` / `SalesOrderViewRepository`: no-tracking detail and stored-procedure-backed paged search with pricing/source link.
- `ISalesReferenceRepository` / `SalesReferenceRepository`: bounded batch reads for exactly the customer, address, products, and carrier needed by a workflow. It returns immutable application projections with active/usage and snapshot fields, not tracked master-data entities.
- `IWorkflowAuditRepository` should be the promoted shared append-only audit repository; do not create a second audit table or generic audit update/delete behavior.
- `ISalesPersistenceErrorClassifier` / `SalesPersistenceErrorClassifier`: recognize EF concurrency plus only the reviewed number, aggregate-line product, source-quote singleton, and workflow FK/check constraints. Unexpected provider errors remain unexpected.
- `IDashboardRepository` / `DashboardRepository`: execute the single dashboard routine and return its one row; no five public repository calls and no in-memory counting.

All custom methods accept and propagate `CancellationToken` where the installed API supports it. Repositories contain EF/SQL/query mechanics only; they do not decide whether a business transition is allowed. No public method returns `IQueryable`, `DbContext`, `DbConnection`, or a SQL/EF exception.

### Searches and detail reads

- Add `dbo.SearchQuote` and `dbo.SearchSalesOrder` under `src/database/routines/Sales`, one object per file. They return pagination metadata first and rows second, matching Paradigm SQL Server multi-result tuple order; add generated parameter/result mappers through the existing CodeGenerator path.
- Search Quote number plus customer account/name snapshots; search Order number, tracking number, and customer account/name snapshots. Exclude tombstoned roots before count and paging. Apply all filters in SQL.
- Implement sort through a closed SQL allow-list, never interpolated identifiers. Default Quote to `quoteNumber desc` and Order to `orderNumber desc`; always add `Id` as the stable final sort. Count before paging and return exact `ItemsCount`, `PageNumber`, `PageSize`, and `TotalPages`.
- Detail is a bounded exact-ID query. Project the header/pricing/link and lines without exposing tracking; use a split bounded query or focused stored routine so line joins cannot duplicate totals. Materialize all components before returning and encode rowversion only after materialization.
- Audit and status-history rows are never joined into collection search results.

### Sequence allocation and persistence

- Allocate numbers from `NEXT VALUE FOR dbo.QuoteNumberSequence` and `dbo.SalesOrderNumberSequence` through repository methods on the context-owned connection. Enlist commands through the Unit of Work where required and never dispose that connection.
- Use a checked eight-digit formatter and fail explicitly if a sequence value exceeds the 20-character contract; do not wrap or reuse numbers.
- Persist an initial Draft status-history row in the same explicit database transaction as each Quote/Order root and its lines. Persist every later accepted transition as one new row beside the status update. Expose no history mutation or deletion path.

### Concurrent quote conversion

The existing filtered unique index `UQ_SalesOrder_SourceQuoteId` is the final exactly-one guard. The normal Provider path must also serialize work cleanly:

1. Resolve Quote, Order, reference, history, and audit repositories before opening the Unit of Work transaction.
2. Begin one explicit same-context transaction. Acquire `UPDLOCK, HOLDLOCK` on the nondeleted Quote row by ID through the Quote repository, then load its lines and existing order relation inside that transaction.
3. If an order already exists, commit/read it and return `Created=false` without validating the replay token or writing another history/audit row.
4. Otherwise validate the expected quote rowversion and Accepted status, allocate the order number, create the order by copying source snapshots and line pricing inputs, stage its initial Draft history, plus `salesOrder/created` and `quote/converted` audit facts, save, and commit once.
5. If a separately written caller defeats the cooperative lock and the unique source-quote constraint wins, roll back, clear tracked state, load the winning order, and return it as `Created=false`. Translate unrelated unique failures normally.

This workflow neither mutates the accepted Quote nor uses the HTTP idempotency ledger. The database FK and unique index keep `SourceQuoteId` singular; Domain requests expose no way to change it after insertion.

### Dashboard consistency

- Add `dbo.GetDashboardSummary` under `src/database/routines/Reporting`. The routine owns a short `SERIALIZABLE` transaction with `XACT_ABORT ON`, captures `SYSUTCDATETIME()` once, calculates all five `COUNT_BIG` values, returns one row, and commits/rolls back safely. Keep lock acquisition order documented and identical in tests to minimize deadlocks.
- Counts are: all Product rows, all Customer rows, all Carrier rows, nondeleted Quotes with status Draft or Sent, and nondeleted Sales Orders whose status is neither Completed nor Cancelled. Inactive master rows are included.
- The Reporting Provider makes exactly one repository call and performs no recalculation. Do not use five list endpoints, parallel queries, cache state, or client-side aggregation.
- Review the live execution plan. Add a filtered/deletion-aware index only if the plan shows an actual scan problem; if added, declare it in database source and regenerate persistence only when mappings change.

## Provider workflows and transaction ownership

Create public sealed exact-name `QuoteProvider`, `SalesOrderProvider`, `QuoteConversionProvider`, and `DashboardProvider` implementations with matching `IProvider` interfaces. Constructor-inject stable collaborators, including all participating repositories, `IUnitOfWork`, operation context, `TimeProvider`, persistence error classifier/session, and no service locator.

For every mutation:

1. Normalize and validate request syntax before tracking changes.
2. Open an explicit Unit of Work transaction only after all repositories have been resolved.
3. Load/lock roots, validate expected version, resolve references in bounded queries, and invoke aggregate behavior.
4. Stage root/children/history and safe audit facts. Audit actions are `created`, `updated`, `deleted`, `statusTransition`, and `converted`; status transitions populate `PreviousStatusCode` and `NewStatusCode` columns. Metadata may contain changed field names and source IDs, never customer/address/contact/snapshot values.
5. Call `CommitChangesAsync()` as needed to obtain generated IDs, but commit the database transaction exactly once. Quote/Order creation may use two saves—root/lines/history to obtain ID, then audit—inside the same transaction.
6. On failure, roll back if active, clear tracked state, preserve cancellation, translate only reviewed conflicts, and rethrow unexpected failures. Never retry a non-idempotent mutation blindly.
7. Reload the no-tracking DTO after commit so database-computed line and aggregate totals plus the new rowversion are authoritative.

Provider method sets are:

- Quote: `SearchAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, and `TransitionAsync`.
- Sales Order: `SearchAsync`, `GetByIdAsync`, `CreateDirectAsync`, `UpdateAsync`, `DeleteAsync`, and `TransitionAsync`.
- Quote conversion: `ConvertAsync` returning `QuoteConversionResult`.
- Dashboard: `GetSummaryAsync`.

Search/detail/dashboard methods are read-only and do not open application transactions except that the dashboard SQL routine owns its consistency transaction. Generic edit Provider bases are not used because these workflows require separate commands, aggregate children, histories, concurrency, audit, and multi-step transactions.

## Test plan

### Domain tests

Extend `BeaconAr.Domain.Tests` with direct, database-free tests covering:

- every date/notes/line boundary; decimal scale and midpoint-away-from-zero parity cases; zero and large values; duplicate products; empty lines; invalid enum values; and validation-path accuracy;
- authoritative snapshot assignment and proof that request types cannot supply derived/snapshot/identity/status fields;
- Quote creation, atomic draft replacement, unchanged versus changed inactive-product lines, customer/address replacement, snapshot preservation, tombstone eligibility, and a failed replacement leaving the original aggregate/children unchanged;
- all four legal Quote transition edges and every skipped, repeated, terminal, deleted, and edit-after-draft rejection;
- direct Order creation/replacement, immutable null source quote, converted-order snapshot copying, immutable non-null source quote, carrier/tracking normalization, and snapshot completeness at confirmation;
- every legal Order transition edge, cancellation from each allowed state, shipping with supplied or existing carrier/tracking, and every skipped/repeated/terminal/invalid-payload rejection;
- number formatting boundaries and canonical version decoding.

### Provider tests

Extend `BeaconAr.Providers.Tests` with focused handwritten fakes and no new mocking dependency. Assert:

- validation occurs before repository mutation; exact bounded reference batches are requested; snapshots come only from repository projections; inactive/reselected behavior matches Domain rules; and all cancellation tokens are forwarded;
- success/not-found/stale-version/reference-inactive/invalid-transition paths for every Provider method;
- root modification stamps force concurrency advancement for line-only edits;
- initial histories, transition histories, actor/time, audit actions, previous/new status values, safe metadata, save counts, and one transaction commit;
- rollback and tracker clearing on every reference, staging, first-save, second-save, and audit failure; unexpected errors are not converted to business conflicts;
- quote conversion returns `Created=true` once, returns the pre-existing order with no writes for replay, copies exact source values, resolves all repositories before transaction creation, and recovers a reviewed unique-index race as `Created=false`;
- Dashboard Provider makes one call and returns repository values unchanged.

### Exhaustive live SQL Server tests

Extend `BeaconAr.Database.IntegrationTests` against a freshly published disposable database. Each concurrent actor gets an independent DI scope/context/connection. Do not replace SQL Server with EF InMemory or SQLite for these scenarios.

#### Quote live matrix

- Create a valid Quote and prove generated number format/uniqueness, Draft root, initial history, snapshots, line computed values, pricing-view totals, audit actor/time/correlation, and returned version.
- Exercise half-cent rounding, multiple lines, 0%/100% discounts, maximum supported decimals, duplicate product IDs, FK/check/length failures, inactive customer, wrong-customer address, non-shipping address, and inactive new product.
- Replace header and lines; add/update/remove lines; prove omitted children are removed while history remains; prove unchanged inactive lines survive and changed/reselected inactive lines fail atomically.
- Cover every search column, filter combination, both directions for every allowed sort, identical-value ID tie-breaks, first/middle/empty/out-of-range pages, tombstone exclusion, and exact paging metadata.
- Run every legal transition on fresh fixtures. For every state, attempt every illegal target, repeat a prior target, and prove current state, rowversion, history count, and audit count do not change on rejection.
- Tombstone Draft and reject tombstone after every non-Draft status. Prove detail/search absence while root, lines, initial history, snapshots, and audit remain in SQL.
- Run two independent writers from the same rowversion for header-only, line-only, transition, and delete races. Exactly one wins; the stale actor receives `concurrency_conflict`; no newer header/line/status/history/audit is overwritten.

#### Sales Order live matrix

- Create a direct Draft and prove `SourceQuoteId` is null, number uniqueness, snapshots, pricing, initial history/audit, and version. Exercise all line/reference/date/carrier/tracking validation and database constraints.
- Replace direct Draft header/lines and carrier/tracking; prove inactive newly assigned carrier fails while an unchanged historical inactive carrier remains readable.
- Convert an Accepted Quote and prove exact customer/address/product snapshot and pricing-input copies, Draft history, both audit facts, immutable source relationship, and preservation after all source master records are edited/deactivated.
- Call conversion sequentially twice and assert the same order ID/number with one order, one order history, and no duplicate audit. Launch at least two simultaneous conversions behind a barrier in separate scopes; both successful callers observe the same order and SQL contains exactly one source-linked order.
- Attempt conversion from Draft, Sent, Rejected, Expired, and tombstoned Quotes and prove no order/history/audit is created. Race acceptance versus conversion and prove a serializable outcome: conversion succeeds only if it observes Accepted, otherwise it returns the documented conflict without partial work.
- Run every legal fulfillment edge on fresh fixtures, including cancellation from Draft/Confirmed/Processing. For every state, attempt every illegal target and repeated target. Verify no rejected call changes carrier/tracking/status/version/history/audit.
- Ship with carrier/tracking supplied atomically; reject missing carrier, inactive carrier, blank tracking, and non-shipping payload use. Prove Completed/Cancelled are read-only.
- Cover every order search column/filter combination—including nullable source quote—both directions for every allowed sort, null requested-ship-date ordering, stable tie-breaks, all page boundaries, and tombstone exclusion.
- Tombstone direct and converted Draft orders, then reject deletion after every non-Draft state. Verify preserved rows/lines/histories and reference protection. If product requirements intend converted draft deletion to remain eligible, preserve the Quote conversion association so reconversion returns that same tombstoned order rather than creating another; ordinary order reads remain absent. Document this behavior for the API task.
- Run stale direct-update, line-only update, transition, shipping, and delete races; assert one winner, one conflict, no lost update, and exactly one accepted history/audit fact.

#### Transaction, audit, and dashboard live matrix

- Inject a failure after root/line/history staging, after the first save, and after audit staging for Quote create, Order create, transition, tombstone, and conversion. Assert complete rollback, including sequence-gap acceptance but no partial business/history/audit rows.
- Query status histories directly to prove one immutable initial row plus one row per accepted transition, correct actor/time/status order, non-cascading preservation, and no application repository capable of update/delete.
- Verify audit rows for every mutation and transition, correct resource IDs/correlation IDs/previous-new status, no personal/snapshot data in metadata, and no audit row after a failed transaction.
- Seed known active/inactive master data and live/tombstoned transactions in each state; verify all five Dashboard counts and one UTC `AsOf`. Modify each contributing table between calls and verify exact deltas.
- Hold coordinated writer transactions across multiple counted tables while requesting the Dashboard summary. Prove the routine blocks or observes a serializable before/after state, never a mixed aggregate; assert one database command/result and no client-side enumeration.
- Review actual Quote search, Order search, and Dashboard execution plans for database paging and bounded index use; record regressions without adding speculative indexes.

### Architecture, generation, and database-object tests

- Extend architecture tests for inward dependencies, no ASP.NET types outside WebApi, no Provider use of EF/SQL, no generated-file edits, exact-name one-interface repository/provider discovery, one aggregate repository per root, no standalone line/history mutation repository, and no persistence entity crossing a Provider contract.
- Verify status seed-to-enum numeric/code parity, history FKs remain `NO ACTION`, source-quote uniqueness remains filtered/non-cascading, sequence definitions remain noncycling, searches return metadata before rows with deterministic ordering, and the dashboard routine contains its explicit consistency transaction/error path.
- Run persistence regeneration only after the database builds; require a clean second regeneration. Generate stored-procedure mappers through the configured generator and never hand-edit generated mapper output.

## Implementation sequence

1. Promote shared operation/audit/version/persistence contracts out of the MasterData namespace and keep all existing master-data tests green.
2. Add Sales and Reporting command/read/error contracts plus validation and domain aggregate behavior; complete Domain tests first.
3. Add Quote/Order search and Dashboard SQL routines, include post-build mapper generation, validate/build the database, then regenerate persistence deterministically.
4. Add Sales edit/view/reference/audit/dashboard repository contracts and exact-name Data implementations, including sequence access, line reconciliation, detail projections, and conversion locking.
5. Add Quote, Sales Order, conversion, and Dashboard Providers with explicit transaction, history, audit, concurrency, and conflict behavior.
6. Complete Provider tests, then the full live SQL matrix with isolated data and coordinated race barriers.
7. Extend architecture/database checks, run all master-data regression suites, and verify generation produces no diff.

## Verification

Run from `examples/beacon-ar`:

```powershell
dotnet restore BeaconAr.slnx --property:RestoreAdditionalProjectSources=../../artifacts
dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release
dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution BeaconAr.slnx --strict --format json
./build/regenerate-persistence.ps1
git diff --exit-code -- src/BeaconAr.Domain/Receivables/Generated src/BeaconAr.Data/Receivables/Generated
dotnet build BeaconAr.slnx --configuration Release --no-restore
dotnet test --solution BeaconAr.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 1
dotnet tool run paradigm validate --project BeaconAr.slnx
dotnet tool run paradigm checks run --project BeaconAr.slnx
```

Run the SQL integration suite with `ConnectionStrings__DatabaseConnection` targeting a freshly published disposable SQL Server database. If live SQL Server is unavailable, list every skipped live scenario explicitly; do not claim pricing translation, stored-procedure paging, rowversion races, lock behavior, transaction rollback, conversion singleton guarantees, or Dashboard consistency passed.

## Acceptance checklist

- Quote and Sales Order application contracts support complete create/read/search/draft-replace/tombstone workflows without exposing HTTP or persistence types.
- All snapshots are server-owned, historical values do not drift with master data, and inactive references follow the documented unchanged-versus-new rules.
- SQL Server computed values and pricing views are authoritative and match the published rounding policy at boundary cases.
- Searches filter/sort/page in SQL, exclude tombstones, have deterministic tie-breaks, and return exact metadata.
- Both state machines implement every allowed edge and reject every other edge atomically with stable application codes.
- Every root starts with one history row; every accepted transition adds exactly one append-only history and one audited previous/new status fact in the same transaction.
- Draft line-only writes advance root concurrency; stale update, transition, shipping, and delete calls never overwrite a newer mutation.
- Direct orders never accept a source quote from callers. Accepted-quote conversion copies exact source snapshots/pricing, returns the existing singleton on retries, and creates exactly one order under concurrency.
- Dashboard returns all five database-side counts and one UTC timestamp from one serializable database operation.
- No controller, policy, authentication, ETag parser, Problem Details middleware, or route is implemented in this task.
- Domain, Provider, architecture, database validation/build, deterministic regeneration, all master-data regressions, and exhaustive live SQL suites pass.
