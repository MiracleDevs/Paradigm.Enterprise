# Beacon AR master-data implementation plan

## Objective

Implement the release-one Product, Customer, Customer Address, and Carrier application capabilities through `BeaconAr.Domain`, `BeaconAr.Data`, and `BeaconAr.Providers`. Deliver database-backed CRUD, deterministic server-side search/filter/sort/paging, domain validation, audit and optimistic-concurrency semantics, case-insensitive uniqueness, atomic address-default changes, and reference-protected deletion with service and database tests.

This task deliberately stops at the application boundary. The later `beacon-ar-api-security-contract` task owns authenticated `/api/v1` controllers, policies, ETags/`If-Match` parsing, Problem Details middleware, JSON serializer registration, and endpoint exposure. The public DTOs and Provider contracts created here must nevertheless be directly usable by that task and by OpenAPI/NSwag without exposing EF entities.

## Inspected foundation and framework APIs

- The branch starts from merged commit `78e10ea`, and the SQL Server DACPAC plus generated persistence boundary are already present.
- `ReceivablesDbContext : DbContextBase<int>` maps generated `Product`, `Customer`, `CustomerAddress`, and `Carrier` entities. Each has the audit quartet and an EF concurrency-token `byte[] RowVersion`.
- SQL already supplies case-insensitive unique constraints for `Product.Sku`, `Customer.AccountNumber`, and `Carrier.Code` using `Latin1_General_100_CI_AS_SC`; all business-history foreign keys use `NO ACTION`; filtered unique indexes enforce one billing and one shipping default per customer.
- The generated entity and context files are replaceable. Handwritten behavior belongs in separate partial files; generated files and the owning T4 templates need no change for this task.
- Paradigm 1.1.0 supplies `RepositoryBase<TContext,TId>`, whose protected members include `EntityContext`, `UnitOfWork`, and `GetDbConnection()`. Repository construction registers its context with the scoped Unit of Work.
- `IUnitOfWork.CreateTransaction()` requires at least one already-resolved repository, `CommitChangesAsync()` saves registered participants but does not commit the transaction, and `ITransaction.Commit()`/`Rollback()` are synchronous.
- `EditProviderBase<...>` automatically maps a mutable view and commits generic writes. It is not the right boundary here: Beacon needs separate create/update commands, field-addressable validation, expected-version input, mandatory audit records in the same transaction, cancellation, and conditional deletes.
- Framework `PaginatedResultDto<T>` serializes `pageInfo/results` and its `PaginationInfo` omits `pageSize`; it cannot represent the required `items/pageNumber/pageSize/totalPages/itemsCount` contract. Use an application-owned page result.
- Public protected controllers must later derive from ASP.NET Core `ControllerBase`; Paradigm controller bases carry `AllowAnonymous`. No controller or authorization code is added in this task.

## Scope

### Included

- Handwritten master-data behavior and validation over the generated persistence partials.
- Separate write commands, read DTOs, search inputs, paging output, version encoding, and application error contracts.
- Exact-name edit/read repository contracts and implementations for all four resources, plus audit persistence and persistence-error classification.
- Custom, public, sealed, exact-name Providers implementing `IProvider` directly.
- Cancellation-aware EF reads and writes, database-side paging, and deterministic allow-listed ordering.
- Explicit same-database transactions for every mutation and for every implicit address-default change.
- Domain, Provider/service, architecture, and SQL Server integration coverage.

### Deferred

- HTTP routes, status codes, `Location`, `If-Match`, idempotency headers, JWT bearer authentication, `business.read`/`business.write`, CORS, and Problem Details rendering.
- Quote/order rules that consume active master data and immutable snapshots. This task exposes repository/application contracts those workflows can reuse but does not implement transaction workflows.
- Address archival, since the release-one contract has no address active flag.
- New production seed data. Tests create isolated fixtures.

## Contract design

Place transport-neutral contracts under `BeaconAr.Domain/MasterData/Contracts`; they must not reference ASP.NET Core, EF Core, or SQL Server.

1. Add `PageResult<T>` with `IReadOnlyList<T> Items`, `PageNumber`, `PageSize`, `TotalPages`, and `ItemsCount`. Return an empty list and `TotalPages = 0` when no rows match.
2. Add concrete `ProductSearchRequest`, `CustomerSearchRequest`, `AddressSearchRequest`, and `CarrierSearchRequest` types. Each exposes `Search`, `PageNumber = 1`, `PageSize = 10`, `SortField`, and `SortDirection`; resource filters are `bool? Active`, or `int? CustomerId`, `string? Type`, and `AddressUsage? Usage` for addresses.
3. Accept page sizes `1..100`; document `10`, `20`, and `50` as the UI choices. Trim search text. Reject nonpositive pages, oversized pages, invalid directions, unknown sort fields, invalid address usage, and nonpositive filter IDs before calling Data.
4. Add string-valued `SortDirection` and `AddressUsage` enums suitable for later camel-case JSON configuration. Keep address `Type` as a string on read DTOs so an unknown catalog code remains displayable; writes accept only `billing`, `shipping`, or `both`.
5. Add purpose-built `ProductCreateRequest`/`ProductUpdateRequest`, `CustomerCreateRequest`/`CustomerUpdateRequest`, `AddressCreateRequest`/`AddressUpdateRequest`, and `CarrierCreateRequest`/`CarrierUpdateRequest`. Body contracts contain no ID, generated audit field, or row version.
6. Add `ProductDto`, `CustomerDto`, `AddressDto`, and `CarrierDto`. Include all resource fields plus `createdByUserId`, `creationDate`, `modifiedByUserId`, `modificationDate`, and a canonical Base64 `version` string. Never expose navigation collections or raw `byte[]` row versions.
7. Provider update and delete methods take path-authoritative `int id` and a separate canonical expected-version string. The future HTTP layer will obtain it from `If-Match`; no HTTP header type leaks inward.
8. Add application exceptions carrying stable codes and safe text: validation errors keyed by request paths, `not_found`, `duplicate_key`, `referenced_record`, `referenced_address`, and `concurrency_conflict`. Preserve the underlying exception for logs, but never place SQL text or personal values in the safe contract.
9. Add an operation-context interface exposing the required local `UserId` and `CorrelationId`. Providers also inject `TimeProvider`. The later identity/API task supplies the request-scoped adapter; tests use deterministic fakes.

## Domain behavior and validation

Add handwritten partials alongside, never inside, `Receivables/Generated` output. Public setters remain the documented database-first persistence exception; callers mutate through the following behavior methods.

### Shared rules

- Normalize all required text with `Trim()` and optional blank text to `null` before mutating a tracked entity. Uppercase the two-letter country code. Preserve user-selected casing for SKU, account number, and carrier code while relying on the database collation for case-insensitive comparison.
- Prevalidate a complete proposed state before applying it so a validation failure cannot leave an EF-tracked entity dirty. Domain behavior receives already-normalized values, uses `DomainValidator` to collect intrinsic failures, and mutates only after validation succeeds.
- Add shared safe-URL validation. Product thumbnails require an absolute `http` or `https` URI with a host, no user information, no control characters, and no template braces. Carrier templates require absolute `https`, the same safety rules, and may contain only the literal optional `{trackingNumber}` placeholder; validate by replacing that token with a safe sentinel. The server never dereferences either URL.
- Add a version codec that accepts only canonical Base64 representing the SQL Server eight-byte rowversion. Invalid version syntax is validation failure; a well-formed nonmatching token is `concurrency_conflict`.

### Product

- `Create`/`Replace` and `Activate`/`Deactivate` behavior enforce SKU length `2..32`, name length `2..120`, nonblank category up to the schema limit of 120, `unitPrice > 0`, `stockQuantity >= 0`, and optional safe thumbnail URL up to 2048 characters.
- `IsActive` is editable through replacement; inactive rows remain visible to ID and unfiltered list reads.

### Customer

- Enforce nonblank account number up to 50, name up to 120, syntactically valid email up to 320, optional phone up to 50, nonnegative `decimal(19,2)` credit limit, and payment terms exactly `0`, `15`, `30`, `45`, or `60`.
- Keep email validation deliberately syntactic and deterministic; do not perform DNS or remote verification.

### Customer address

- Enforce a positive existing customer ID; exact write type `billing`, `shipping`, or `both`; label/city up to 120; line1/line2 up to 200; postal code up to 32; optional state up to 120; and an uppercase two-letter country matching the existing database constraint.
- A billing default requires type `billing` or `both`; a shipping default requires `shipping` or `both`.
- Permit re-parenting only when the address is unreferenced. A referenced address cannot change customer because doing so would invalidate historical ownership; return `referenced_address` before mutation.

### Carrier

- Enforce nonblank code up to 32, name and service level up to 120, and an optional safe HTTPS tracking template up to 2048.
- `IsActive` is editable; historical reads continue to return inactive carriers.

## Data layer

### Repository contracts and discovery

Define application repository interfaces in Domain inheriting `IRepository`, then create one public concrete exact-name implementation per interface in Data. Use `RepositoryBase<ReceivablesDbContext, int>` rather than the generic read/edit bases because the required operations use DTO projections, expected versions, cancellation, reference queries, and explicit staging.

- `IProductRepository` / `ProductRepository`: tracked load, stage add/delete, case-insensitive SKU existence excluding an ID, and quote/order-line reference check.
- `IProductViewRepository` / `ProductViewRepository`: no-tracking detail projection and paged search.
- Equivalent edit/view pairs for Customer, CustomerAddress, and Carrier.
- `IAuditLogRepository` / `AuditLogRepository`: stage append-only audit facts. It never updates or deletes an audit row.
- `IMasterDataPersistenceErrorClassifier` / `MasterDataPersistenceErrorClassifier`: recognize only reviewed SQL Server unique/FK constraint failures and EF concurrency failures, returning an application-neutral conflict kind. Unknown failures are rethrown unchanged.

All repository methods that are not inherited framework members accept and pass a `CancellationToken`. Never expose `DbContext`, `DbConnection`, `IQueryable`, EF entities, or provider exceptions from a public repository contract.

### Reads and searches

- Implement the four searches as bounded EF Core projections. These are simple single-root queries (addresses join the small address-type catalog), with static predicates and a switch over the allow-list; EF provides predictable translation and cancellation without dynamic SQL.
- Perform `CountAsync` after search/filter and before paging. Apply the selected primary order and always `ThenBy(Id)` unless ID is already the unique primary sort. Apply `Skip((pageNumber - 1) * pageSize)` and `Take(pageSize)` in SQL, then project directly to DTOs with Base64 conversion performed after materialization.
- Search columns are: SKU/name/category; account number/name/email/phone; address label and all address lines/city/state/postal code/country; carrier code/name/service level. SQL Server's configured case-insensitive collation supplies case-insensitive matching.
- Address `type` matches the exact catalog code. `usage=shipping` includes `shipping` and `both`; `usage=billing` includes `billing` and `both`. `customerId` remains optional for administration but is supported efficiently for editor lookups.
- Allowed sorts are exactly Product `id|sku|name`, Customer `id|name`, Address `id|name` (`name` orders by label), and Carrier `id|name`. Default to `id asc`.

### Writes, defaults, and database races

- Track edit entities only inside mutation methods. Read DTO queries use `AsNoTracking`.
- Compare the decoded expected version to the loaded rowversion before mutation, then allow EF's rowversion predicate to catch a concurrent change between load and save.
- Duplicate prechecks use the same SQL collation and exclude the current ID. The existing unique constraints remain authoritative under races; classify constraints `UQ_Product_Sku`, `UQ_Customer_AccountNumber`, and `UQ_Carrier_Code` as `duplicate_key`.
- Reference prechecks cover Product -> QuoteLine/SalesOrderLine, Customer -> CustomerAddress/Quote/SalesOrder, CustomerAddress -> Quote/SalesOrder, and Carrier -> SalesOrder. Existing `NO ACTION` FKs remain the race-safe authority; classify only those named FKs to the resource's documented reference code.
- Add a CustomerAddress repository operation that, inside the active Unit of Work transaction, locks affected customer rows in ascending ID order with parameterized SQL Server `UPDLOCK, HOLDLOCK`. It then loads current default rows for update. Do not dispose the context-owned connection; enlist commands through `UnitOfWork.UseTransaction(command)`.
- When a new billing/shipping default is requested, clear that flag on the previous default and stamp its modification audit fields. Moving an unreferenced address locks both old and new customers, clears its old defaults, and applies the requested defaults in the destination. The filtered unique indexes remain the final invariant.
- Add indexes only if live query plans demonstrate a missing bounded-search path; do not alter the generated model or duplicate foundation constraints speculatively.

## Provider workflows and transaction ownership

Create `IProductProvider`, `ICustomerProvider`, `IAddressProvider`, and `ICarrierProvider`, each inheriting `IProvider`. Implement them as public sealed exact-name classes with constructor-injected repositories, `IUnitOfWork`, operation context, and `TimeProvider`. Methods are `SearchAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, and `DeleteAsync`, all with `CancellationToken`.

Every mutation resolves all participating repositories through constructor injection before calling `CreateTransaction()`. Use this template:

1. Validate and normalize request/search input before changing tracked state.
2. Open the Unit of Work transaction; load/check required records and expected version.
3. Run uniqueness/reference checks and domain behavior.
4. Stamp creation/modification fields from the operation context and `TimeProvider`.
5. Stage entity/default changes and audit facts, call `CommitChangesAsync()`, and commit the transaction.
6. For create, save the new entity inside the still-active transaction to obtain its ID, then stage its audit row, save again, and commit. Both saves remain atomic.
7. Roll back when active on any failure. Translate only recognized duplicate/reference/concurrency failures; rethrow cancellation and unexpected failures.
8. Reload and return the no-tracking DTO after commit.

Audit actions are `created`, `updated`, `activated`, `deactivated`, and `deleted`. An update that changes `IsActive` uses the activation-specific action; otherwise it uses `updated`. Each implicitly cleared address default gets its own `defaultBillingCleared` and/or `defaultShippingCleared` audit fact because it is a separate resource mutation. Audit metadata may name changed fields but must not contain email, phone, street address, URL, or other personal/business values.

Address create/update additionally opens the transaction before acquiring the customer lock, verifies the referenced customer and active address-type catalog entry, applies all default changes, and writes all resource/audit changes before one transaction commit. A failure at any point must leave both the target and previous defaults unchanged.

Delete is not idempotent at the Provider boundary: a missing resource returns `not_found`. It requires the expected version, performs the reference precheck, hard-deletes only an unreferenced row, and records the audit fact in the same transaction. There is no cascade and no fallback soft delete.

## Test plan

### Domain tests

Extend `BeaconAr.Domain.Tests` with direct tests for normalization, every boundary, all allowed payment terms and address types, default/type compatibility, activation changes, email syntax, Base64 rowversion parsing, safe product URLs, safe carrier templates, rejected schemes/user info/control characters/placeholders, and atomic prevalidation (a failed replacement leaves the original object unchanged).

### Provider/service tests

Add `BeaconAr.Providers.Tests` using focused handwritten fakes rather than a new mocking dependency. Cover each Provider's success paths and assert:

- correct repository calls, DTO mapping, cancellation forwarding, and no unbounded read path;
- not-found, invalid expected version, stale version, duplicate, and referenced-delete application errors;
- actor/time stamps and safe audit action/metadata;
- one transaction commit on update/delete, two Unit of Work saves but one transaction commit on create, rollback on every collaborator/save failure, and no second commit after failure;
- address default clear/set ordering, audit for implicit changes, deterministic old/new customer lock order, and rollback preserving the prior default;
- unexpected storage exceptions remain unexpected and do not become safe business conflicts.

### SQL Server integration tests

Extend `BeaconAr.Database.IntegrationTests` and create fresh DI scopes per concurrent actor. Against a published disposable database, prove:

- every search column/filter/sort direction, ID tie-breaker, page boundary, empty page, and exact `itemsCount/totalPages/pageSize` metadata;
- case-insensitive duplicate SKU/account/code rejection, including concurrent creates, while casing is preserved on read;
- stale update and stale delete raise concurrency conflict and do not overwrite/delete newer state;
- Product, Customer, Address, and Carrier deletes succeed when unreferenced and fail with the documented code for every quote/order FK path without deleting history;
- two defaults of each kind cannot coexist; changing defaults clears the prior row atomically; concurrent changes serialize and finish with exactly one default; injected failure rolls the complete transaction back;
- create/update/delete audit rows share the actor, timestamp, resource ID, action, and correlation ID, and a forced failure leaves neither resource nor audit changes;
- inactive rows remain readable and active filters return only eligible lookup rows; unknown existing address type codes can be read while unsupported codes are rejected on writes.

### Architecture and discovery tests

Extend `BeaconAr.Architecture.Tests` to require inward project references, no ASP.NET types outside WebApi, no hand edits under generated folders, public concrete exact-name repository/provider pairs, exactly one application repository interface per repository, Provider independence from controllers, no `IQueryable`/EF/SQL types in contracts, and no public persistence entity accepted or returned by a Provider.

## Implementation sequence

1. Add application paging, search, request/response, validation-error, conflict, operation-context, and version-token contracts in Domain.
2. Add and unit-test generated-entity partial behavior and shared normalization/URL validators.
3. Add edit/view/audit repository contracts, concrete `RepositoryBase<ReceivablesDbContext,int>` implementations, projections, bounded searches, conflict classification, and customer default locking.
4. Add the four custom Provider interfaces and implementations with explicit mutation transactions and audit staging.
5. Add the Provider test project to root `BeaconAr.slnx`; implement Domain and service tests.
6. Add SQL integration fixtures for searches, duplicates, references, concurrency, default-address atomicity, and audit rollback.
7. Extend architecture/discovery checks and verify generation still produces no diff.

## Verification

Run from `examples/beacon-ar`:

```powershell
dotnet restore BeaconAr.slnx --property:RestoreAdditionalProjectSources=../../artifacts
dotnet build BeaconAr.slnx --configuration Release --no-restore
dotnet test --solution BeaconAr.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 1
dotnet tool run paradigm validate --project BeaconAr.slnx
dotnet tool run paradigm checks run --project BeaconAr.slnx
dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution BeaconAr.slnx --strict --format json
dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release
```

Run SQL integration tests with `ConnectionStrings__DatabaseConnection` set to a disposable published SQL Server database. Run the existing persistence-regeneration script and require a clean diff. If SQL Server is unavailable, report the exact skipped integration scenarios; do not claim uniqueness races, FK translation, rowversion conflicts, default serialization, or audit rollback passed.

## Acceptance checklist

- All four master-data resources support application-level list/search/get/create/replace/delete behavior without a public controller.
- Search/filter/sort/page work is executed by SQL Server with validated allow-lists, a maximum page size of 100, exact metadata, and deterministic ID tie-breaking.
- Product SKU, customer account number, and carrier code are case-insensitively unique in prechecks and under database races.
- Validation is server-owned, field-addressable, and aligned with both the functional requirements and existing schema limits.
- Product and carrier URLs accept only the documented safe forms and are never fetched.
- Each mutable response carries audit metadata and a canonical version; stale updates/deletes cannot overwrite newer data.
- Each mutation and each implicit address-default mutation is audited with actor, UTC time, resource/action, and correlation ID in the same transaction.
- Address default changes are serialized and atomic, with at most one default of each kind per customer.
- Referenced master data is preserved with stable conflict codes and no cascade; unreferenced data is physically deleted.
- Public contracts expose no EF entities or infrastructure types and are ready for the later controller/OpenAPI task.
- Domain, Provider, architecture, and live SQL integration suites pass, and generated persistence output remains unchanged.
