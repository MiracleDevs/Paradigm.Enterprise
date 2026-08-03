# Beacon AR foundation implementation plan

## Objective

Create `examples/beacon-ar/` as a self-contained .NET 10 Beacon AR backend example based on the reviewed Paradigm Web API template. Establish a modular-monolith solution, SQL Server database-first source of truth, Aspire-managed local topology, foundational domain contracts, and deterministic verification. The foundation must make every release-one persistence and integrity requirement possible without prematurely implementing master-data use cases, quote/order workflows, dashboard queries, or the public REST contract.

This task establishes buildable boundaries and durable data contracts. Later tasks own business repositories, Providers, controllers, authentication behavior, OpenAPI shapes, and end-to-end scenarios.

## Inspected baseline

- The repository is on `task/beacon-ar-foundation`; `examples/beacon-ar/` does not yet exist except for this task document.
- .NET SDK `10.0.302` is installed.
- The repository's approved Paradigm package line is `1.1.0` in `build/Paradigm.Version.props`.
- The sibling reviewed template exists at `C:\Repositories\github\Paradigm.Web.ApiTemplate`. Its Domain, Data, Providers, WebApi, and CodeGenerator projects target .NET 10; its Interfaces analyzer targets `netstandard2.0` as required for Roslyn loading.
- The checked-out template still contains literal Paradigm `1.0.23` references and one `Microsoft.AspNetCore.OpenApi` 9.x reference. Scaffold with `--paradigm-version 1.1.0`, then centralize/normalize dependencies; do not copy those literals as the Beacon AR dependency policy.
- The repository-root tool manifest currently contains DocFX only. The implementation must create an example-local manifest rather than assuming a globally installed Paradigm or Aspire CLI.
- The Paradigm CLI scaffolder refuses a non-empty destination and copies its result beneath `<output>/src`. Because this plan already makes `examples/beacon-ar/` non-empty, scaffold into a temporary empty staging directory, inspect it, then move only the generated application files into the example while preserving `docs/`.
- Current framework APIs use `EntityBase<int>`, `DbContextBase<int>`, `RegisterContext<TContext>(connectionStringName)`, and a scoped `SqlServerDbContextConnectionProvider`. Repositories register their context with `IUnitOfWork`; `CommitChangesAsync` saves participants sequentially and does not start a transaction. Atomic use cases must resolve all participating repositories first, call `CreateTransaction()`, stage changes, commit the Unit of Work, then commit/roll back the transaction.
- The existing template is a scaffold, not a production host: it uses a web-app authentication helper, permissive CORS, a sample authentication middleware, and an empty health route. None of those defaults is accepted unchanged for the bearer-token API required here.

## Scope boundary

### Included in `beacon-ar-foundation`

- A new `BeaconAr` .NET 10 solution under `examples/beacon-ar/src` with the reviewed layer projects and correct dependency direction.
- Aspire AppHost, ServiceDefaults, and a finite SQL Server database bootstrap project.
- An SDK-style `Microsoft.Build.Sql` SQL Server project under `src/database`, included in the application solution.
- The complete release-one relational schema, constraints, indexes, system catalogs, status histories, audit log, idempotency ledger, sequences, and derived-pricing views.
- Database-first generation configuration and generated persistence shapes, with handwritten code kept outside replaceable files.
- Minimal domain primitives and getter-only contracts needed to stabilize identifiers, audit fields, workflow catalog values, address-type write values, concurrency tokens, and monetary rounding.
- Configuration contracts, safe placeholder configuration, standard liveness/readiness wiring, and registration composition roots sufficient for the empty host to start after database publication.
- Domain, architecture, schema, and bootstrap tests appropriate to the foundation.
- Repository CI/quality integration for restore, build, test, Paradigm validation, database validation, and OpenAPI-ready host startup.

### Explicitly deferred

- Product, customer, address, and carrier CRUD repositories, Providers, and endpoints.
- Search projections, endpoint-specific filters/sort allow-lists, and paginated HTTP response DTOs.
- Quote commands, pricing orchestration, draft editing, state transitions, deletion behavior, and conversion implementation.
- Direct sales-order commands, fulfillment transitions, tracking assignment, and deletion implementation.
- Dashboard query/repository/provider/controller implementation.
- `/me`, JWT claim-to-user synchronization, policy handlers, controller authorization metadata, Problem Details translation, ETags/`If-Match`, idempotency HTTP behavior, CORS finalization, and all `/api/v1` routes.
- OpenAPI endpoint schemas, NSwag Angular client generation, and frontend integration.
- Deterministic sample business data. The foundation seeds only application-owned closed catalogs; later feature tasks own non-production products/customers/addresses/carriers.
- A production deployment target or Bicep output. Local managed SQL Server is not a production architecture decision.

## Architecture decisions

Each decision records context, choice, consequences, rejected alternatives, and a trigger for reconsideration.

### AD-01: one deployable modular monolith

- **Context:** Release one is one AR team, one API, one consistency boundary, and has no independently scaled capability or separate data owner.
- **Choice:** Build one `BeaconAr.WebApi` deployable. Organize every layer by the `Access`, `MasterData`, `Sales`, `Reporting`, and `Operations` capabilities. Preserve `WebApi -> Providers -> Data -> Domain -> Interfaces`; lower layers never reference ASP.NET Core or the hosts.
- **Consequences:** Cross-capability calls are in-process and releases remain coordinated. Capability folders and focused contracts are mandatory so the monolith does not become an unstructured catalog.
- **Rejected:** Separate quote, order, catalog, and reporting services; they add network failure and distributed consistency without a release-one owner/scale benefit.
- **Reconsider when:** independent teams, deployment cadence, security isolation, or measured scaling needs justify a service boundary.

### AD-02: one receivables database and one domain context

- **Context:** Quote conversion, master-data references, default-address changes, histories, idempotency, and audit records require database transactions. Paradigm's Unit of Work is only atomic when an explicit compatible transaction is used.
- **Choice:** `ReceivablesDbContext : DbContextBase<int>` owns the Beacon AR schema. Repositories remain capability-specific even though they share this domain-specific context and the canonical `DatabaseConnection` connection name.
- **Consequences:** Release-one invariants can use a single SQL transaction. Reporting reads the same database with read-only projections. The context must be organized through capability-specific configuration partials rather than becoming a business-logic owner.
- **Rejected:** one general `AppDbContext` by convenience; several module contexts for one database, which add enlistment complexity without independent ownership; separate databases, which make quote conversion distributed.
- **Reconsider when:** a capability gets a real data owner and no synchronous cross-boundary invariant remains.

### AD-03: SQL Server database-first source of truth

- **Context:** SQL Server is preferred and the example must demonstrate Paradigm database-first generation and durable integrity.
- **Choice:** Use an SDK-style `Microsoft.Build.Sql` project targeting SQL Server 2022-compatible features. The DACPAC is schema source; generated EF/context files are replaceable output. No EF migration catalog is added.
- **Consequences:** Every object is one file, grouped by capability; publication is DACPAC-based and validated before model regeneration. Production Azure/SQL target compatibility must be confirmed separately.
- **Rejected:** EF migrations, PostgreSQL, ad hoc API-startup schema creation, and a second database-only solution.
- **Reconsider when:** the deployment target is selected or a platform constraint requires another SQL target.

### AD-04: external identity with a local application-user key

- **Context:** Entra/OIDC is the authentication authority, while audit FKs need a stable local positive integer identity.
- **Choice:** `ApplicationUser` maps `(Issuer, Subject)` to an internal integer `Id` and stores current display name/email metadata. Token claims determine effective `business.read` and `business.write`; the database does not become an identity provider or authorization store.
- **Consequences:** All audit FKs use `int`, matching business entity IDs and `DbContextBase<int>`. Users are never cascade-deleted. A later security task owns safe just-in-time synchronization and claim mapping.
- **Rejected:** storing access tokens, using email as identity, custom password authentication, or persisting Entra role membership as the authorization authority.
- **Reconsider when:** multi-issuer subject collision, tenant partitioning, or local entitlement administration becomes a requirement.

### AD-05: explicit bearer-token API boundary

- **Context:** Requirements explicitly call for bearer tokens attached by the Angular SPA and 401/403 semantics.
- **Choice:** Replace the template web-app/cookie setup with JWT bearer resource-server configuration in the later API/security task. Foundation configuration reserves authority/issuer, audience, accepted scopes/roles, and known SPA origins but contains no credentials.
- **Consequences:** The SPA must use Authorization Code with PKCE. All business endpoints later require explicit policies; operational health routes stay separate and secret-free.
- **Rejected:** the template's custom GUID header middleware, permissive CORS, anonymous base-controller inheritance, and storing tokens in the database.
- **Reconsider when:** the product adopts a backend-for-frontend/session architecture.

### AD-06: optimistic concurrency is database `rowversion`

- **Context:** Every mutable resource must reject stale writers.
- **Choice:** Add `RowVersion ROWVERSION NOT NULL` to Product, Customer, CustomerAddress, Carrier, Quote, and SalesOrder. Aggregate-line writes always touch their root so its version advances. Later HTTP contracts encode the eight bytes as a strong ETag and require `If-Match` on mutation.
- **Consequences:** Lines do not expose independent concurrency tokens because they are mutated only through the aggregate root. EF mappings mark row versions generated-on-write.
- **Rejected:** timestamps, client counters, and last-write-wins.
- **Reconsider when:** offline merge semantics require a richer conflict model.

### AD-07: append-only workflow history and tombstoned draft deletion

- **Context:** Database guidance requires initial and transition history to be append-only with non-cascading FKs. The API also allows draft quote/order deletion.
- **Choice:** Quote and SalesOrder contain nullable `DeletionDate` and `DeletedByUserId` lifecycle facts. A successful draft DELETE tombstones the aggregate and returns 204; reads treat it as absent. Lines and status history remain preserved. This is deliberate lifecycle modeling, not a global `IsDeleted` convention.
- **Consequences:** Numbers remain reserved, FK history is retained, and every query must exclude tombstoned transactions unless an operator-only tool explicitly requests them. Database filtered indexes account for tombstoned rows where appropriate.
- **Rejected:** cascading hard deletion of histories/lines and a generic application-wide soft-delete filter.
- **Reconsider when:** legal retention policy requires physical erasure or the product explicitly defines draft purging.

### AD-08: stored inputs, database-derived line amounts, view-derived aggregate totals

- **Context:** Clients must not edit totals and snapshots/prices must remain authoritative.
- **Choice:** Store quantity, unit price, and discount percent. Persist computed line subtotal/discount/total columns using two-decimal `ROUND(..., 2)`; SQL Server and .NET both use midpoint-away-from-zero for nonnegative release-one amounts. Schema-bound pricing views sum the rounded line values for quote/order totals.
- **Consequences:** No duplicated aggregate-total column can drift. Product `UnitPrice` uses `decimal(19,4)` and must be positive; transaction line unit prices use `decimal(19,4)` and may be zero; percent uses `decimal(5,2)`; returned amounts use `decimal(19,2)`. Aggregates sum rounded lines, not unrounded raw extensions.
- **Rejected:** browser-derived totals, floating point, stored editable totals, and a trigger-maintained header total.
- **Reconsider when:** taxes, multi-currency, or alternate rounding rules enter scope.

### AD-09: local Aspire manages SQL; finite bootstrap owns schema publication

- **Context:** Developers need repeatable local startup, while external databases must never be changed implicitly.
- **Choice:** Default `.env.example` to `SqlServer`, `Managed`, a persistent container, and `Database__PublishOnStart=true`. The graph is `sqlserver -> database -> database-bootstrap -> webapi`; the API uses `WaitForCompletion` on bootstrap. External mode requires `ConnectionStrings__DatabaseConnection`, disables BACPAC import, and defaults publication off.
- **Consequences:** First run and restart are deterministic. `.env` is ignored, process variables have precedence, and no secret is logged. No cloud target is inferred.
- **Rejected:** Docker scripts that bypass Aspire, schema publication in WebApi startup, runtime tool installation, and automatic import/publish to external databases.
- **Reconsider when:** a production platform and deployment owner are chosen.

### AD-10: no release-one external side-effect infrastructure

- **Context:** Release one sends no email, message, payment, or inventory reservation.
- **Choice:** Use ordinary database transactions only; do not add a queue or outbox speculatively.
- **Consequences:** Audit and idempotency are database facts in the same transaction as the mutation. Future external delivery must define timeout, retry, duplication, and recovery before introducing an outbox.
- **Rejected:** an unused message bus/outbox and claims that a database transaction can make future remote calls atomic.
- **Reconsider when:** a required external side effect is added.

## Target repository layout

```text
examples/beacon-ar/
|-- .config/dotnet-tools.json
|-- .env.example
|-- .gitignore
|-- Directory.Build.props
|-- Directory.Packages.props
|-- README.md
|-- aspire.config.json
|-- start.sh
|-- docs/beacon-ar-foundation/
|   |-- implementation-plan.md
|   |-- change-summary.md
|   `-- review-feedback.md
|-- BeaconAr.slnx
|-- src/
|   |-- BeaconAr.Interfaces/
|   |-- BeaconAr.Domain/
|   |-- BeaconAr.Data/
|   |-- BeaconAr.Providers/
|   |-- BeaconAr.WebApi/
|   |-- BeaconAr.CodeGenerator/
|   |-- BeaconAr.ServiceDefaults/
|   |-- BeaconAr.AppHost/
|   |-- BeaconAr.DatabaseBootstrap/
|   `-- database/
|       |-- BeaconAr.Database.sqlproj
|       |-- tables/{Access,MasterData,Sales,Operations}/
|       |-- views/{Sales,Reporting}/
|       |-- sequences/Sales/
|       |-- scripts/prepredeployment/PrePreDeployment.sql
|       |-- scripts/predeployment/PreDeployment.sql
|       |-- scripts/postdeployment/PostDeployment.sql
|       |-- scripts/postdeployment/{MasterData,Sales}/*Data.sql
|       |-- scripts/verification/FoundationSmoke.sql
|       `-- bootstrap/                 # empty; optional BeaconAr.bacpac later
`-- tests/
    |-- BeaconAr.Domain.Tests/
    |-- BeaconAr.Architecture.Tests/
    `-- BeaconAr.Database.IntegrationTests/
```

The scaffolded nested `.github/` files are not retained because GitHub ignores nested workflow directories in this monorepo. Reuse the repository-root Paradigm problem matcher and extend the existing root quality workflow for Beacon AR.

## Project and dependency boundaries

### Project references

- `BeaconAr.Interfaces`: Roslyn analyzer/source-generator project targeting `netstandard2.0`; references `Paradigm.Enterprise.Interfaces` and compiler packages with `PrivateAssets=all`.
- `BeaconAr.Domain`: targets `net10.0`; references `Paradigm.Enterprise.Domain`; references Interfaces as `OutputItemType="Analyzer"`. It owns entities, read views, status/address constants, validation, and repository contracts.
- `BeaconAr.Data`: targets `net10.0`; references Domain and `Paradigm.Enterprise.Data.SqlServer`. It owns `ReceivablesDbContext`, EF configuration, database-first generated shapes, repositories, and read-query mechanics.
- `BeaconAr.Providers`: targets `net10.0`; references Data and `Paradigm.Enterprise.Providers`. Foundation contains only registration/composition seams; later tasks add use cases.
- `BeaconAr.WebApi`: targets `net10.0`; references Providers plus Data/Domain only where the template's discovery registration requires them. It owns transport, host policy, serialization, middleware, and later controllers; no business decisions.
- `BeaconAr.CodeGenerator`: targets `net10.0`; remains an explicit developer tool for JSON contexts, stored-routine mappers, and later NSwag generation. It must not assume a non-existent frontend path in the foundation.
- `BeaconAr.ServiceDefaults`: targets `net10.0`; supplies OpenTelemetry defaults and `/health` readiness plus `/alive` liveness.
- `BeaconAr.DatabaseBootstrap`: targets `net10.0`; references ServiceDefaults only if needed and owns finite SQL publication orchestration. It does not reference WebApi or install tools.
- `BeaconAr.AppHost`: targets `net10.0`; references WebApi and DatabaseBootstrap and composes infrastructure. No lower layer references AppHost.
- Domain and architecture tests reference only the layers they inspect. Database integration tests reference Data/Domain and explicitly reference the SQL client package they use; they do not rely on a transitive compile dependency.

### Dependency policy

1. Add `Directory.Packages.props` at the example root with central package management. Import `../../build/Paradigm.Version.props` and assign every `Paradigm.Enterprise.*` package `$(ParadigmEnterpriseVersion)` so the example tests the repository's current package line.
2. Pin all other direct NuGet dependencies centrally and enable a packages lock file. Normalize .NET packages to compatible 10.x releases; do not retain the template's `Microsoft.AspNetCore.OpenApi` 9.x pin.
3. Before adding packages not already approved for this example, record purpose, alternatives, license, official repository, maintenance/security posture, and meaningful transitives, then obtain the required approval. Expected review groups are Aspire hosting/service defaults, Microsoft.Build.Sql, SQL client/SqlPackage, Microsoft identity/OpenAPI, and MSTest test tooling.
4. Add only packages used directly by each project. Do not rely on transitive compile access.
5. Pin `Paradigm.Enterprise.Cli` to `1.1.0` in the example-local tool manifest, pin `dotnet-sqlpackage`, and let the first approved stable Aspire CLI install record its resolved version. Subsequent restores must not silently upgrade tools.

## SQL Server schema plan

Use `[dbo]`, singular PascalCase object/column names, named keys/indexes/defaults, and no cascades unless an explicitly owned lifecycle demands one. In this foundation all foreign keys use `NO ACTION`; aggregate child removal is an explicit repository operation so business history cannot disappear through an accidental root delete.

All business/resource IDs are `INT IDENTITY(1,1)`. Audit-log/idempotency IDs may be `BIGINT IDENTITY`. System catalogs use assigned `INT` IDs. Audit timestamps use `DATETIMEOFFSET(7)` UTC. Calendar dates use `DATE`. Text lengths below are storage/API maxima that later request validation must mirror.

### Access and operations

#### `ApplicationUser`

- `Id`, `Issuer` (400), `Subject` (200), `DisplayName` (200), `Email` (320 nullable), `IsActive`.
- Standard audit columns: `CreatedByUserId`, `CreationDate`, `ModifiedByUserId`, `ModificationDate`. Bootstrap/operator-created users may have null actor IDs; normal application mutations require an authenticated actor.
- `UQ_ApplicationUser_Issuer_Subject`; optional normalized email index for lookup only, never identity.
- Self-referencing audit FKs are `NO ACTION`. Users are not physically deleted by business APIs.

#### `AuditLog`

- `Id BIGINT`, `ResourceType` (100), `ResourceId` (50 string so future non-integer operational resources remain representable), `Action` (100), `UserId`, `CreationDate`, `CorrelationId` (128), `PreviousStatusCode`/`NewStatusCode` (32 nullable), and optional safe structured metadata with an enforced size limit.
- Indexes on `(ResourceType, ResourceId, CreationDate)` and `(CorrelationId)`.
- Append-only application contract; no generic update/delete repository. Do not store tokens, credentials, address payloads, email bodies, or other personal snapshots in metadata.

#### `IdempotencyRequest`

- `Id BIGINT`, `UserId`, `Operation` (150), `KeyHash BINARY(32)`, `RequestHash BINARY(32)`, `StateId`, `ResourceType`/`ResourceId`, `ResponseStatusCode`, `CreationDate`, `CompletionDate`, and `ExpirationDate`.
- Unique `(UserId, Operation, KeyHash)` prevents duplicate creation; a request hash mismatch later produces a conflict instead of replaying another request's result.
- Seeded state catalog values `InProgress=1`, `Completed=2`, `Failed=3`, aligned with a service-side enum. Housekeeping is an operator-only later concern and never deletes an in-progress entry.

Every later mutation writes its resource changes and `AuditLog` row in the same explicit transaction. Creation endpoints that advertise idempotency also create/complete the idempotency row in that transaction.

### Master data

#### `Product`

- `Id`, `Sku NVARCHAR(32)` with explicit `Latin1_General_100_CI_AS_SC` collation, `Name` (120), `Category` (120), `UnitPrice DECIMAL(19,4)`, `StockQuantity INT`, `ThumbnailUrl` (2,048 nullable), `IsActive`, standard audit columns, `RowVersion`.
- Trim/nonblank/length checks, `UnitPrice > 0`, `StockQuantity >= 0`, `UQ_Product_Sku`, and indexes supporting `(IsActive, Id)`, SKU/name ordering, and the documented search projection.
- URL syntax/scheme is a Provider validation rule; SQL stores the bounded value and never fetches it.

#### `Customer`

- `Id`, `AccountNumber` (50, explicit case-insensitive collation), `Name` (120), `Email` (320), `Phone` (50 nullable), `CreditLimit DECIMAL(19,2)`, `PaymentTermsDays SMALLINT`, `IsActive`, standard audit columns, `RowVersion`.
- Trim/nonblank checks, `CreditLimit >= 0`, payment terms in `(0,15,30,45,60)`, `UQ_Customer_AccountNumber`, and search/sort/active indexes.
- Full email validation remains in Domain/Provider code.

#### `AddressType`

- Assigned IDs and stable codes: `Billing=1`/`billing`, `Shipping=2`/`shipping`, `Both=3`/`both`, with display name and `IsActive`.
- The read boundary returns the code as a string, not a closed public JSON enum. Future/legacy inactive catalog rows can remain readable; later writes accept only the three active release-one codes.

#### `CustomerAddress`

- `Id`, `CustomerId`, `AddressTypeId`, `Label` (120), `Line1` (200), `Line2` (200 nullable), `City` (120), `State` (120 nullable), `PostalCode` (32), `Country` (2 ISO alpha-2), `IsDefaultBilling`, `IsDefaultShipping`, standard audit columns, `RowVersion`.
- Required/trim checks and checks that billing default is only set for Billing/Both and shipping default only for Shipping/Both.
- Filtered unique indexes on `CustomerId` where each default bit is true guarantee at most one default of each kind under concurrency.
- Indexes on `(CustomerId, AddressTypeId, Id)` and label. There is intentionally no active/archive column in release one.

#### `Carrier`

- `Id`, `Code` (32, explicit case-insensitive collation), `Name` (120), `ServiceLevel` (120), `TrackingUrlTemplate` (2,048 nullable), `IsActive`, standard audit columns, `RowVersion`.
- Trim/nonblank checks, `UQ_Carrier_Code`, and active/search/sort indexes.
- HTTPS and optional `{trackingNumber}` token validation stays in Domain/Provider code; the application never fetches this URL.

Every master-data FK from transactional history uses `NO ACTION`, so normal physical deletion naturally fails for referenced products, customers, addresses, and carriers. Customer-to-address is also `NO ACTION` because a customer with any address cannot be physically deleted.

### Quotes

#### `QuoteStatus`

- Assigned IDs and codes: `Draft=1`, `Sent=2`, `Accepted=3`, `Rejected=4`, `Expired=5`.
- Rerunnable authoritative seed script with display name and `IsActive`; IDs are never reused, renumbered, or source-deleted.
- `QuoteStatus` .NET enum has exact numeric parity. Transition legality remains domain behavior, not a repository or foreign-key decision.

#### `Quote`

- `Id`, immutable `QuoteNumber` (20), `CustomerId`, `ShippingAddressId`, `QuoteDate DATE`, `ValidUntil DATE`, `StatusId`, `Notes NVARCHAR(1000)` nullable.
- Immutable customer snapshot: account number, name, email, and phone.
- Immutable shipping snapshot: label, line1, line2, city, state, postal code, country, and transported address-type code.
- Standard audit columns, `DeletionDate`, `DeletedByUserId`, and `RowVersion`.
- `UQ_Quote_QuoteNumber`; checks for nonblank number, `ValidUntil >= QuoteDate`, bounded notes, and actor/date pairing for tombstones.
- Search/filter/sort indexes on quote number, `(StatusId, QuoteDate, Id)`, `(CustomerId, Id)`, and `(ValidUntil, Id)`.
- A SQL sequence supplies collision-free numeric values; the later Provider formats `Q-00000001` (stable prefix plus eight digits) before insert. Database uniqueness remains the final guard.

#### `QuoteLine`

- `Id`, `QuoteId`, `ProductId`, immutable authoritative `SkuSnapshot` (32) and `ProductNameSnapshot` (120), `Quantity INT`, `UnitPrice DECIMAL(19,4)`, and `DiscountPercent DECIMAL(5,2)`.
- Persisted computed `LineSubtotal`, `DiscountAmount`, and `LineTotal`, all `DECIMAL(19,2)` and derived by the AD-08 formula.
- Checks for positive quantity, nonnegative unit price, and discount from 0 through 100.
- `UQ_QuoteLine_QuoteId_ProductId` prevents repeated products; index `(QuoteId, Id)` provides deterministic line order. Add an explicit `LineNumber` only if the frontend contract later needs user-controlled ordering.

#### `QuoteStatusHistory`

- `Id`, `QuoteId`, `StatusId`, `CreatedByUserId`, and `CreationDate` only.
- `NO ACTION` FKs and indexes on `(QuoteId, CreationDate, Id)`.
- The initial Draft row and every accepted transition are inserted atomically with the current `Quote.StatusId`. No generic update/delete repository is generated.

#### `QuotePricing` view

- Schema-bound grouped projection by `QuoteId` returning subtotal, discount total, and grand total as sums of persisted line values, with zero-safe values for repository composition.
- This is the authoritative aggregate-pricing read shape; callers cannot write it.

### Sales orders

#### `SalesOrderStatus`

- Assigned IDs and codes: `Draft=1`, `Confirmed=2`, `Processing=3`, `Shipped=4`, `Completed=5`, `Cancelled=6`.
- Rerunnable authoritative seed with exact `SalesOrderStatus` .NET enum parity and immutable IDs.

#### `SalesOrder`

- `Id`, immutable `OrderNumber` (20), nullable `SourceQuoteId`, `CustomerId`, `ShippingAddressId`, `StatusId`, nullable `RequestedShipDate DATE`, nullable `CarrierId`, and nullable bounded `TrackingNumber` (200).
- Customer and shipping-address snapshot columns matching Quote. Direct drafts may initially have incomplete snapshots; confirmation requires a complete authoritative snapshot in Provider validation. Converted orders receive copied quote snapshots immediately.
- Standard audit columns, `DeletionDate`, `DeletedByUserId`, and `RowVersion`.
- `UQ_SalesOrder_OrderNumber`; filtered unique index on non-null `SourceQuoteId` guarantees exactly one order per quote while allowing many direct orders.
- Checks pair tracking/carrier requirements for persisted Shipped/Completed states where row-local enforcement is possible, require tombstone actor/date pairing, and reject blank stored tracking numbers. Carrier active status and transition legality remain Provider checks.
- Search/filter/sort indexes on order number, `(StatusId, Id)`, `(CustomerId, Id)`, `(SourceQuoteId)`, `(RequestedShipDate, Id)`, and tracking number.
- A SQL sequence supplies values formatted later as `SO-00000001`.

#### `SalesOrderLine`

- Same product FK, snapshot, quantity, price, discount, computed amount, uniqueness, validation, and deterministic-order strategy as QuoteLine.
- Conversion copies QuoteLine inputs/snapshots, not recalculated current Product data.

#### `SalesOrderStatusHistory`

- `Id`, `SalesOrderId`, `StatusId`, `CreatedByUserId`, and `CreationDate`; append-only and non-cascading.
- Initial Draft and every accepted transition are written atomically with current status.

#### `SalesOrderPricing` view

- Schema-bound grouped authoritative totals using rounded line amounts, parallel to `QuotePricing`.

### Integrity ownership not expressible as a row constraint

The database owns keys, FKs, allowed scalar ranges, uniqueness, row versions, default-address cardinality, and single-order-per-quote. Domain/Providers later own rules needing current related state: active customer/product/carrier, address belonging/usage, no duplicate command line before mutation, minimum one line, legal workflow transition, unchanged inactive products, full validity on send/confirm, snapshot refresh/freeze, tombstone eligibility, and retry semantics. Those Providers must use explicit SQL transactions; repositories never decide them.

## Database project and generation steps

1. Create `src/database/BeaconAr.Database.sqlproj` with the approved stable Microsoft.Build.Sql SDK. Add it to root `BeaconAr.slnx`; do not create another solution.
2. Create one file per object in the capability layout. Exclude `scripts/prepredeployment`, included post-deploy fragments, verification, maintenance, and optional baseline files from model compilation.
3. Register exactly one DACPAC PreDeploy and PostDeploy root. `PostDeployment.sql` includes AddressType, IdempotencyState, QuoteStatus, and SalesOrderStatus seed scripts in FK-safe order. Seeds are rerunnable and update by assigned ID/code without deleting missing rows.
4. Keep `PrePreDeployment.sql` present and idempotent even when initially empty of compatibility operations. DatabaseBootstrap executes it with a SQLCMD-compatible runner after DACPAC build and optional managed-empty baseline import, but before SqlPackage plan generation.
5. Do not commit a BACPAC in this task. Preserve `bootstrap/` as the documented optional location for at most one safe non-production baseline.
6. Build and strictly validate the SQL project before generation.
7. Configure EF Core Power Tools/T4 deterministically for `ReceivablesDbContext`, integer IDs, nullable reference types, selected tables/views, capability namespaces, and generated output. Generated headers must clearly mark replaceable files.
8. Add handwritten partial entities/configurations for behavior or mappings that generation cannot own. Never hand-edit generated context/entities/interfaces/serializer contexts.
9. Regenerate once from the disposable published database, review the entire diff, build immediately, and ensure exact table/status/rowversion mappings.

## Foundational domain contracts

Implement only the primitives needed before feature slices:

- `QuoteStatus` and `SalesOrderStatus` enums with exact seed parity.
- `IdempotencyState` enum with exact seed parity.
- An internal/write-side `AddressType` enum or value object mapped to assigned IDs, plus public read code as string for forward-compatible legacy values.
- `MonetaryRounding` with scale 2 and `MidpointRounding.AwayFromZero`, and tests proving boundary behavior. It must match the SQL expressions.
- Getter-only interfaces for generated auditable/versioned entities. Use `EntityBase<int>` and the framework's `IAuditableEntity<DateTimeOffset,int>` contract; do not invent a second identity/audit hierarchy.
- A small immutable concurrency value/codec may live in Domain only if it has no HTTP dependency; the later WebApi task owns ETag headers and base64 formatting.
- Entities expose private/protected mutation and intention-revealing methods. Foundation-generated persistence setters are a documented generator exception; behavior belongs in handwritten partials.
- No `PagedResult`, request DTO, Problem Details, controller model, claims principal, or ASP.NET type enters Domain. Those contracts are added with their owning feature/API task.

Domain tests construct primitives directly and do not require EF, ASP.NET, or the full container.

## Aspire and configuration plan

1. Initialize the current official Aspire workflow skills/tools during implementation; do not vendor them into this repository.
2. Generate/validate root `aspire.config.json` with the current Aspire CLI. AppHost resolves the example root from that rooted file and loads `.env` before `DistributedApplication.CreateBuilder(args)`.
3. Parse `.env` conservatively: process environment wins; reject malformed/duplicate keys; never expand variables, log values, or overwrite existing process variables.
4. Commit placeholders only in `.env.example` for:
   - `Database__Provider=SqlServer`
   - `Database__Mode=Managed`
   - `Database__Name=BeaconAr`
   - `Database__Password=<set-locally>`
   - `Database__PublishOnStart=true`
   - `ConnectionStrings__DatabaseConnection=<required-only-for-external>`
5. `.env` must be ignored and untracked. Keep safe non-secret defaults in appsettings; use environment/user secrets for identity configuration and SPA origins.
6. Managed mode creates a persistent SQL Server container/data volume and a database resource explicitly mapped to `ConnectionStrings__DatabaseConnection` for bootstrap and WebApi.
7. DatabaseBootstrap uses bounded connection retry, cancellation, two-minute readiness default, fifteen-minute import/publish process limits, secret-free structured logs, and nonzero failure propagation. It proves managed-database emptiness before optional import, builds exactly one expected DACPAC into a known artifact directory, runs pre-pre, generates a deployment report/plan, publishes without disabling data-loss protection, and probes `[dbo].[Product]` plus the seeded status catalogs.
8. External mode uses only a secret connection-string resource, never imports a baseline, and skips bootstrap unless publication is explicitly enabled.
9. `start.sh` supports `start` (foreground/default), `stop`, `doctor`, and `help`; checks .NET 10 plus Docker command/daemon; restores pinned local tools; and addresses the explicit AppHost path.
10. WebApi calls `AddServiceDefaults()` before build and maps `/health` for dependency-aware readiness and `/alive` for process-only liveness. Neither returns dependency/configuration details.

## Implementation sequence

### 1. Scaffold safely

1. Pack the current Paradigm `1.1.0` packages/CLI into repository `artifacts` using the existing quality workflow commands.
2. Install the packed CLI into a temporary local tool location or manifest; do not add it globally.
3. Run the scaffold dry-run against the sibling reviewed template with name `BeaconAr`, version `1.1.0`, and an empty ignored staging directory. Review the complete inventory.
4. Run the real scaffold into that staging directory, then move its `src`, `start.sh`, and relevant settings into `examples/beacon-ar/` without overwriting `docs/`.
5. Drop the staged nested GitHub workflow/matcher and integrate checks into the repository-root workflow instead.
6. Restore/build the untouched scaffold before adding database/Aspire/foundation code. Record any template defect rather than carrying sample middleware or incompatible package pins silently.

### 2. Establish build/dependency policy

1. Add example-local central package management, package locks, strict nullable/analysis defaults, and the root Paradigm version import.
2. Normalize all project names/namespaces to `BeaconAr` and package references to the reviewed versions.
3. Remove template sample modules and sample authentication behavior only after replacement composition seams compile.
4. Add test projects and solution folders without reversing dependency direction.

### 3. Build and publish the database

1. Add all schema objects, constraints, indexes, sequences, views, deployment roots, and assigned catalog data described above.
2. Run strict database validation and build the DACPAC.
3. Publish to a disposable local SQL Server and run `FoundationSmoke.sql`.
4. Validate a second publication is idempotent and generates no unintended destructive change.
5. Generate persistence code only from that verified schema, then review/build the generated diff.

### 4. Add foundation primitives and composition

1. Implement status/address/idempotency values, monetary rounding, shared audit/concurrency mappings, and EF configuration partials.
2. Register `SqlServerDbContextConnectionProvider`, `ReceivablesDbContext` with `DatabaseConnection`, and scoped `IUnitOfWork`.
3. Leave repository/provider discovery empty or foundation-only; do not create placeholder CRUD implementations.
4. Replace unsafe template host defaults with minimal explicit configuration and standard health/telemetry plumbing, while leaving business auth/endpoints to the named later task.

### 5. Add Aspire and quality integration

1. Implement the managed/external resource graph and finite bootstrap contract.
2. Update repository CI to pack current Paradigm packages, restore/build/test `examples/beacon-ar/BeaconAr.slnx`, run Paradigm checks, strictly validate/build the SQL project, and run database integration tests when the SQL service is available.
3. Update `examples/README.md` and add a focused Beacon AR README with architecture, prerequisites, start commands, configuration, and explicit incomplete-feature list.

## Deterministic test and validation matrix

### Always-run static/unit checks

- `dotnet restore`, build, and test the solution in Release with no implicit second restore.
- `paradigm packages check`, `packages audit`, `validate`, and `checks run` against `BeaconAr.slnx` using the packed/current 1.1.0 CLI.
- `paradigm database validate --strict` against `BeaconAr.Database.sqlproj` and the same solution.
- `dotnet build` of the SQL project; assert exactly the expected DACPAC is produced.
- Architecture tests assert project references and namespaces follow the dependency direction and that Domain has no ASP.NET/host/concrete-database references.
- Domain tests assert status/idempotency/address assigned IDs, SQL-seed parity, and monetary rounding boundary cases.
- Generation checks rerun the configured generator and fail on a dirty tracked diff.
- Secret scan asserts `.env` is ignored/untracked and placeholders contain no real credential.

### Disposable SQL integration checks

- First bootstrap creates an empty managed database, publishes schema, loads all catalogs, and passes the schema probe.
- Second bootstrap is idempotent and does not re-import or require destructive publication.
- Case-insensitive duplicate SKU/account/carrier codes fail; trimmed/nonblank and numeric/date checks fail deterministically.
- Only one default billing and shipping address can exist per customer under concurrent attempts.
- Referenced master records cannot be physically deleted; no FK cascades business history.
- Duplicate products in quote/order lines fail.
- Duplicate non-null `SourceQuoteId` fails under concurrent attempts while multiple null direct orders succeed.
- `RowVersion` changes on each mutable-root update and a stale EF update raises a concurrency exception.
- Initial/current status rows accept only seeded IDs; status-history rows are not cascade-deleted.
- Line and aggregate pricing matches the .NET midpoint-away-from-zero test vectors.
- Audit and idempotency unique/index/FK constraints behave as designed.
- Managed BACPAC import is skipped when absent and rejected when database emptiness is uncertain; external mode never imports and does not publish without explicit opt-in.
- Health readiness stays unavailable until bootstrap completes; liveness remains process-only; `aspire describe` shows only the canonical generated connection-string mapping and no secret values.

If Docker, SQL Server, Aspire CLI, or SqlPackage is unavailable, complete all static/build checks and list the exact unrun runtime scenarios. Do not claim first-run, restart idempotency, concurrency, external-mode, or publish behavior passed.

## Later task map

The foundation is complete only when the following work is clearly unimplemented and ready to consume it:

1. **`beacon-ar-master-data`** — behavior-rich Product/Customer/Address/Carrier models; database-backed repositories and projections; validation; default-address transaction; CRUD Providers; reference-protected deletion tests.
2. **`beacon-ar-quote-workflow`** — Quote aggregate, snapshots, pricing parity, searches, draft editing/tombstoning, every transition/history/audit transaction, concurrency, and accepted-quote immutability.
3. **`beacon-ar-sales-order-workflow`** — direct order aggregate, quote conversion singleton/idempotency/concurrency transaction, snapshots, fulfillment transitions, carrier/tracking rules, and deletion behavior.
4. **`beacon-ar-dashboard`** — one internally consistent aggregate query and reporting Provider with database-side counts.
5. **`beacon-ar-api-security-contract`** — JWT bearer validation, `/me`, policies, `/api/v1` controllers, paging/filter/sort inputs, safe Problem Details, ETags, idempotency headers, CORS/HTTPS, JSON contexts, and operational route exposure.
6. **`beacon-ar-openapi-client`** — versioned OpenAPI artifact, examples/enums/error schemas, NSwag TypeScript generation, Angular strict compile, and resolution of all documented client-contract differences.
7. **`beacon-ar-end-to-end`** — authenticated API integration suite for all ten acceptance scenarios, performance baseline, logs/traces/metrics assertions, and deployment-runbook verification.

Each later workflow task must name its transaction boundary and write resource changes, status history where applicable, `AuditLog`, and idempotency completion in the same database transaction. A Provider owns that decision; repositories only stage persistence.

## Foundation acceptance checklist

- `examples/beacon-ar/BeaconAr.slnx` targets .NET 10 and builds from repository-packed Paradigm 1.1.0 packages.
- The solution contains all layer, host, bootstrap, database, and test projects with no forbidden reverse references.
- The SQL project represents every release-one table/relationship/integrity mechanism in this plan, validates strictly, builds a DACPAC, and publishes twice to a disposable SQL Server without drift.
- Catalog seeds and .NET values have exact numeric/code parity and remain rerunnable without deleting published IDs.
- All mutable resource roots have rowversion; all auditable resources have the coherent audit-field set; all mutation categories can write the append-only audit log.
- Quote/order histories preserve initial states and are non-cascading; draft deletion uses the documented tombstone lifecycle.
- Historical customer/address/product snapshots and source-quote uniqueness are structurally possible and protected from master-data deletion.
- Database-computed line values and pricing views match .NET rounding tests.
- Aspire waits for a finite, successful schema bootstrap before WebApi readiness, preserves local data, and treats external databases safely.
- `.env` and secrets are absent from Git/logs; health endpoints are cheap and secret-free.
- Repository CI and local commands cover restore, build, unit/architecture tests, package checks/audit, Paradigm validation, strict database validation, SQL build, and the documented runtime smoke suite.
- No master-data CRUD, quote/order use case, dashboard endpoint, or public business controller has leaked into the foundation task.

## Rollback and compatibility boundary

The foundation has no production deployment or stored compatibility obligation yet. If it cannot restore/build/publish cleanly, remove the new `examples/beacon-ar/` solution and its root CI/catalog references as one unit; do not add forwarding projects, duplicate solutions, EF migrations, runtime schema creation, or disabled data-loss checks. Once a later task publishes stable status IDs or exercises persisted sample data, those identifiers and database changes become compatibility boundaries and must be evolved through reviewed DACPAC transitions rather than renumbered or recreated.
