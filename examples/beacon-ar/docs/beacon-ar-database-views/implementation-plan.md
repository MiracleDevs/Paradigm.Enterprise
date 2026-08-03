# Beacon AR Database Views — Implementation Plan

## Objective

Expose schema-bound read views that supply the Beacon AR API/domain entity shapes with base-record fields plus useful related display data. Consolidate quote and sales-order pricing totals into their respective primary views while allowing schema-bound pricing helpers to remain internal database implementation details.

## Scope

- Add schema-bound `{Entity}View` SQL views for:
  - `ApplicationUser`
  - `Product`
  - `Customer`
  - `CustomerAddress`
  - `Carrier`
  - `Quote`
  - `QuoteLine`
  - `SalesOrder`
  - `SalesOrderLine`
- Select all base-table columns necessary to match the corresponding entity interfaces, then add useful joined names, codes, status descriptions, and pricing values.
- Expose the existing subtotal, discount-total, and grand-total calculations through `QuoteView` and `SalesOrderView`; rework consumers so internal pricing helpers are not treated as API DTO projections.
- Make `BeaconAr.Database.sqlproj` explicitly present database object categories (tables, views, functions, routines, types, and sequences) and relevant deployment scripts in the project structure without adding duplicate SDK `Build` items. Use an explicit Microsoft.Build.Sql-compatible project-item approach; do not use `None`/`Content` entries solely to create linked visibility.
- Confirm the database project remains correctly named, is located at `src/database`, and is included by root `BeaconAr.slnx`.
- Update affected documentation and tests, then validate strict database checks, the SQL project build, and the full solution build.

## Non-goals

- No new write procedures, API endpoints, or domain behavior beyond the read projections.
- No DTO view for append-only/internal tables or status catalogs unless a consuming entity/API DTO actually needs one.
- No schema redesign or data migration unrelated to enabling these projections.
- No implementation or commit is part of this planning change.

## Planned file changes

| Area | Planned changes |
| --- | --- |
| `examples/beacon-ar/src/database/Views/` | Add the nine schema-bound entity views, organized consistently with existing database object folders/conventions. |
| `examples/beacon-ar/src/database/BeaconAr.Database.sqlproj` | Explicitly surface object folders/categories and relevant scripts without duplicate `Build` inclusions under `Microsoft.Build.Sql`. |
| `examples/beacon-ar/src/database/...` consumers | Replace any references to `QuotePricing`/`SalesOrderPricing` with their consolidated parent projections. |
| `examples/beacon-ar/src/...` domain/repository/provider/API code | Align read mappings, contracts, and queries with the new views where current projections depend on pricing views. |
| `examples/beacon-ar/tests/...` | Add/adjust projection and database integration coverage. |
| `examples/beacon-ar/docs/...` | Document the view contract, projection boundaries, and pricing consolidation. |
| `examples/beacon-ar/BeaconAr.slnx` | Verification only: ensure the project entry references `src/database/BeaconAr.Database.sqlproj` under `02.Modules`. |

## View design

All views should use `WITH SCHEMABINDING`, two-part object names, explicit column lists (no `SELECT *`), and deterministic aggregates/joins permitted by the target SQL Server rules. Every selected expression should receive a stable, intentional alias where the entity/read model expects a named field.

| View | Base projection | Related joins / derived fields |
| --- | --- | --- |
| `ApplicationUserView` | Application-user entity columns, identifiers, audit/state fields needed by the interface. | User status code/name and other required identity/display attributes from approved lookup tables, if modeled separately. |
| `ProductView` | Product entity columns including SKU/code, descriptive fields, pricing, state, and audit fields. | Product status name/code and related classification names/codes where those are part of the read shape. |
| `CustomerView` | Customer entity columns, code/name/contact/business fields, state, and audit fields. | Customer status name/code; preferred/primary address display data only if the entity interface requires it. |
| `CustomerAddressView` | Customer-address entity columns, address components, flags, state, and audit fields. | Customer code/name plus address/status descriptors needed for consumers. |
| `CarrierView` | Carrier entity columns, carrier code/name/contact/state/audit fields. | Carrier status name/code and any supported service/display code. |
| `QuoteView` | Quote entity columns, customer/shipping-address references, dates, status, snapshots, deletion metadata, and audit fields. | Customer and shipping-address display data; quote status code/display name; existing line-derived subtotal, discount total, and grand total; converted sales-order identifier. Quotes have no carrier, tax, freight, currency, or terms columns in the Beacon AR schema. |
| `QuoteLineView` | Quote-line entity columns, parent/product references, quantities, unit prices, discount, and existing computed line totals. | Quote number and current product SKU/name/activity. Quote lines have no tax or line-status columns in the Beacon AR schema. |
| `SalesOrderView` | Sales-order entity columns, source quote/customer/shipping-address/carrier references, status, snapshots, deletion metadata, and audit fields. | Customer/address, carrier, source-quote, and status display data; existing line-derived subtotal, discount total, and grand total. Sales orders have no tax, freight, currency, or terms columns in the Beacon AR schema. |
| `SalesOrderLineView` | Sales-order-line entity columns, parent/product references, quantities, unit price, discount, and existing computed line totals. | Sales-order number and current product SKU/name/activity. Sales-order lines have no tax or line-status columns in the Beacon AR schema. |

## Implementation sequence

1. Inventory the generated/entity interfaces and current table definitions to establish exact required columns, nullability, aliases, and identifier conventions for each view.
2. Inspect existing database view, function, table, and naming conventions; identify current `QuotePricing` and `SalesOrderPricing` definitions and all consumers.
3. Add the nine schema-bound views with explicit dependencies. Design totals so consumers read them through `QuoteView` and `SalesOrderView`, preserving existing business-calculation semantics and null/rounding behavior while retaining internal helpers if needed for schema-safe aggregation.
4. Update repository/read-model mappings and dependent SQL so they select the consolidated quote/order projections instead of standalone pricing views.
5. Update `BeaconAr.Database.sqlproj` using explicit item/folder metadata compatible with SDK-style Microsoft.Build.Sql. Verify that SQL source files are compiled once by default and that the project UI still clearly presents tables, views, functions, routines, types, sequences, and relevant pre/post-deployment or seed scripts.
6. Verify `BeaconAr.Database.sqlproj` name/path and its existing root `BeaconAr.slnx` inclusion; correct only if evidence shows a discrepancy.
7. Update architecture/database documentation and tests to state that API DTO projections are limited to consumer-facing entities, not append-only/internal tables or status catalogs by default.
8. Run the required validation commands and resolve any schema-binding, dependency, DACPAC, or mapping failures.

## Test and validation plan

- Add database tests that deploy/build the database and query every new view.
- Assert each view exposes the required entity-interface columns with expected aliases and nullability-compatible values.
- Seed representative records to validate joined names/codes/status descriptions.
- For quote and sales-order views, test zero-line, one-line, and multi-line cases; verify subtotal, discount-total, and grand-total calculations, including null and rounding behavior.
- Assert no consumer treats `QuotePricing` or `SalesOrderPricing` as an API DTO after the consolidation; schema-bound helper dependencies may remain internal.
- Run the repository's strict database validation command.
- Build `examples/beacon-ar/src/database/BeaconAr.Database.sqlproj` (including DACPAC/schema-binding validation).
- Build `examples/beacon-ar/BeaconAr.slnx` and run the affected test suite.

## Decisions and risks

| Topic | Decision / mitigation |
| --- | --- |
| Projection boundary | Entity-facing views are added only for the named business entities. Append-only/internal tables and status catalogs remain implementation details unless a concrete API DTO/interface requires exposure. |
| Schema binding | Use explicit two-part references and columns to preserve binding validity; avoid unsupported constructs and account for dependency ordering during table/view changes. |
| Pricing migration | `QuoteView` and `SalesOrderView` become the sole consumer-facing parent pricing projections. Preserve the existing totals contract while keeping schema-bound helper views internal if aggregation requires them. |
| Aggregation semantics | Compare existing pricing definitions/tests before exposing totals through parent views, especially around discounts, rounding, and empty-line behavior. Do not invent tax or freight semantics absent from the schema. |
| SDK project items | Microsoft.Build.Sql normally includes SQL build assets by convention. Do not add overlapping `Build` globs/items; validate the project after adding visibility/organization metadata. |
| Join cardinality | Keep entity views one row per base entity; use carefully constrained joins or aggregates so related tables cannot duplicate result rows. |
| API compatibility | Treat aliases and data types consumed by generated/domain/repository mappings as a contract; update consumers and tests atomically. |
