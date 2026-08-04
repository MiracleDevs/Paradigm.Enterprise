# Beacon AR Database Views — Change Summary

> Historical record: commands and solution references below describe validation before the migration to root `BeaconAr.slnx`. Use `BeaconAr.slnx` for current work.

## Implemented changes

- Added schema-bound, one-row-per-entity views named `ApplicationUserView`, `ProductView`, `CustomerView`, `CustomerAddressView`, `CarrierView`, `QuoteView`, `QuoteLineView`, `SalesOrderView`, and `SalesOrderLineView`.
- Kept every base-table column on its matching view so the planned EF Core Power Tools-generated entity/view interfaces can provide a compile-time-compatible mapping surface.
- Expanded foreign keys with balanced read data: audit-user display names; customer account/name; address type and display address fields; status code/display name; source quote number; carrier code/name/service level; and parent/product names on line views.
- Followed the actual Beacon AR schema. Quote and sales-order projections expose only `Subtotal`, `DiscountTotal`, and `GrandTotal`; no tax, freight, quote-carrier, line-status, currency, or terms fields were invented.
- Retained the canonically renamed `QuotePricingView` and `SalesOrderPricingView` as schema-bound aggregation helpers. They are implementation dependencies of `QuoteView` and `SalesOrderView`, not consumer-facing DTO views.
- Updated `SearchQuote` and `SearchSalesOrder` to consume `QuoteView` and `SalesOrderView` directly instead of joining the helper pricing views themselves.
- Updated the database smoke script to require every entity view, verify schema binding, and reject duplicate rows by base identifier.
- Extended the live sales workflow test to query all nine views and validate quote/order totals after a complete quote-to-order workflow.
- Documented the entity-view convention, the intended mapping ownership, the current database DTO boundary, and the database project location in the example README.

## Deferred application-layer adoption

This task establishes the SQL Server DTO contract and moves the quote and sales-order search procedures onto `QuoteView` and `SalesOrderView`. It does not yet generate the EF Core Power Tools entity/view types, shared domain interfaces, or database context code. The existing detail repositories therefore continue to read their entity/pricing projections, and no provider mapping changes were made here.

Tasks 2 and 3 will generate the model/context artifacts, move detail reads to the generated view shapes (including detail line views where appropriate), and implement provider-owned entity-to-view mapping and validation orchestration. The target architecture remains: repositories retrieve entities or views; Providers perform mapping. This distinction avoids treating the intended architecture as already implemented.

## SQL project configuration

`BeaconAr.Database.sqlproj` now disables the Microsoft.Build.Sql catch-all SQL glob and explicitly includes model objects under `tables`, `views`, `functions`, `routines`, `types`, and `sequences`. Deployment scripts retain their correct `PreDeploy`, `PostDeploy`, or non-model `None` roles, and `Folder` items make all required object categories visible in Visual Studio without compiling SQL files twice.

The evaluated project contains 37 `Build` items and 37 unique paths, including all nine new entity views. The solution already contained the correctly named project at `src/database/BeaconAr.Database.sqlproj` through the `database\BeaconAr.Database.sqlproj` entry in `src/BeaconAr.sln`, so the solution file required no change.

## Validation

- `dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution src/BeaconAr.sln --strict --format json`: passed with no diagnostics.
- `dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release --no-restore`: passed with 0 warnings and 0 errors; DACPAC model validation includes all schema-bound dependencies.
- `dotnet build src/BeaconAr.sln --configuration Release --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test tests/BeaconAr.Architecture.Tests/BeaconAr.Architecture.Tests.csproj --configuration Release --no-build --no-restore`: passed, 11 tests.
- Published the Release DACPAC successfully to a uniquely named disposable SQL Server 2025 LocalDB instance (`17.0.4025.3`), then removed the instance after validation. SqlPackage emitted its existing warning that target compatibility level 170 is newer than the tool's supported compatibility metadata, but publication completed successfully.
- `FoundationSmokeScriptPasses` and `QuoteConversionAndFulfillmentPersistOneAtomicWorkflow` against that disposable database: passed, 2 tests. This exercised the schema-binding/cardinality smoke checks and queried all nine views with live quote/order data and pricing totals.

## Design references

The view shape follows the `rdx-dms-mvp` pattern of schema-bound `{Entity}View` objects that preserve the base entity columns and add useful display fields through explicit joins. The implementation also follows the Paradigm database skill's SQL Server guidance: one object per file, two-part object names, explicit projection lists, schema binding, database source under `src/database`, and deterministic validator/DACPAC checks.

The task decision record documents the consumer-facing table scope, exclusions for internal/status/history/idempotency storage, pricing-helper boundary, joined-field balance, deferred repository adoption, and why these judgment-based choices were added as guidance rather than a deterministic validator: [decisions](decisions.md).
