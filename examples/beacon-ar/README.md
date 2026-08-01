# Beacon AR foundation

Beacon AR is a .NET 10 modular-monolith example built on Paradigm.Enterprise 1.1.0. This foundation establishes the inward layer graph, SQL Server database-first schema, Aspire local topology, finite database publication, health endpoints, and the small domain contracts shared by later feature slices.

## Prerequisites

- .NET SDK 10.0.302 or a compatible later patch
- Docker with a running Linux-container daemon for managed SQL Server
- Bash for `start.sh`
- Microsoft SQLCMD 18 (`mssql-tools18`) for reviewed SQLCMD-compatible pre-pre-deployment execution
- Repository-packed Paradigm 1.1.0 packages in `../../artifacts` when working from this source checkout

Restore local tools and the solution from this directory:

```bash
dotnet tool restore --add-source ../../artifacts
dotnet restore src/BeaconAr.sln --property:RestoreAdditionalProjectSources=../../artifacts
dotnet build src/BeaconAr.sln --configuration Release --no-restore
dotnet test --solution src/BeaconAr.sln --configuration Release --no-build --no-restore --minimum-expected-tests 1
```

## Local start

Copy `.env.example` to `.env`, replace the database password placeholder, and run:

```bash
./start.sh doctor
./start.sh start
```

Managed mode provisions a persistent SQL Server resource, publishes the DACPAC through the finite bootstrap, and starts the API only after publication succeeds. External mode requires `ConnectionStrings__DatabaseConnection` and does not publish unless `Database__PublishOnStart=true` is explicitly set. The API exposes dependency-aware `/health` and process-only `/alive` endpoints.

## Architecture

The dependency direction is `WebApi -> Providers -> Data -> Domain -> Interfaces`. `ReceivablesDbContext` is the single release-one transaction context. The SQL project under `src/database` is the schema source; no EF migration catalog or runtime schema creation is used. Every database foreign key is non-cascading so business history cannot disappear through aggregate or master-data deletion. The generation templates normalize SQL Server's filtered-index ambiguity so `Customer` consistently owns a `CustomerAddresses` collection rather than a false one-to-one navigation.

The database bootstrap delegates pre-pre-deployment scripts to Microsoft SQLCMD 18, including its native `GO`, include, and variable semantics. SQL credentials are passed through `SQLCMDPASSWORD`, never as process arguments. Bootstrap publication requires `Encrypt=True`; certificate trust is enabled only when `DatabaseConnection` explicitly sets `TrustServerCertificate=True`, and unsupported TLS modes are rejected instead of weakened. `Database__SqlCmdPath` may select the executable, while the bootstrap and `start.sh doctor` enforce the pinned major-version contract. Bootstrap logs use stable structured phase, duration, outcome, and failure-kind fields without connection strings or secret values. The direct dependencies introduced by the approved implementation plan are Microsoft/Aspire components (Apache-2.0 or MIT), Microsoft.Build.Sql/SqlPackage (MIT), Microsoft.Data.SqlClient (MIT), Microsoft.Extensions.Logging.Console (MIT), MSTest (MIT), and the repository's Paradigm packages (MIT). They are centrally pinned; no unrelated runtime dependency was added.

## Deliberately incomplete

This task does not expose public business endpoints. Master-data CRUD, quote and sales-order workflows, dashboard queries, bearer-token policy, Problem Details/ETags/idempotency HTTP behavior, OpenAPI/Angular client generation, and authenticated end-to-end scenarios belong to the later tasks listed in the implementation plan. The checked-in persistence boundary was reverse-engineered from a disposable published database with the pinned EF Core tool. `build/regenerate-persistence.ps1` repeats that process, and CI fails unless regeneration leaves both generated directories clean.

Strict schema validation is the required local gate:

```bash
dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution src/BeaconAr.sln --strict
```

See [the implementation plan](docs/beacon-ar-foundation/implementation-plan.md) and [the validation/change summary](docs/beacon-ar-foundation/change-summary.md).
