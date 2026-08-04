# Beacon AR Provider and API conventions - change summary

## Provider contracts

- Replaced the four MasterData `IEditProvider<TView,int>` contracts with the smallest honest `IProvider` contracts for the supported request-based Search/Get/Create/Update/Delete use cases.
- Replaced `EditProviderBase` inheritance with explicit typed repository, view-repository, and `IUnitOfWork` dependencies. The existing audited transaction, optimistic-concurrency, cancellation, duplicate/reference, and default-address workflows remain intact.
- Removed `OfficialMasterDataMutationGuard` and all inherited view-based Add/Update/Save/Delete overloads. Generated views remain response/read shapes and never become mutation input.
- Kept the Sales aggregate, quote-conversion, idempotency, dashboard, and coordinator services in Providers because they coordinate repositories, transactions, identity/time, audit, locking, or cross-aggregate facts.

## Validation ownership

- Moved authenticated-identity completeness validation from `CurrentUserProvider` to `AuthenticatedIdentity.Validate` while preserving the stable `forbidden` application error.
- Moved positive Sales reference-selection preconditions onto the four owning create/update request types. Quote and SalesOrder Providers invoke those request behaviors before repository calls and retain only existence/active/reference resolution.
- Preserved query-shape validation on each search request and transport-only positive route ID/ETag/idempotency validation at the Web API edge.

## Query binding and compatibility

- Products, Customers, Carriers, Addresses, Quotes, and SalesOrders now accept one `[FromQuery]` search request object and pass that same object to the Provider.
- Controllers remain direct protected `ControllerBase` types with their existing routes, operation IDs, policies, bodies, ETags, idempotency, status codes, and cancellation behavior.
- The OpenAPI document filter canonicalizes complex-query property names/order/default metadata back to the existing flat camel-case contract. The checked OpenAPI artifact has no semantic or byte diff, and the generated TypeScript client retains its existing flat method signatures.
- Database-free hosted ASP.NET Core tests exercise all six search endpoints through real HTTP. They prove omitted defaults, every public camel-case query key, positional Sales-record construction, enum and request-rule failures, stable `validation_failed` Problem Details fields, and authorization. Unauthenticated and MVC model-binding failures stop before Provider invocation; validly bound request-rule failures enter the Provider, call the request's own `Validate()` behavior, and fail before repository or other collaborator execution.

## Deterministic fixtures and guidance

- Architecture fixtures now prove that MasterData contracts implement `IProvider` but not `IEditProvider`, concrete Providers have no misleading base, generated views are absent from public mutation inputs, the fail-closed guard is gone, and reflection confirms all six searches have exactly one complex `[FromQuery]` request plus `CancellationToken`, direct `ControllerBase` ancestry, and the read policy.
- Domain/Provider tests cover authenticated-identity validation and rejection of invalid Sales reference IDs before collaborator calls.
- Provider, Web API, and review skills now record the durable rules for honest Provider surfaces and cohesive query-object binding.

## Validation evidence

- The implementation release build passed with 0 warnings and 0 errors. After review remediation, the targeted Architecture release build (including its new Web API project reference) also passed with 0 warnings and 0 errors, and Architecture Tests passed 46/46.
- Offline Web API suite after adding hosted binding coverage: 60 total, 41 passed, and 19 explicitly skipped because the live SQL connection was absent.
- Fresh SQL Server gate (`BeaconArTask5R10`): DACPAC deploy report, publish, and schema probe passed; Database Integration Tests passed 24/24 and Web API Tests passed 60/60 with no skips. The first substantive live run exposed one historical test that still expected the former Provider-layer exception for aggregate overflow; the assertion was corrected to the current entity-owned `DomainException`, then the fresh gate passed.
- The disposable gate used only resources labeled `paradigm.task=beacon-ar-provider-api-r10`: the explicitly named SQL/bootstrap/database-create containers, one user-defined network, one bootstrap image, host port 14345, and no volume. Exact-name cleanup completed. Post-cleanup label queries returned no Task 5 containers, networks, images, or volumes; the pre-existing stopped `sqlserver-a91e0506` container and `beaconar.apphost-a91e05069e-sqlserver-data` volume remained untouched.
- Repository-source strict database validation passed against `src/database/BeaconAr.Database.sqlproj` after the live gate.
- OpenAPI generation: passed twice and byte-stable (`F0398F38EDA2976BC7937FCD673D70E1F40C17BEF92A7B02C3FE96223EE1B3EB`); checked artifact unchanged.
- TypeScript client generation: passed twice and byte-stable (`DA1FDFC3F9B83F19D55A9607A578AD4CF3F93737FF6AD16102B64D62777840F0`); strict TypeScript check passed. `npm ci` reported only the existing Node 24.13.0 versus Angular 22.1.0 engine-range warning and no vulnerabilities.
- `git diff --check`: passed.

## CLI and package-tool evidence

- Pinned example `paradigm doctor --project BeaconAr.slnx`: failed with the documented `PE1002` DACPAC metadata limitation. A pre-final-build run also correctly reported a stale Architecture output after its project reference changed; the release build refreshes that output before the final doctor record.
- Pinned example `paradigm validate --project src/BeaconAr.Data/BeaconAr.Data.csproj`: failed with the known `PE3002` mixed `int`/`long` Operations identifier inference for `AuditLog` and `IdempotencyRequest`. These pinned-tool limitations and their repository-source fixes are documented in [`beacon-ar-context-boundaries/decisions.md`](../beacon-ar-context-boundaries/decisions.md).
- Pinned example `paradigm checks run` passed separately for `BeaconAr.Providers.csproj` and `BeaconAr.WebApi.csproj`.
- Pinned example `paradigm packages check --project BeaconAr.slnx` passed.
- Pinned example `paradigm packages audit --project BeaconAr.slnx` exited successfully, reported no vulnerabilities, and warned only about available stable updates for existing Roslyn, EF Design, Microsoft.Extensions, Aspire service-discovery/resilience, OpenAPI, and OpenTelemetry packages. Package upgrades were outside this task; no package changed.
- Repository-source `database validate --strict` passed for the SQL project. After refreshing the changed Architecture and Data release outputs, repository-source whole-solution `doctor` and project-scoped Data `validate` both passed, confirming that the pinned `PE1002`/`PE3002` results are tool-version limitations rather than application regressions.
- No package, database object, generated EF persistence file, or official T4 template was changed.
