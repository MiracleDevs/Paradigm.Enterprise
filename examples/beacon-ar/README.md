# Beacon AR

Beacon AR is a complete .NET 10 and SQL Server example built on Paradigm.Enterprise 1.1.0. It exposes durable master-data, quote, conversion, sales-order, and dashboard workflows through an authenticated REST API for an Angular accounts-receivable client.

The solution is a modular monolith with inward dependencies: `WebApi -> Providers -> Data -> Domain -> Interfaces`. Providers own transactions, audit facts, validation, lifecycle transitions, snapshots, concurrency, and creation idempotency. Controllers only own HTTP concerns. The SQL project in `src/database` is the schema source; runtime schema creation and cascading business-history deletes are intentionally absent.

## Database read projections

Every consumer-facing entity or transactional table has a schema-bound `{Entity}View` projection. These views expose the entity's complete mapping surface and expand foreign keys with commonly needed names, codes, and display values while preserving one row per base entity. `SearchQuote` and `SearchSalesOrder` now query these projections directly, establishing the database DTO contract.

The intended application boundary is that EF Core Power Tools generates matching entity/view types and their shared domain interfaces, repositories retrieve entities or view shapes, and Providers own entity-to-view mapping and validation orchestration. That generated model/context work, detail-repository adoption of the views, and provider mapping remain pending Tasks 2 and 3; the current detail repositories still use the existing entity/pricing projections.

`QuoteView` and `SalesOrderView` are the public pricing projections and expose the schema's `Subtotal`, `DiscountTotal`, and `GrandTotal` values. Their schema-bound pricing helpers are database implementation details rather than standalone API DTOs. Status catalogs, audit history, idempotency storage, and other internal tables do not receive API views unless a concrete consumer requires one.

The SQL Server project is `src/database/BeaconAr.Database.sqlproj` and is included in `src/BeaconAr.sln`. Its Microsoft.Build.Sql item groups explicitly include tables, views, functions, routines, types, sequences, and deployment/support scripts so the full schema is visible when the solution is opened in Visual Studio.

## Prerequisites and local start

- .NET SDK 10.0.302 or a compatible later patch
- Docker using Linux containers
- Bash
- Node.js compatible with the pinned Angular packages when validating the generated client
- repository-packed Paradigm packages in `../../artifacts` for a source checkout

Copy `.env.example` to `.env`, replace placeholders, then run:

```bash
dotnet tool restore --add-source ../../artifacts
dotnet restore src/BeaconAr.sln --property:RestoreAdditionalProjectSources=../../artifacts
./start.sh doctor
./start.sh start
```

Managed mode provisions SQL Server, builds the repository-owned database-bootstrap image, publishes the DACPAC through that finite container, and starts the API after publication. The image owns SQLCMD and SqlPackage, so neither tool is a workstation prerequisite. External mode requires `ConnectionStrings__DatabaseConnection`. `/alive` is process liveness and `/health` is dependency readiness; both intentionally expose only status.

## API and security

All business routes live under `/api/v1` and require a signed OIDC/OAuth 2.0 bearer access token. `business.read` protects reads and `business.write` protects mutations and implies read. The resource server validates issuer, audience, signature, and lifetime, then maps exact `scp` or `roles` values. `/api/v1/me` resolves the validated `(issuer, subject)` to the local audit identity.

Mutable resources return a strong `ETag`; updates, deletes, and transitions require it in `If-Match`. The six creation routes accept `Idempotency-Key`; only its SHA-256 hash and a canonical request fingerprint are stored in the SQL ledger in the same transaction as the resource and audit write. Quote conversion is the idempotent singleton `PUT /api/v1/quotes/{id}/sales-order`.

Expected failures use RFC 7807 `application/problem+json` with stable `code` and W3C `correlationId`. JSON names are camel case, enums are camel-case strings, calendar dates are `YYYY-MM-DD`, UTC timestamps end in `Z`, and monetary values remain JSON decimals.

## OpenAPI and Angular client

Builds emit the versioned OpenAPI 3.0 document at `artifacts/openapi/beacon-ar-v1.json`. Generate the NSwag Angular client and compile its strict consumer with:

```bash
dotnet build src/BeaconAr.WebApi/BeaconAr.WebApi.csproj --configuration Release
dotnet run --project src/BeaconAr.CodeGenerator/BeaconAr.CodeGenerator.csproj --configuration Release -- openapi-typescript ../../artifacts/openapi/beacon-ar-v1.json ../../tests/BeaconAr.ClientContract/generated/beacon-ar-v1.ts
npm ci --prefix tests/BeaconAr.ClientContract
npm run check --prefix tests/BeaconAr.ClientContract
```

## Verification

```bash
dotnet build src/BeaconAr.sln --configuration Release
dotnet test --solution src/BeaconAr.sln --configuration Release --no-build --no-restore --minimum-expected-tests 1
dotnet tool run paradigm validate --project src/BeaconAr.sln
dotnet tool run paradigm checks run --project src/BeaconAr.Domain/BeaconAr.Domain.csproj
dotnet tool run paradigm checks run --project src/BeaconAr.Data/BeaconAr.Data.csproj
dotnet tool run paradigm checks run --project src/BeaconAr.Providers/BeaconAr.Providers.csproj
```

After publishing a disposable database, set `ConnectionStrings__DatabaseConnection` and run both database and authenticated HTTP acceptance suites with `--filter TestCategory=Integration`.

Implementation records: [foundation](docs/beacon-ar-foundation/implementation-plan.md), [master data](docs/beacon-ar-master-data/implementation-plan.md), [sales workflows](docs/beacon-ar-sales-workflows/implementation-plan.md), and [API contract](docs/beacon-ar-api-contract/implementation-plan.md). Operational configuration and incident steps are in the [API runbook](docs/beacon-ar-api-contract/runbook.md).
