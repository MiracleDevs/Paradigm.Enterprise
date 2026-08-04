# Beacon AR Database Views — Review Feedback

> Historical record: commands and solution references below describe validation before the migration to root `BeaconAr.slnx`. Use `BeaconAr.slnx` for current work.

## Findings

### High — the documented view boundary is not yet implemented in the data layer

- **Locations:** [README.md](../../README.md#L9), [QuoteViewRepository.cs](../../src/BeaconAr.Data/Sales/QuoteViewRepository.cs#L49), and [SalesOrderViewRepository.cs](../../src/BeaconAr.Data/Sales/SalesOrderViewRepository.cs#L86).
- **Observed behavior:** The documentation says API-facing read models are generated from and queried through the entity views. The two existing view repositories still read `Quotes`/`SalesOrders`, join `QuotePricings`/`SalesOrderPricings`, and manually reconstruct the related data. The new views are therefore used only by the two search stored procedures and integration assertions, not by the detail read path.
- **Consequence:** This leaves the core requirement—repositories retrieve view shapes and the provider maps later—unmet, preserves the duplicate pricing/join projection, and gives future maintainers a false description of the implemented architecture.
- **Smallest correction:** Regenerate the view entities/context from the new views and change the detail repositories to retrieve those view projections (with line views for detail collections), then map at the provider boundary. If that work is deliberately deferred to the next task, change the README and this task's change summary to state that the database contract exists but the data/domain generation and repository adoption are pending; do not claim it is already queried through the views.

## Verified

- The nine requested entity views are present, use the required `{Entity}View` names, are schema-bound, explicitly list their projections, preserve all columns from their respective base tables, and add bounded one-to-one/lookup joins. The quote-to-order join is protected from row multiplication by `UQ_SalesOrder_SourceQuoteId`.
- `QuotePricingView` and `SalesOrderPricingView` remain sensible internal, schema-bound aggregation helpers. These canonical names supersede the original pre-convention names. `QuoteView` and `SalesOrderView` expose their totals, and the modified search procedures retain their stable count-first/result-second contract and deterministic final `Id` ordering.
- `BeaconAr.Database.sqlproj` is correctly named, under `src/database`, included by `src/BeaconAr.sln`, explicitly includes the database model categories once, and retains the pre-pre, pre-deployment, post-deployment, seed, verification, and maintenance item roles without duplicate `Build` items.
- The added smoke checks cover existence, schema binding, and duplicate base rows for all nine views. The committed task documentation records the projection and pricing decisions.

## Validation and residual gaps

- Passed: `dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution src/BeaconAr.sln --strict`.
- Passed: `dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release --no-restore` (0 warnings, 0 errors).
- I did not repeat the disposable-database publish/live integration suite reported in the change summary. It remains the appropriate final verification for the actual view contents, joins, and aggregate totals after the data-layer correction.

## Resolution

The README and change summary now distinguish the implemented database contract from the planned application-layer adoption. The nine schema-bound DTO views and the `SearchQuote`/`SearchSalesOrder` procedure changes are complete in this task. EF Core Power Tools generation of entity/view types, shared domain interfaces, and context code is deferred to Task 2; changing detail repositories to retrieve the generated views and implementing Provider-owned mapping is deferred to Task 3.

This preserves the durable architectural rule that repositories retrieve entities or view shapes and Providers own mapping and validation orchestration, without incorrectly claiming that the current detail repositories already use the new views. No repository or generated-domain code was changed because those depend on the generation work explicitly assigned to the next tasks.

## Second-pass result

The high finding is **resolved for Task 1**. The README and change summary now accurately distinguish the completed SQL Server view/procedure contract from the intentionally deferred EF Core Power Tools generation, shared interfaces/context work (Task 2), and repository/provider adoption (Task 3). The current repositories are no longer represented as though they already consume the new views.

No remaining actionable database-view or SQL-project defects were identified in this second pass. The residual work is the explicitly recorded Task 2/3 application-layer adoption, not a defect in this database task.

Quick verification remains clean:

- `dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution src/BeaconAr.sln --strict`
- `dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release --no-restore` (0 warnings, 0 errors)

## Final-pass result

No actionable issues found. The new database guidance is appropriately scoped: it requires entity-named, schema-bound projections only for consumer-facing major/entity transaction tables, retains IDs and the base mapping surface, and explicitly leaves helper/reporting and internal/status/history/audit/idempotency tables to concrete-consumer judgment. That avoids both a blanket-view rule and an unsupported deterministic validator.

The SQL Server guidance gives one bounded, verified response to the Visual Studio visibility problem—disable the SDK SQL glob, explicitly include model categories once, and preserve deployment-script roles—without prescribing it when SDK globbing already works. `decisions.md` records the example-specific scope, excluded storage, pricing-helper boundary, join-cardinality rule, deferred application adoption, and why the learning is guidance rather than a validator.
