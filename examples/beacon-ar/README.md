# Beacon AR

Beacon AR is a complete .NET 10 and SQL Server example built on Paradigm.Enterprise 1.1.0. It exposes durable master-data, quote, conversion, sales-order, and dashboard workflows through an authenticated REST API for an Angular accounts-receivable client.

The solution is a modular monolith with inward dependencies: `WebApi -> Providers -> Data -> Domain -> Interfaces`. Providers own transactions, audit facts, validation, lifecycle transitions, snapshots, concurrency, and creation idempotency. Controllers only own HTTP concerns. The SQL project in `src/database` is the schema source; runtime schema creation and cascading business-history deletes are intentionally absent.

## Prerequisites and local start

- .NET SDK 10.0.302 or a compatible later patch
- Docker using Linux containers
- Bash and Microsoft SQLCMD 18
- Node.js compatible with the pinned Angular packages when validating the generated client
- repository-packed Paradigm packages in `../../artifacts` for a source checkout

Copy `.env.example` to `.env`, replace placeholders, then run:

```bash
dotnet tool restore --add-source ../../artifacts
dotnet restore src/BeaconAr.sln --property:RestoreAdditionalProjectSources=../../artifacts
./start.sh doctor
./start.sh start
```

Managed mode provisions SQL Server, publishes the DACPAC through the finite bootstrap, and starts the API after publication. External mode requires `ConnectionStrings__DatabaseConnection`. `/alive` is process liveness and `/health` is dependency readiness; both intentionally expose only status.

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
dotnet test src/BeaconAr.sln --configuration Release --filter "TestCategory!=Integration"
dotnet tool run paradigm validate --project src/BeaconAr.sln
dotnet tool run paradigm checks run --project src/BeaconAr.WebApi/BeaconAr.WebApi.csproj
```

After publishing a disposable database, set `ConnectionStrings__DatabaseConnection` and run both database and authenticated HTTP acceptance suites with `--filter TestCategory=Integration`.

Implementation records: [foundation](docs/beacon-ar-foundation/implementation-plan.md), [master data](docs/beacon-ar-master-data/implementation-plan.md), [sales workflows](docs/beacon-ar-sales-workflows/implementation-plan.md), and [API contract](docs/beacon-ar-api-contract/implementation-plan.md). Operational configuration and incident steps are in the [API runbook](docs/beacon-ar-api-contract/runbook.md).
