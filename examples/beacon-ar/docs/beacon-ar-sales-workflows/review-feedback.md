# Beacon AR sales-workflows final re-review feedback

## Outcome

Approved. The final implementation resolves every blocking and non-blocking finding from the initial review. The aggregate-pricing boundary, immutable transaction snapshots, reference-eligibility races, multi-statement read consistency, workflow integration coverage, and shared rowversion codec were re-reviewed in source and exercised against a freshly published disposable SQL Server database. No implementation fix was made during this re-review.

## Final finding status

No blocking or non-blocking implementation finding remains.

### Resolved - aggregate pricing is rejected before persistence

- `src/BeaconAr.Domain/Sales/Validation/SalesDomainValidation.cs` computes subtotal, discount, and total with the same independently rounded line values used by the SQL views, checks decimal arithmetic, and rejects values outside the `DECIMAL(19,2)` storage contract before repository staging.
- The validation is shared by quote create/update, direct-order create/update, and quote-to-order conversion.
- The live aggregate-boundary test proves the request is rejected and leaves no root, line, history, or audit rows. The interrupted two-save create test also proves the transaction rolls back completely.

### Resolved - retained product snapshots remain immutable

- The line builders now preserve the existing SKU and product-name snapshots for every retained product ID, regardless of the product's current activity or catalog text.
- Catalog text is copied only when a product is newly introduced to the transaction.
- Live quote replacement and quote-to-order conversion coverage proves later catalog edits do not rewrite captured snapshots.

### Resolved - reference eligibility is protected through commit

- `src/BeaconAr.Data/Sales/SalesReferenceRepository.cs` reads customer, address, product, and carrier eligibility with transaction-enlisted `UPDLOCK, HOLDLOCK` semantics.
- Product IDs are distinct and sorted before locking, which gives concurrent sales mutations a deterministic lock order.
- The Providers open the unit-of-work transaction before reference resolution and retain it through persistence and commit.
- Independent-scope live races prove customer, address, product, and carrier master-data changes block while the sales mutation owns the eligibility lock, then proceed after its commit.

### Resolved - detail and paged reads use one consistent database state

- Quote and Sales Order detail repositories wrap their header/pricing/version and line queries in one serializable transaction when they do not inherit an existing transaction.
- `Sales.SearchQuote` and `Sales.SearchSalesOrder` wrap count and page selection in explicit serializable, `XACT_ABORT ON` transactions with rollback handling.
- A live session probe confirmed a search routine returns the connection to read-committed isolation, so its stronger boundary does not leak into later work on the pooled session.

### Resolved - workflow integration coverage is materially complete

- The SQL-backed suite now covers aggregate rejection and rollback, retained snapshots and conversion, quote terminal states, all allowed order-cancellation origins, invalid/repeated transitions, draft-only deletion protection, concurrent conversion, search/tombstone/dashboard behavior, and reference-eligibility races.
- All 21 database integration tests passed against the freshly published database with no skips or failures.

### Resolved - Sales delegates the shared rowversion codec

- `SalesRequestValidator` retains Sales-specific field-addressable error translation while delegating canonical Base64 encoding and decoding to `BeaconAr.Domain.Operations.VersionTokenCodec`.

## Verified strengths

- Quote and Sales Order transitions remain owned by handwritten domain behavior, while repositories own persistence and Providers own orchestration and transaction boundaries.
- State history is append-only; root, child, history, conversion, and audit relationships remain non-cascading.
- Root rowversions advance for line-only replacements, stale writes translate to `concurrency_conflict`, and rollback clears tracked state.
- Accepted-quote conversion remains exactly once under concurrency: two simultaneous callers resolve to one order and one initial order-history row.
- Quote-to-order conversion copies stored transaction snapshots and validated pricing inputs rather than silently refreshing catalog text.
- Search contracts retain closed filter/sort allow-lists, escaped wildcard text, SQL paging, 64-bit counts, and deterministic ID tie-breakers.
- Dashboard counts remain one serializable database snapshot with one UTC `AsOf` value.

## Independent verification

Executed from `examples/beacon-ar/src` on 2026-08-01:

- `dotnet restore BeaconAr.sln`: passed.
- `dotnet build BeaconAr.sln --no-restore`: passed with zero warnings and zero errors.
- Offline `dotnet test BeaconAr.sln --no-build --no-restore`: 88 discovered, 67 passed, 21 SQL integration tests skipped, zero failed.
- Published `BeaconAr.Database.dacpac` to a fresh disposable SQL Server 2022 database, then ran the complete database integration project: 21 passed, zero skipped, zero failed.
- Manual search-procedure session probe: the connection reported read-committed isolation after `Sales.SearchQuote` returned.
- `paradigm doctor --project BeaconAr.sln`: success.
- `paradigm packages check`: success.
- `paradigm database validate --strict`: success with no diagnostics.
- `paradigm checks run --project BeaconAr.WebApi/BeaconAr.WebApi.csproj`: success with no diagnostics.
- `dotnet tool run aspire restore --apphost BeaconAr.AppHost/BeaconAr.AppHost.csproj`: success.
- `git diff --check`: no whitespace error.

## Residual test scope

The implementation review did not identify a correctness defect. A future hardening pass could add coordinated reader/writer integration tests that pause detail and search reads between result components; the reviewed serializable boundaries already provide the required consistency guarantee. Product eligibility locks are intentionally acquired once per sorted product ID for deterministic ordering; batching that path may be considered later if quote/order line counts make the additional round trips measurable.
