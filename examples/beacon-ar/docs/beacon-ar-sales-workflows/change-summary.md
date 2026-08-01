# Beacon AR sales-workflows change summary

## Delivered

- Added transport-neutral Quote, Sales Order, conversion, search, line, and Dashboard contracts without introducing HTTP or ASP.NET dependencies.
- Added behavior-rich generated-entity partials for authoritative snapshots, atomic draft replacement, inactive-reference preservation, deterministic line validation, tombstoning, Quote and Sales Order state machines, shipping requirements, and accepted-quote copying.
- Promoted the operation context, persistence reset, audit repository, conflict kind, and rowversion codec primitive into capability-neutral `Operations` boundaries while retaining the field-addressable master-data codec adapter.
- Added exact-name Quote, Sales Order, sales-reference, view, and Dashboard repository contracts and implementations. Aggregate children and histories have no standalone mutation repositories.
- Added SQL Server stored-procedure boundaries for Quote and Sales Order filtered/sorted/paged search and a single serializable Dashboard summary. Generated parameter/data-reader mappers were regenerated and verified deterministic.
- Added transaction-owning Quote, Sales Order, quote-conversion, and Dashboard Providers. Mutations stage root/children/history/audit together, compare canonical rowversions, translate reviewed conflicts, reload database-computed pricing, and roll back plus clear tracked state on failure.
- Implemented accepted-quote singleton conversion with `UPDLOCK, HOLDLOCK`, the filtered source-quote uniqueness constraint as the final guard, retry recovery, exact snapshot/pricing-input copying, and replay support even after an eligible converted draft is tombstoned.

## Important decisions

- SQL Server computed columns and schema-bound pricing views remain authoritative for all returned monetary values. Domain rounding exists for validation/parity tests only.
- Sequence allocation runs on the context-owned connection and enlists in the active Unit of Work transaction. Sequence gaps after rollback remain valid.
- Ordinary detail/search reads exclude tombstones. Conversion lookup intentionally includes a tombstoned source-linked order so reconversion cannot manufacture a second order.
- Search sort fields are closed allow-lists in both Domain validation and SQL; all result ordering ends with the root ID as a deterministic tie-breaker.
- Dashboard acquires its five counts and `AsOf` in one `SERIALIZABLE`, `XACT_ABORT ON` routine with an explicit rollback path.

## Tests added

- Domain tests cover field-addressable validation, decimal/date boundaries, authoritative snapshots, inactive-line preservation, failed-replacement atomicity, both state machines, tombstoning, conversion copying, shipping payload rules, rowversion syntax, and midpoint-away-from-zero rounding.
- Provider tests cover bounded reference selection, history/audit creation, save/transaction counts, stale versions, rollback/tracker clearing, invalid search short-circuiting, status audit codes, and one-call Dashboard pass-through.
- Live SQL Server tests cover authoritative computed pricing, complete Quote-to-Order fulfillment, sequential conversion replay, concurrent conversion singleton behavior across independent scopes/connections, rowversion stale-writer protection, stored-procedure searches, tombstone exclusion, persisted history/audit cardinality, and Dashboard execution.
- Architecture/database-source tests cover exact-name discovery, transport-neutral contracts, aggregate-owned children/history, closed non-dynamic searches, deletion filtering, and the serializable Dashboard routine.

## Verification evidence

- `dotnet restore src/BeaconAr.sln --property:RestoreAdditionalProjectSources=../../artifacts`: passed.
- `dotnet build src/BeaconAr.sln --configuration Release --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution src/BeaconAr.sln --strict --format json`: passed with no diagnostics.
- Persistence regeneration against SQL Server 2025 LocalDB: passed; generated persistence output remained unchanged.
- Stored-procedure mapper regeneration: passed and a second hash comparison was identical.
- Final Release test run against a freshly published disposable SQL Server LocalDB database: 88 passed, 0 failed, 0 skipped. The database integration assembly contributed 21 passing live tests; the disposable database was removed afterward.
- `dotnet tool run paradigm checks run --project src/BeaconAr.WebApi/BeaconAr.WebApi.csproj`: passed.
- `dotnet tool run paradigm doctor`, `paradigm packages check`, and strict database validation: passed. Package audit reported only the documented direct-package update warnings and no vulnerability error.
- `git diff --check`: passed.

## Known tooling note

`paradigm validate` reports the existing database-first public-setter warnings for generated entities. A solution-wide `paradigm checks run --project src/BeaconAr.sln` cannot semantically compile the Aspire AppHost's generated `Projects.*` types; the prescribed consuming Web API project check passes. Neither condition is introduced by the sales workflow implementation.

## Final review remediation

- Added pre-persistence aggregate pricing validation that reproduces the SQL Server line rounding order and rejects any subtotal, discount total, or grand total outside `DECIMAL(19,2)`. The same guard covers Quote create/update, direct-order create/update, and accepted-quote conversion.
- Draft replacements now preserve the original SKU and product-name snapshots for every retained product ID, including active products whose catalog text changed. Current catalog text is captured only when a product is newly selected.
- Eligibility reads now acquire deterministic transaction-enlisted `UPDLOCK, HOLDLOCK` locks for customer, shipping address, product, and carrier rows. Concurrent deactivation or address-type changes therefore serialize before or after the sales mutation instead of invalidating a selection between validation and commit.
- Quote and Sales Order detail projections now execute their header/pricing/version and line reads in one `SERIALIZABLE` transaction. Both paged search routines likewise wrap count and page result sets in explicit serializable transactions with rollback paths.
- Sales field-addressable version validation now delegates canonical Base64 rowversion encoding and decoding to `Operations.VersionTokenCodec`.
- Expanded the live suite with aggregate-overflow and interrupted-save rollback assertions, active-catalog snapshot retention plus conversion copying, Quote rejected/expired branches, cancellation from Draft/Confirmed/Processing, skipped/repeated transition and non-Draft deletion failures, persisted history/audit cardinality, and coordinated customer/address/product/carrier eligibility races.

The final disposable SQL Server run executed 21 database integration tests with zero failures or skips. The complete solution now discovers 88 tests (67 non-database tests plus 21 live database tests).
