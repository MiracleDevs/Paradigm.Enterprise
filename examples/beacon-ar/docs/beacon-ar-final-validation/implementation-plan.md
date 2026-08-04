# Beacon AR final cross-cutting validation plan (Task 5)

> **Historical persistence note (superseded 2026-08-03):** This document records an earlier implementation stage. Current persistence ownership is defined by docs/beacon-ar-context-boundaries: four disjoint Access, MasterData, Operations, and Sales contexts/configurations; there are no Receivables or Reporting application roots.

## Release objective

Perform a release audit of the merged Beacon AR example from the user-request baseline (`8a018ca`) through the current merged Web API head (`aaa5081`), including the supporting guidance. Earlier foundation history may be consulted for provenance, but it is not the user-request audit baseline. The audit is evidence-led: it must distinguish a verified pass, a verified defect with the smallest safe correction, and a not-run check with its exact external limitation.

This task may correct defects and documentation only after each finding is recorded. It must not treat a passing build as proof of database, generated-code, authorization, package, or operational correctness.

## Audit inputs and traceability

Review the final merged history, not only the working tree:

```powershell
git log --oneline --decorate 8a018ca..HEAD -- examples/beacon-ar
git diff --stat 8a018ca..HEAD -- examples/beacon-ar
git diff --check 8a018ca..HEAD -- examples/beacon-ar
git diff --name-status 8a018ca..HEAD -- examples/beacon-ar
git status --short
```

Create `examples/beacon-ar/docs/beacon-ar-final-validation/decisions.md` before accepting the release. It must be a separate record (not a section hidden in a summary) containing: decision, requirement/source, evidence/command and date, chosen outcome, rejected alternative, compatibility/security consequence, owner, and revisit trigger. It must capture at least:

- release-one delegated-user-only Entra actor decision, exact `idtyp`/`scp`/`roles` handling, and why client-credential actors are rejected;
- direct protected `ControllerBase` decision rather than Paradigm generic API controller bases that inherit `[AllowAnonymous]`;
- generated `*View` read-shape and request DTO write-boundary decision;
- custom Quote/SalesOrder/QuoteConversion provider/transaction boundary versus official master-data CRUD bases;
- SQL Server database-first/DACPAC plus EF Core Power Tools generated ownership/byte-stability decision;
- Swashbuckle selection and the exclusion of deprecated `Microsoft.AspNetCore.Authentication.AzureAD.UI`;
- root endpoint/version, Swagger availability, CORS, health, and secret-delivery decisions;
- each live-SQL/Entra/Aspire limitation and the release disposition.

Build a requirement traceability matrix in `review-feedback.md` or `change-summary.md`, with one row per requirement, its owning files/tests, exact validation, outcome, and remaining risk. The matrix must cover schema/database-first generation; views; master-data CRUD; Quote/SalesOrder workflows; API security/ETags/idempotency/OpenAPI/root; TypeScript client; Aspire/bootstrap; diagnostics; and documentation. Link the relevant existing task records:

- `docs/beacon-ar-foundation/`
- `docs/beacon-ar-master-data/`
- `docs/beacon-ar-sales-workflows/`
- `docs/beacon-ar-api-contract/`
- `docs/beacon-ar-database-views/`
- `docs/beacon-ar-ef-power-tools/`
- `docs/beacon-ar-framework-crud/`
- `docs/beacon-ar-web-api/`
- `docs/beacon-ar-final-verification/`

## Reference implementation comparison

Compare the completed example with `C:\Repositories\github\microsoft\rdx-dms-mvp\src\api\`, recording useful precedents and intentional non-adoptions. Inspect its Web API project, Program composition, controller bases, Swagger setup, root route, DI ordering, and authentication packages.

Expected conclusion to prove, not assume:

- retain useful patterns: explicit composition root, DI before convention discovery, Swagger document/UI setup, root endpoint, and `UseAuthentication()` before `UseAuthorization()`;
- do not copy its legacy `Microsoft.AspNetCore.Authentication.AzureAD.UI`, broad CORS, anonymous Paradigm-base inheritance, custom token middleware, or API-key authorization model;
- Beacon AR remains a .NET 10 bearer API using Microsoft Identity Web, narrow CORS, direct protected `ControllerBase` controllers, safe Problem Details, generated views at data/provider boundaries, and a versioned root response.

Document any divergence that lacks a requirement or current-security rationale as a finding, not an aesthetic preference.

## Stale, transitional, and duplicate-surface audit

Run bounded searches (exclude `bin`, `obj`, intentionally generated OpenAPI/TypeScript outputs, and historical task documents unless checking documentation drift):

```powershell
rg -n --glob '!**/bin/**' --glob '!**/obj/**' --glob '!artifacts/**' `
  'MasterDataProviderBase|SalesProviderBase|BearerSecurityDocumentTransformer|AddOpenApi|MapOpenApi|Microsoft\.AspNetCore\.Authentication\.AzureAD\.UI|ReadApiControllerBase|EditApiControllerBase|ApiAuthorizationAttribute|AddEndpointExposureControl' examples/beacon-ar

rg -n --glob '!**/bin/**' --glob '!**/obj/**' `
  'ProductDto|CustomerDto|AddressDto|CarrierDto|ProductView|CustomerView|CustomerAddressView|CarrierView|database-first\.json' examples/beacon-ar/src examples/beacon-ar/tests examples/beacon-ar/README.md

rg -n --glob '!**/bin/**' --glob '!**/obj/**' `
  'Authentication:|AzureAd|SigningKey|Authority|Audience|Issuer|ClientSecrets|AllowAnyOrigin|AllowAnyHeader|AllowAnyMethod' examples/beacon-ar
```

Classify each hit before changing it:

- `MasterDataProviderBase`, `SalesProviderBase`, old built-in OpenAPI transformer/registration, and AzureAD UI package references must be absent except in historical/task documentation that expressly records their removal.
- Generated master-data views are canonical application read shapes. Legacy `ProductDto`, `CustomerDto`, `AddressDto`, and `CarrierDto` may remain only as intentional, tested HTTP compatibility adapters; no stale duplicate mapper, repository read projection, OpenAPI schema, JSON context, or client contract may disagree with a generated view.
- `database-first.json` must be removed or explicitly documented as a non-executed legacy artifact; `efcpt-config.json` and T4 templates are the authoritative generation inputs.
- Search for duplicate names/types in compiled source and ensure exactly one intended provider/repository interface/implementation is discovered by conventions.
- Search README, `.env.example`, AppHost/bootstrap docs, API runbook, and task summaries for stale routes, package names, auth keys, generation paths, old test commands, or secret-bearing examples.

Fix stale references by removing obsolete code/config/docs, or document a deliberate compatibility shim with its test and removal trigger. Do not keep a parallel abstraction merely because tests compile.

## SQL Server project, schema, and generated persistence audit

### Project and Visual Studio metadata

Inspect:

- root `BeaconAr.slnx` and `src/database/BeaconAr.Database.sqlproj`;
- `src/database/scripts/{prepredeployment,predeployment,postdeployment,verification}`;
- `src/BeaconAr.DatabaseBootstrap/{Dockerfile,Program.cs,BeaconAr.DatabaseBootstrap.csproj}`;
- `src/BeaconAr.AppHost/Program.cs`, `.env.example`, `aspire.config.json`, `start.sh`, and database/bootstrap tests.

Prove the SQL project is included in the application solution, uses `Microsoft.Build.Sql`, targets `Sql160`, has a single explicit non-duplicated SQL item strategy (`EnableDefaultSqlItems=false` with Build/None/PreDeploy/PostDeploy items), and keeps design-time folders/metadata visible to Visual Studio. Verify no `.sqlproj.user`, `.suo`, `.dacpac`, BACPAC, publish profile, or machine-specific Visual Studio artifact is committed. Confirm pre-pre deployment bootstrap runs before DACPAC planning and that host execution never requires workstation SQLCMD/SqlPackage.

Run:

```powershell
Set-Location examples/beacon-ar
dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release
dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution BeaconAr.slnx --strict --format json
```

### Database views and joins

Review every `src/database/views/**/*.sql` alongside its generated entity/view and consumer repository. For `ApplicationUserView`, `ProductView`, `CustomerView`, `CustomerAddressView`, `CarrierView`, `QuoteView`, `QuoteLineView`, `SalesOrderView`, `SalesOrderLineView`, `QuotePricingView`, and `SalesOrderPricingView`, verify:

- schema binding, fully qualified references, column names/types/nullability, keys/rowversion, and EFPT selection;
- one-row-per-root/view cardinality and no multiplicative joins;
- required joins versus `LEFT JOIN` for optional source quote/carrier/audit/deletion users;
- customer/address/product/carrier snapshot versus live descriptive-field intent;
- price views aggregate rounded stored line values and preserve zero-line behavior;
- names agree across SQL, generated entities, mapper/repository contracts, provider output, OpenAPI, and TypeScript client;
- stored-procedure search result ordering, paging metadata-before-rows convention, deterministic ID tie-breaks, tombstone exclusion, and approved sort allow-lists.

Use SQL Server integration tests and a disposable database to validate actual view results/joins—not only static SQL review.

### EF Core Power Tools and regeneration

Inspect `src/BeaconAr.Data/efcpt-config.json`, `CodeTemplates/EFCore/{DbContext,EntityType}.t4`, `build/regenerate-persistence.ps1`, `.config/dotnet-tools.json`, all generated entity/context output, and `GeneratedPersistenceTests.cs`/`GeneratedMapperTests.cs`.

Acceptance requires all of the following:

- configuration names the correct tables/views, excludes unsupported stored-procedure scaffolding intentionally, uses the committed T4 templates, contains no connection string/credentials, and writes only the owned generated directories;
- generated files start with the expected auto-generated ownership marker and emit `GeneratedCode("EFCorePowerTools", "10.1.1386")` metadata;
- handwritten behavior is partial/outside generated files; no generated file contains business behavior or manual edits;
- the script validates exact expected files, backs up/restores safely, redacts connection-string failures, and passes `-TestFailureRecovery` and `-TestRedaction`;
- after a database build against the same disposable schema, execute regeneration twice and require identical SHA-256 outputs and no `git diff` after the second pass:

```powershell
$env:ConnectionStrings__DatabaseConnection = '<disposable database supplied out of band>'
./build/regenerate-persistence.ps1
git diff --exit-code -- src/BeaconAr.Domain/Receivables/Entities src/BeaconAr.Data/Receivables/Context
./build/regenerate-persistence.ps1
git diff --exit-code -- src/BeaconAr.Domain/Receivables/Entities src/BeaconAr.Data/Receivables/Context
./build/regenerate-persistence.ps1 -TestFailureRecovery
./build/regenerate-persistence.ps1 -TestRedaction
```

Never run regeneration against an unknown shared/production database. Restore the caller's environment value after the audit and never write it to logs, source, generated output, or task records.

## Layer, provider, repository, and transaction audit

Inspect the architecture tests plus `Domain`, `Data`, `Providers`, and `WebApi` project references. Verify `Interfaces <- Domain <- Data <- Providers <- WebApi`, with AppHost/bootstrap only at the composition/operations edge. Run targeted searches for forbidden dependencies (`Microsoft.AspNetCore` outside WebApi, `DbContext`/EF/SQL in Providers, repositories/contexts in controllers, `IQueryable` in public repository interfaces, `ActionResult`/HTTP types below WebApi).

For Product, Customer, CustomerAddress, and Carrier, prove—not merely by ancestry—that:

- provider interfaces inherit `IEditProvider<TView,int>` and repository contracts inherit the official typed read/edit contracts;
- implementations use `ReadProviderBase`/`EditProviderBase`, `ReadRepositoryBase<TView,ReceivablesDbContext,int>`, and `EditRepositoryBase<TEntity,ReceivablesDbContext,int>` at the correct boundary;
- mapping/validation live in provider hooks, repositories only query/stage, generic edit operations own the commit, and every inherited single/bulk operation is exercised through its interface or explicitly fail-closed by `OfficialMasterDataMutationGuard` with a documented HTTP/concurrency decision;
- custom stored-procedure search overrides return generated view shapes and preserve bounded filtering/sorting/paging.

For Quote, SalesOrder, and QuoteConversion, prove the contrary boundary:

- no generic CRUD base is forced onto workflows that need aggregate lines, status history, audit, idempotency, snapshotting, conversion singleton semantics, concurrency, and explicit transaction ownership;
- `SalesProviderBase` is gone; `ProviderBase` or direct `IProvider` uses direct constructor-injected collaborators only;
- transaction creation, commit/rollback, persistence-session clearing, conflict translation, root rowversion advancement, histories, and audit records are all tested through failure/race paths.

When an invariant is repeated and a deterministic check can observe it (for example generic-provider operations left callable without tests, or a protected controller inheriting anonymous metadata), record a minimal fixture, false-positive boundary, stable diagnostic design, and the candidate `paradigm validate`/`paradigm checks` rule in `decisions.md`. Do not implement framework guidance/tooling in this release unless the evidence is a recurring, general practice rather than a one-off app repair.

## Web API, security, root, and OpenAPI audit

Inspect `BeaconAr.WebApi/Program.cs`, controllers, security requirements/handlers, access middleware, exception/ETag/idempotency helpers, JSON context, OpenAPI filters, appsettings, and every Web API test.

Validate these release conditions through the real middleware pipeline:

- all business routes remain direct `ControllerBase` endpoints with no inherited `IAllowAnonymous`; only deliberate root/health/development documentation routes are anonymous;
- no `ReadApiControllerBase`/`EditApiControllerBase` inherited `search`, `get-by-id`, `save`, or `delete` routes are mapped; endpoint exposure control is not incorrectly treated as authorization;
- release one accepts delegated-user Entra tokens only: explicit `idtyp=user`, or legacy fallback `scp` present; roles-only/app-only/no-scope shapes are rejected before user provisioning; never use `oid == sub` to classify an actor;
- exact `business.read`/`business.write` scope/role policy matrix, write-implies-read, safe 401/403, no PII/token logging, production missing-config failure, and test-only signing-key isolation pass;
- master-data response shapes match generated views or an explicitly approved compatibility adapter; write requests are narrow DTOs; Quote/Sales Order continue named custom commands;
- ETags, required strong `If-Match`, idempotent creation, request-size/content-type limits, CORS, HTTPS, trace/problem correlation, root name/version, liveness/readiness, and unexpected-error redaction preserve their documented behavior;
- Swagger/OpenAPI is generated by a single Swashbuckle pipeline. The artifact has stable `v1` operations/routes/IDs, correct bearer/anonymous security split, schemas/examples/headers/problem responses, and source-generated JSON coverage. Development-only UI/document routing does not imply anonymous production documentation.

Run the existing Web API acceptance, security-matrix, host-hardening, transport, discovery/documentation, generated mapping, OpenAPI artifact, and TypeScript client tests after regenerating the reviewed artifact:

```powershell
./scripts/generate-openapi.ps1
dotnet run --project src/BeaconAr.CodeGenerator/BeaconAr.CodeGenerator.csproj --configuration Release --no-build -- openapi-typescript ../../artifacts/openapi/beacon-ar-v1.json ../../tests/BeaconAr.ClientContract/generated/beacon-ar-v1.ts
npm ci --prefix tests/BeaconAr.ClientContract
npm run check --prefix tests/BeaconAr.ClientContract
git diff --exit-code -- artifacts/openapi/beacon-ar-v1.json tests/BeaconAr.ClientContract/generated/beacon-ar-v1.ts
```

## Packages, locks, licenses, configuration, and operations

Inspect `Directory.Packages.props`, every project file, every `packages.lock.json`, `.config/dotnet-tools.json`, package audit output, and package source metadata. Verify direct references are necessary and centrally pinned, all `Paradigm.Enterprise.*` versions agree, locks are current, no deprecated/vulnerable/suppressed package is accepted without an explicit reviewed exception, and no project uses accidental transitive compilation.

For `Microsoft.Identity.Web` and `Swashbuckle.AspNetCore`, record official source repository, MIT license, net10 compatibility, approval, purpose, meaningful transitives, and the absence of AzureAD UI. For any new/changed package, follow the same record; do not infer approval from a sibling project.

Run:

```powershell
dotnet tool restore
dotnet tool run paradigm doctor --project BeaconAr.slnx
dotnet tool run paradigm packages check --project BeaconAr.slnx
dotnet tool run paradigm packages audit --project BeaconAr.slnx
```

Inspect Aspire's resource graph and finite bootstrap contract. Run `./start.sh doctor` and `dotnet tool run aspire restore`; verify `sqlserver -> database -> database-bootstrap -> webapi` ordering, waits, external-mode guards, managed versus external publication policy, local `.env` precedence/ignore rules, secret parameters, no secret defaults, and liveness/readiness split. The bootstrap image must own SqlPackage/SQLCMD and DACPAC publication; neither API startup nor a developer workstation may publish an external schema implicitly.

## Safe local-SQL readiness and live-test gate

The local ignored `examples/beacon-ar/.env` was re-checked secret-safely on 2026-08-02. It contains non-empty `ConnectionStrings__DatabaseConnection`, `Database__Password`, `Database__Mode`, and `Database__PublishOnStart` settings; no values were printed. The explicit external connection remains untrusted for this audit, while the managed password can support a new task-owned disposable SQL Server. Neither fact by itself establishes reachability or authorizes publication to an existing target.

Before any live operation, run this secret-safe classification and record only booleans/target classification, never values:

```powershell
$envPath = 'examples/beacon-ar/.env'
$pairs = Get-Content $envPath | Where-Object { $_ -match '^\s*[^#\s][^=]*=' }
$connection = ($pairs | Where-Object { $_ -match '^\s*ConnectionStrings__DatabaseConnection=' } | Select-Object -First 1) -split '=', 2 | Select-Object -Last 1
if ([string]::IsNullOrWhiteSpace($connection)) { throw 'No local SQL connection is configured.' }
if ($connection -match '(?i)(prod|production)') { throw 'Refuse automatic live validation against a production-class target.' }
Write-Output 'Connection configured; target must be confirmed disposable by its owner before publication.'
```

Only after the owner confirms a disposable SQL Server target and approves schema publication:

1. export the connection only into the current PowerShell process;
2. build/validate the SQL project;
3. publish through the repository-owned Docker/Aspire bootstrap, not host SQLCMD/SqlPackage;
4. run `BeaconAr.Database.IntegrationTests` and the full sales/master-data live matrix against that target;
5. run EFPT regeneration twice, then restore/clean any expected generated diff;
6. capture only status/counts/test names, never connection/server/user/password values.

If target ownership/disposability, Docker/Aspire availability, or connectivity is not proved, do not publish and do not run EFPT regeneration. Mark the exact live SQL suites as **not run** in the release record: database views/SQL routines, generated-model round trip, stored-procedure paging, rowversion races, explicit transaction rollback, conversion singleton race, dashboard serializable consistency, and bootstrap first-run/restart. Offline tests are not a substitute for these checks.

## Ordered execution matrix

Run from `examples/beacon-ar`, retain raw command output as CI/artifact evidence when it contains no secrets, and record result/diagnostics:

```powershell
dotnet restore BeaconAr.slnx --property:RestoreAdditionalProjectSources=../../artifacts
dotnet build BeaconAr.slnx --configuration Release --no-restore
dotnet test --solution BeaconAr.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 1
dotnet tool run paradigm doctor --project BeaconAr.slnx
dotnet tool run paradigm packages check --project BeaconAr.slnx
dotnet tool run paradigm packages audit --project BeaconAr.slnx
dotnet tool run paradigm validate --project BeaconAr.slnx
dotnet tool run paradigm checks run --project BeaconAr.slnx
dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution BeaconAr.slnx --strict --format json
dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release --no-restore
./build/regenerate-persistence.ps1 -TestFailureRecovery
./build/regenerate-persistence.ps1 -TestRedaction
./start.sh doctor
dotnet tool run aspire restore
./scripts/generate-openapi.ps1
dotnet run --project src/BeaconAr.CodeGenerator/BeaconAr.CodeGenerator.csproj --configuration Release --no-build -- openapi-typescript ../../artifacts/openapi/beacon-ar-v1.json ../../tests/BeaconAr.ClientContract/generated/beacon-ar-v1.ts
npm ci --prefix tests/BeaconAr.ClientContract
npm run check --prefix tests/BeaconAr.ClientContract
git diff --check
git status --short
```

Then, only through the approved disposable-SQL gate, run:

```powershell
dotnet test --project tests/BeaconAr.Database.IntegrationTests/BeaconAr.Database.IntegrationTests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 1
./build/regenerate-persistence.ps1
./build/regenerate-persistence.ps1
git diff --exit-code -- src/BeaconAr.Domain/Receivables/Entities src/BeaconAr.Data/Receivables/Context
```

## Release acceptance and remediation policy

Accept only when:

- every traceability row is verified, intentionally deferred with owner/date, or blocked by a clearly named external prerequisite;
- build/test/validation/database/package/OpenAPI/client checks are clean and non-empty;
- all protected endpoint metadata and behavior are verified fail-closed, with no generic anonymous controller inheritance exposed;
- no generated ownership/drift, stale abstraction, broken SQL project metadata, duplicate DTO/view shape, obsolete package/config/documentation, or secret leakage remains;
- the disposable live-SQL matrix passes, or the release record explicitly excludes the affected claims and names the owner/condition for completion;
- Aspire/bootstrap external/managed behavior is safe and repeatable; operational health/telemetry/configuration checks pass;
- `decisions.md`, `review-feedback.md`, and `change-summary.md` are complete, concise, and separate facts from unresolved questions.

For each defect, report file/line, observed behavior, consequence, smallest safe fix, regression test, and whether the fix changes public API/schema/generated output. Re-run the full relevant matrix after a fix; regenerate only from validated database source. Do not "fix" release blockers by loosening validation, deleting tests, adding default secrets, or suppressing package/analysis diagnostics.

## Non-goals

- No automatic publication to a non-disposable, shared, or production SQL Server.
- No new framework guidance/check/fixer without the evidence and governed-fixture threshold described above.
- No broad package upgrades, route/version changes, schema redesign, or new runtime features merely to make final validation easier.
- No printing, committing, or embedding `.env`/connection-string/identity secret values in logs, source, generated files, task documents, or test artifacts.
