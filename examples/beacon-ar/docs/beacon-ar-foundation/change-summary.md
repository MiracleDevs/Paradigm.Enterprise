# Beacon AR foundation change summary

> Historical record: commands and solution references below describe validation before the migration to root `BeaconAr.slnx`. Use `BeaconAr.slnx` for current work.

## Implemented

- Scaffolded the .NET 10 `BeaconAr` modular monolith with the inward `WebApi -> Providers -> Data -> Domain -> Interfaces` dependency graph, Aspire AppHost, ServiceDefaults, finite DatabaseBootstrap, SQL project, and focused test projects.
- Added the complete release-one SQL Server model, catalog seeds, sequences, schema-bound pricing views, rowversions, status histories, non-cascading foreign keys, tombstone lifecycles, computed pricing, and smoke verification.
- Enforced shipping-address ownership in the schema with a `CustomerAddress(CustomerId, Id)` candidate key and composite quote/order foreign keys. The SQL integration suite proves another customer's address is rejected.
- Applied exact `Latin1_General_100_BIN2` comparison to OIDC issuer and subject identifiers and added an integration test proving case-distinct protocol identities can coexist.
- Made strict database validation authoritative. Mutable `IdempotencyRequest` now has the complete canonical audit quartet; append-only `AuditLog` uses the explicitly documented `RecordedAt` fact instead of presenting itself as a partially auditable mutable entity. CI runs `database validate --strict`.
- Linked database deadlines and every child tool to host shutdown. Cancelled/timed-out tools are killed as an entire process tree and reaped before failure propagates. Process-level tests verify a cancelled descendant cannot survive to write a marker.
- Replaced the ad hoc SQL batch splitter with Microsoft SQLCMD 18. The bootstrap uses native SQLCMD script semantics, passes SQL passwords only through `SQLCMDPASSWORD`, requires `Encrypt=True`, adds certificate trust only when the connection string explicitly requests it, and rejects TLS modes it cannot preserve.
- Added stable structured start/completion/failure events for database wait, baseline decision, DACPAC build, pre-pre-deployment, deploy report, publish, and schema probe phases, including duration and secret-free outcome metadata.
- Preserved OpenTelemetry logging registration by removing the later `ClearProviders()` call from Web API startup.
- Made code generation fail fast. JSON-context and stored-procedure-mapper generation are independently selectable (`json` or `mappers`), unknown targets fail the process, generated directories are replaced atomically only after successful generation, and the deferred proxy generator/configuration/package were removed from the foundation.
- Updated the owning EF Core/source-generator templates to current Paradigm 1.1 signatures: `DbContextBase<int>`, `EntityBase<int,...>`, `IEntity<int>`, `IAuditableEntity<DateTimeOffset,int>`, and `EntityMapperBase<int,...>`. Architecture tests prevent regression to the obsolete forms.
- Published the DACPAC to a disposable LocalDB database and reverse-engineered the checked-in EF Core persistence boundary: 17 table shapes, two keyless pricing views, 19 `DbSet` mappings, rowversions, computed columns, and composite ownership foreign keys. CI regenerates it from the published SQL Server schema and requires a clean diff.
- Corrected SQL Server scaffolding's filtered-index cardinality ambiguity in the owning T4 templates. `FK_CustomerAddress_Customer` now regenerates as a one-to-many relationship with a `CustomerAddresses` collection, and a compiled EF metadata/fixup test prevents regression.
- Bounded live quote/order ownership-test identifiers to the schema's 20-character contract and asserted each named composite foreign key, so the test proves two independent SQL error 547 failures instead of stopping on truncation error 2628.
- Added a disposable SQL Server CI service. CI creates an empty managed database, runs the finite bootstrap, executes the smoke and focused integrity tests without skipping, then runs the bootstrap a second time and fails if its deployment report contains schema operations.

## Verification evidence

Executed from `examples/beacon-ar` on 2026-08-01 unless noted:

- `dotnet restore src/BeaconAr.sln --property:RestoreAdditionalProjectSources=../../artifacts` passed.
- `dotnet build src/BeaconAr.sln --configuration Release --no-restore` passed with 0 warnings and 0 errors and produced `BeaconAr.Database.dacpac`.
- `dotnet test src/BeaconAr.sln --configuration Release --no-build --no-restore` passed: 17 succeeded and 3 SQL integration tests were skipped when no connection string was supplied.
- `build/regenerate-persistence.ps1` completed against the disposable published LocalDB schema and the regenerated solution built with zero warnings and zero errors.
- The focused compiled EF metadata test passed, and the ownership integration test passed live against LocalDB while asserting `FK_Quote_CustomerAddress` and `FK_SalesOrder_CustomerAddress` separately.
- With the disposable LocalDB connection supplied, the complete solution test run passed all 20 tests with no failures or skips.
- The bootstrap test suite covers SQLCMD TLS/secret argument construction, structured phase events, and host-cancellation process-tree cleanup.
- `dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution src/BeaconAr.sln --strict --format json` passed with `status: success` and zero diagnostics.
- `dotnet run --project src/BeaconAr.CodeGenerator/BeaconAr.CodeGenerator.csproj --configuration Release --no-build -- invalid` exited nonzero with the expected unknown-target exception.
- `dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release` passed and produced the expected DACPAC.
- `paradigm packages check` and `paradigm validate` passed for `BeaconAr.sln`; focused C# checks passed for Domain, Data, Providers, and WebApi.
- `paradigm packages audit` exited 0 with update-only warnings for the reviewed template/Aspire pins; it reported no vulnerability or deprecation finding.
- `aspire restore` passed. `aspire doctor` reported 3 checks passed with Docker-not-running and an older development-certificate version as warnings.
- `.env` remains ignored/untracked and `.env.example` contains placeholders only.

## Environmental gaps

- Docker Desktop is installed on the review workstation but its Linux daemon is not running. Local first publication, second-publication idempotency, live `FoundationSmoke.sql`, relational integrity tests, BACPAC behavior, external publication, and Aspire readiness ordering therefore remain unexecuted here. The repository CI now supplies the disposable SQL Server prerequisite and treats those integration tests as a required gate.
- The EF persistence boundary was generated from a DACPAC published to disposable SQL Server LocalDB. Because that workstation engine is SQL Server 2019, publication required `AllowIncompatiblePlatform=True`; the SQL Server 2022 service in CI remains the authoritative live compatibility and regeneration-clean gate.
- Aspire/SqlPackage are locally pinned and restorable. `aspire run/describe/logs` still require the unavailable container daemon, so no local runtime claim is made for first-run or restart readiness.

## Deferred feature work

Master-data CRUD, quote/order Providers and controllers, dashboard queries, bearer-token policy, Problem Details/ETags/idempotency HTTP behavior, OpenAPI/Angular client generation, and authenticated end-to-end scenarios remain owned by the later tasks in the implementation plan.
