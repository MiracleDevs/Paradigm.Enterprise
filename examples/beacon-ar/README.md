# Beacon AR

Beacon AR is a complete .NET 10 and SQL Server example built on Paradigm.Enterprise 1.1.0. It exposes durable master-data, quote, conversion, sales-order, and dashboard workflows through an authenticated REST API for an Angular accounts-receivable client.

The solution is a modular monolith with inward dependencies: `WebApi -> Providers -> Data -> Domain -> Interfaces`. Entities own intrinsic invariants, normalization, and lifecycle transitions; search/write request objects own their complete query or proposed-input rules. Providers invoke those behaviors and own collaborator-dependent checks, transactions, audit facts, concurrency, idempotency, and cross-aggregate orchestration. Controllers own routing, binding, authorization, HTTP preconditions, and response concerns. The SQL project in `src/database` is the schema source; runtime schema creation and cascading business-history deletes are intentionally absent.

The canonical solution is `BeaconAr.slnx` at the example root. It organizes the unchanged physical project paths under `00.SolutionItems`, `01.Shared`, `02.Modules`, `03.Hosts`, `04.Tools`, and `05.Tests`. These solution folders classify responsibility and do not change the inward dependency graph. The SQL Server schema project remains owned by the application modules at `src/database/BeaconAr.Database.sqlproj`.

## Database read projections

Every database view ends in `View`. Consumer-facing entity or transactional tables use schema-bound `{Entity}View` projections that expose the entity's complete mapping surface and expand foreign keys with commonly needed names, codes, and display values while preserving one row per base entity. `SearchQuote` and `SearchSalesOrder` query these projections directly, establishing the database DTO contract.

EF Core Power Tools generates matching entity/view types into four capability-owned models: `AccessDbContext`, `MasterDataDbContext`, `OperationsDbContext`, and `SalesDbContext`. Every table/view and generated CLR type belongs to exactly one context. Cross-context SQL Server foreign keys remain authoritative database constraints and are represented by scalar IDs rather than duplicated EF navigations. The Domain analyzer derives a getter-only `I{Entity}` scalar contract from each mapped table entity, and both the entity and its `{Entity}View` implement that interface. Repositories materialize canonical entity/view shapes. Providers return those view contracts and coordinate behavior; they do not own intrinsic validation or parallel entity-to-view DTO mapping. MasterData uses explicit command-oriented `IProvider` contracts because the current generic view-mutation base cannot express its allow-listed commands, ETags, audit, cancellation, and transaction protocol. See [Provider and API decisions](docs/beacon-ar-provider-api-conventions/decisions.md).

`QuoteView` and `SalesOrderView` are the public pricing projections and expose the schema's `Subtotal`, `DiscountTotal`, and `GrandTotal` values. Their schema-bound `QuotePricingView` and `SalesOrderPricingView` helpers are database implementation details rather than standalone API DTOs. `AuditLogView`, `IdempotencyRequestView`, and `IdempotencyStateView` provide the explicitly required Operations projections, but remain internal: no controller, serializer context, OpenAPI component, or generated client exposes them or their raw hash/metadata fields.

The SQL Server project is `src/database/BeaconAr.Database.sqlproj` and is included in root `BeaconAr.slnx` under `02.Modules`. Its Microsoft.Build.Sql item groups explicitly include tables, views, functions, routines, types, sequences, and deployment/support scripts so the full schema is visible when the solution is opened in Visual Studio.

## Database-first regeneration

The deterministic regeneration path is the repository-local EF Core Power Tools CLI, pinned as `ErikEJ.EFCorePowerTools.Cli` 10.1.1386 in `.config/dotnet-tools.json`. Four checked-in configurations—`efcpt-access-config.json`, `efcpt-masterdata-config.json`, `efcpt-operations-config.json`, and `efcpt-sales-config.json`—select disjoint sets that together cover all 17 persistence tables and all 14 views. They contain no connection strings. The EF Core 10 T4 templates under `src/BeaconAr.Data/CodeTemplates/EFCore` are derived from the official `Paradigm.Web.ApiTemplate` revision recorded in `PROVENANCE.md`; their local delta is limited to reusable current-generic API and generated-ownership adaptations. Application relationship corrections and domain behavior live in partial files, not type-name lists inside T4.

Build and publish `src/database/BeaconAr.Database.sqlproj` to a disposable SQL Server database before regenerating. Live-database generation is required because DACPAC reverse engineering can omit view/computed-column metadata. Supply that disposable connection only through the process environment, then run:

```powershell
$env:ConnectionStrings__DatabaseConnection = '<disposable SQL Server connection>'
./build/regenerate-persistence.ps1
```

The script restores the local tool, validates the four disjoint configurations and official-derived templates, backs up the exact 31 generated entity/view files and four generated contexts, runs EFPT for Access, MasterData, Operations, and Sales, validates ownership metadata, builds, repeats generation, and requires byte-stable output. On any failure it restores all four boundaries. Handwritten behavior partials are co-located beside their generated halves, so the script replaces exact filenames and never clears an `Entities` or `Context` directory. Do not hand-edit generated files; review the complete generated diff after every run. The Visual Studio extension is optional convenience, not the source of truth.

Production repositories contain no handwritten SQL query or command strings. Use ordinary EF for bounded CRUD/read work and typed SQL Server stored procedures for locking, cross-capability reference checks, sequence allocation, reporting, complex multi-entity work, or performance-sensitive paths. The four contexts are registered against the same scoped `DatabaseConnection`; workflows that stage more than one context require an explicit Unit of Work transaction because sequential saves alone are not atomic.

## Prerequisites and local start

- .NET SDK 10.0.302 or a compatible later patch
- Docker using Linux containers
- Bash
- Node.js compatible with the pinned Angular packages when validating the generated client
- repository-packed Paradigm packages in `../../artifacts` for a source checkout

Copy `.env.example` to `.env`, replace placeholders, then run:

```bash
dotnet tool restore --add-source ../../artifacts
dotnet restore BeaconAr.slnx --property:RestoreAdditionalProjectSources=../../artifacts
./start.sh doctor
./start.sh start
```

Managed mode provisions SQL Server, builds the repository-owned database-bootstrap image, publishes the DACPAC through that finite container, and starts the API after publication. The image owns SQLCMD and SqlPackage, so neither tool is a workstation prerequisite. External mode requires `ConnectionStrings__DatabaseConnection`. `/alive` is process liveness and `/health` is dependency readiness; both intentionally expose only status.

## API and security

All business routes live under `/api/v1` and require a signed delegated-user OIDC/OAuth 2.0 bearer access token. Configure Microsoft Identity Web with the non-secret `AzureAd__Instance`, `AzureAd__TenantId`, and `AzureAd__ClientId` values shown in `.env.example`. `business.read` protects reads and `business.write` protects mutations and implies read. The resource server validates issuer, audience, signature, and lifetime, accepts explicit `idtyp=user` or requires delegated `scp` when `idtyp` is absent, and rejects roles-only client-credentials tokens before user provisioning. Exact delegated scopes or roles carried by an already-classified user can grant permissions. `/api/v1/me` resolves the validated `(issuer, subject)` to the local audit identity. Anonymous `GET /` returns only the API name and assembly product version.

Mutable resources return a strong `ETag`; updates, deletes, and transitions require it in `If-Match`. The six creation routes accept `Idempotency-Key`; only its SHA-256 hash and a canonical request fingerprint are stored in the SQL ledger in the same transaction as the resource and audit write. Quote conversion is the idempotent singleton `PUT /api/v1/quotes/{id}/sales-order`.

Expected failures use RFC 7807 `application/problem+json` with stable `code` and W3C `correlationId`. JSON names are camel case, enums are camel-case strings, calendar dates are `YYYY-MM-DD`, UTC timestamps end in `Z`, and monetary values remain JSON decimals.

## OpenAPI and Angular client

Run the isolated generator to refresh the checked versioned OpenAPI 3.0 document at `artifacts/openapi/beacon-ar-v1.json`. Swagger JSON at `/openapi/v1.json` and Swagger UI at `/swagger` are exposed only in Development. Generate the NSwag Angular client and compile its strict consumer with:

```bash
./scripts/generate-openapi.ps1
dotnet run --project src/BeaconAr.CodeGenerator/BeaconAr.CodeGenerator.csproj --configuration Release -- openapi-typescript ../../artifacts/openapi/beacon-ar-v1.json ../../tests/BeaconAr.ClientContract/generated/beacon-ar-v1.ts
npm ci --prefix tests/BeaconAr.ClientContract
npm run check --prefix tests/BeaconAr.ClientContract
```

## Verification

```bash
dotnet build BeaconAr.slnx --configuration Release
dotnet test --solution BeaconAr.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 1
dotnet tool run paradigm validate --project BeaconAr.slnx
dotnet tool run paradigm checks run --project src/BeaconAr.Domain/BeaconAr.Domain.csproj
dotnet tool run paradigm checks run --project src/BeaconAr.Data/BeaconAr.Data.csproj
dotnet tool run paradigm checks run --project src/BeaconAr.Providers/BeaconAr.Providers.csproj
```

After publishing a disposable database, set `ConnectionStrings__DatabaseConnection` and run both database and authenticated HTTP acceptance suites with `--filter TestCategory=Integration`.

Implementation records: [context boundaries](docs/beacon-ar-context-boundaries/implementation-plan.md), [foundation](docs/beacon-ar-foundation/implementation-plan.md), [master data](docs/beacon-ar-master-data/implementation-plan.md), [sales workflows](docs/beacon-ar-sales-workflows/implementation-plan.md), [API contract](docs/beacon-ar-api-contract/implementation-plan.md), and [framework-aligned Web API integration](docs/beacon-ar-web-api/implementation-plan.md). Operational configuration and incident steps are in the [API runbook](docs/beacon-ar-api-contract/runbook.md); current persistence decisions are recorded in the [context-boundary decisions](docs/beacon-ar-context-boundaries/decisions.md).
