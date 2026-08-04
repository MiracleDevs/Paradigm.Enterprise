# Beacon AR Provider and API conventions - review feedback

## Review status

**Not approved.** The code-level Provider/API refactor is coherent and the offline contract artifacts are stable, but two required behavioral gates are not proven and active documentation still teaches superseded ownership/base decisions.

## Findings

### P1 - Complex query-object binding is asserted as source text, not exercised through ASP.NET Core

Paths:

- `tests/BeaconAr.Architecture.Tests/EstablishedLayerBoundaryTests.cs:415-434`
- `src/BeaconAr.WebApi/Controllers/ProductsController.cs:36-38`
- `src/BeaconAr.WebApi/Controllers/CustomersController.cs:36-38`
- `src/BeaconAr.WebApi/Controllers/CarriersController.cs:36-38`
- `src/BeaconAr.WebApi/Controllers/AddressesController.cs:36-38`
- `src/BeaconAr.WebApi/Controllers/QuotesController.cs:40-42`
- `src/BeaconAr.WebApi/Controllers/SalesOrdersController.cs:38-40`
- `docs/beacon-ar-provider-api-conventions/implementation-plan.md:92-101`

The new architecture test reads controller source and searches for the literal text `[FromQuery] {Request} request`. It does not prove MVC metadata, model construction, property binding, omitted-value defaults, enum failures, or the resulting Problem Details contract. No hosted Web API test was added for any of the six changed actions. This is particularly important for the two positional Sales search records and for the defaults that are now supplied by complex-object construction rather than primitive action parameters. The unchanged OpenAPI/client artifacts prove description compatibility, not runtime binding compatibility.

Smallest safe correction:

1. Replace or supplement the source-string fixture with reflection over each `Search` action: assert one parameter with `FromQueryAttribute`, its exact request type, one `CancellationToken`, no primitive parallel query parameters, direct `ControllerBase` ancestry, and the existing read policy.
2. Add database-free hosted tests with capturing fake Providers for all six searches. Exercise an empty query and every optional public query key; assert the exact request property values/defaults received by the Provider. Exercise malformed enums/model-binding values plus validly bound but request-invalid paging/filter/sort values and assert the existing status/code/error-field contract.
3. Keep the current OpenAPI and generated-client byte-stability assertions; those are useful complementary evidence.

### P1 - Transaction/discovery behavior after removing four Provider bases lacks the required live SQL Server gate

Paths:

- `src/BeaconAr.Providers/MasterData/ProductProvider.cs:10-116`
- `src/BeaconAr.Providers/MasterData/CustomerProvider.cs:10-121`
- `src/BeaconAr.Providers/MasterData/CarrierProvider.cs:10-121`
- `src/BeaconAr.Providers/MasterData/AddressProvider.cs:10-191`
- `docs/beacon-ar-provider-api-conventions/change-summary.md:37-40`

The explicit repositories and scoped `IUnitOfWork` match the old base's collaborators, exact-name discovery remains intact for all ten Providers, and the Product mock tests cover commit/rollback/audit behavior. However, the implementation run skipped 42 SQL/authenticated integration cases, including the only real evidence for cross-context transaction enlistment, optimistic concurrency, audit persistence, default-address locking, convention-host activation, and the mutation routes. The task plan explicitly made a freshly published disposable SQL Server database and authenticated Web API suite a release gate.

Smallest safe correction:

1. Publish the existing SQL Server project into a fresh disposable database.
2. Run the complete `BeaconAr.Database.IntegrationTests` and `BeaconAr.WebApi.Tests` integration categories against that connection, including the concurrent/default-address/idempotency cases.
3. Record exact passed/skipped counts and the disposable database lifecycle in `change-summary.md`. Do not claim transaction preservation from mocks alone.

### P2 - Active and historical Beacon documentation contradicts the new ownership and Provider contract

Paths:

- `README.md:5`
- `README.md:13`
- `docs/beacon-ar-web-api/decisions.md:7`
- `docs/beacon-ar-web-api/change-summary.md:6`
- `docs/beacon-ar-final-validation/implementation-plan.md:148-150`

The active README still says Providers own validation and entity-to-view mapping. The Web API decisions/change summary and final-validation plan still speak in the present tense about MasterData `IEditProvider`, `EditProviderBase`, Provider mapping, and `OfficialMasterDataMutationGuard`. Those statements now directly conflict with this task's code and durable skills. They are especially risky because the user asked that the corrected facts be discoverable to the next person or agent.

Smallest safe correction:

1. Update the README to state the current split: entities own intrinsic invariants/transitions, request objects own query/transport-shape rules, Providers invoke those rules and own collaborator-dependent orchestration/transactions, repositories materialize canonical views, and controllers own HTTP concerns.
2. Preserve earlier task reports as historical evidence, but add a prominent superseded/historical note linking to `docs/beacon-ar-provider-api-conventions/decisions.md`; do not leave present-tense obsolete guidance without qualification.
3. Search all Beacon docs for `IEditProvider`, `EditProviderBase`, `OfficialMasterDataMutationGuard`, Provider-owned validation/mapping, and primitive query lists. Qualify historical occurrences and correct active instructions.

### P2 - The Task 5 validation-gap record is inaccurate and incomplete

Path: `docs/beacon-ar-provider-api-conventions/change-summary.md:28-40`

The summary says the combined Paradigm command produced no output and timed out. Independent per-command execution produced deterministic results: the pinned example CLI reports the already-documented `PE1002` DACPAC metadata error for whole-solution doctor, project-scoped validate reports the known `PE3002` mixed-key Operations limitation, and project-scoped `checks run` succeeds for Providers and WebApi. The summary also has no result for package check/audit. A vague timeout hides known, actionable tool-version behavior and prevents a future reviewer from distinguishing an application regression from the pinned CLI limitations already documented by the context-boundaries task.

Smallest safe correction:

1. Run each CLI command separately with a bounded timeout and record its exact exit/result.
2. Reference the existing `beacon-ar-context-boundaries/decisions.md` explanation for pinned CLI `PE1002`/`PE3002`, then run the repository-source CLI where that document says the fixes exist.
3. Record package check/audit outcomes separately; do not combine five commands into one unobservable timeout claim.

## Verified clean areas

- All ten public concrete Providers still implement exactly one convention-matched interface and `IProvider`; the four MasterData contracts no longer expose `IEditProvider` or generated-view mutation parameters.
- All eight controllers remain direct, protected `ControllerBase` types. The reviewed diffs only change the six search signatures; routes, operation IDs, authorization attributes, mutation transport logic, ETags, idempotency, and statuses are otherwise unchanged.
- Provider validation is request/entity-owned or collaborator-dependent. Controller checks are limited to route/ETag/idempotency transport concerns.
- No package, project reference, database object, generated EF/T4 persistence source, or serializer context changed.
- The explicit MasterData Provider constructors receive the same scoped `IUnitOfWork` used by repositories/coordinators under the current host registration; no extra commit was added around generic framework operations.
- Guidance additions are general and consistent with the Provider, Web API, model, review, and evolution skills.

## Independent validation

- `dotnet build BeaconAr.slnx --configuration Release --no-restore`: passed, 0 warnings/errors.
- Domain tests: 50/50 passed.
- Provider tests: 18/18 passed.
- Architecture tests: 46/46 passed.
- Non-integration Web API tests: 37/37 passed.
- OpenAPI generation twice: byte-stable SHA-256 `F0398F38EDA2976BC7937FCD673D70E1F40C17BEF92A7B02C3FE96223EE1B3EB`; no checked artifact diff.
- TypeScript generation twice: byte-stable SHA-256 `DA1FDFC3F9B83F19D55A9607A578AD4CF3F93737FF6AD16102B64D62777840F0`; strict TypeScript check passed; no semantic generated-client diff.
- `git diff --check`: passed.
- Pinned whole-solution `paradigm doctor`: failed with known `PE1002` on `BeaconAr.Database.dacpac`.
- Pinned project-scoped `paradigm validate`: failed with known `PE3002` for `AuditLog`/`IdempotencyRequest` identifier inference.
- Project-scoped `paradigm checks run` for Providers and WebApi: passed.

## Remaining environment gaps

- No disposable SQL Server was available during this review; live database and authenticated Web API integration suites remain unexecuted.
- Package check/audit and repository-source CLI verification were not completed in this bounded review and remain part of the correction cycle.

## Cycle 2 re-review

### Status

**Not approved.** Both P1 findings and the CLI/package evidence finding are resolved. The documentation finding is only partially resolved, and one new Task 5 evidence sentence contradicts the implemented validation flow.

### Resolved - hosted query binding and authorization

- `tests/BeaconAr.WebApi.Tests/QueryObjectBindingTests.cs` starts the real host through `WebApplicationFactory`, replaces only application Providers, authenticates through the configured bearer pipeline, and invokes all six routes over HTTP.
- `EmptyQueriesBindEveryRequestDefaultThroughMvc` proves the four MasterData requests and both positional Sales records receive their intended omitted-value defaults.
- `CamelCaseQueryKeysBindEveryOptionalFilterAndEnumThroughMvc` covers every public query key across Product, Customer, Carrier, Address, Quote, and SalesOrder and inspects the exact request received by the Provider.
- `InvalidBindingAndRequestRulesKeepStableProblemDetails` gives every endpoint invalid coverage and verifies `400`, `validation_failed`, and the expected camel-case error field. `SearchEndpointsRequireReadAuthorizationBeforeProviderExecution` proves all six unauthenticated requests stop before Provider invocation.
- `EstablishedLayerBoundaryTests.SearchControllersBindOneExistingComplexQueryRequest` now uses reflection rather than source text and proves exact request type, one `FromQueryAttribute`, one `CancellationToken`, direct `ControllerBase`, and the read policy.
- Independent rerun: QueryObjectBinding tests 4/4 passed; Architecture tests 46/46 passed; Architecture Release build passed with 0 warnings/errors.

The new Architecture-to-WebApi project reference is a reasonable test-only dependency for metadata inspection. Its lock-file expansion is the transitive result of that project graph, not a new direct package, and the recorded package audit covers the resolved graph.

### Resolved - SQL Server transaction/discovery gate and cleanup

- `change-summary.md` records a fresh `BeaconArTask5R10` DACPAC deploy report/publish/schema probe, Database Integration 24/24, and authenticated Web API 60/60 with no skips.
- The live run exercised the real convention host, scoped Unit of Work, cross-context audit transactions, concurrency, default-address locking, Sales workflows, and idempotency. The only test correction changes a historical Provider-layer `SalesValidationException` expectation to the current entity-owned `DomainException`; `QuoteLine`/`SalesOrderLine` behavior emits the asserted "too large" rule, so this is an ownership-alignment fix rather than weakened validation.
- The task label `paradigm.task=beacon-ar-provider-api-r10` currently resolves to zero containers, networks, images, and volumes. The documented pre-existing stopped `sqlserver-a91e0506` container and `beaconar.apphost-a91e05069e-sqlserver-data` volume remain present and untouched. This is internally consistent cleanup evidence, so the reviewer did not recreate a live database.

### Resolved - CLI and package evidence

- Pinned CLI outcomes are separated and accurately identify the known `PE1002` whole-solution DACPAC classification and `PE3002` mixed-key inference limitations; Provider/WebApi source checks pass.
- Package check passed. Package audit exited successfully with no vulnerabilities and lists only stable updates to existing dependencies.
- Independent repository-source rerun used the source CLI project explicitly: whole-solution `doctor` passed and inspected 16 managed projects, while Data-project `validate` passed and inferred `AuditLog`/`IdempotencyRequest` as `long`.
- Repository-source strict database validation is recorded as passing. No package reference, database object, generated EF persistence file, or T4 template changed.

### P2 - Historical documentation qualification remains incomplete

Paths:

- `docs/beacon-ar-database-views/decisions.md:29`
- `docs/beacon-ar-database-views/review-feedback.md:29-31`
- `docs/beacon-ar-domain-ownership/implementation-plan.md:13`
- `docs/beacon-ar-domain-ownership/decisions.md:52`
- `docs/beacon-ar-web-api/implementation-plan.md:16,118`
- `docs/beacon-ar-api-contract/implementation-plan.md:15`

The README and the exact Web API/final-validation/framework-CRUD files cited in cycle 1 are now corrected or prominently labeled as historical. However, the repository-wide scan requested by the cycle-1 remedy still finds unqualified historical files that teach Provider-owned mapping/validation, say the generic fail-closed surface remains current/deferred, or describe `IEditProvider` as the intended Provider contract. Their task-folder names establish provenance, but the statements are still phrased as durable/current rules and conflict with the user's requirement that future readers reach the corrected ownership decision from any relevant document.

Smallest safe correction: add the same concise superseded note and link to `beacon-ar-provider-api-conventions/decisions.md` at the top of these remaining historical reports. Preserve their original task evidence; do not rewrite historical commands/results or delete the obsolete decision text.

### P2 - Hosted-test evidence overstates when Providers are not invoked

Paths:

- `docs/beacon-ar-provider-api-conventions/change-summary.md:21`
- `tests/BeaconAr.WebApi.Tests/QueryObjectBindingTests.cs:122-225`

The summary claims that "rejected or unauthenticated requests do not call Providers." That is true for unauthenticated and MVC model-binding failures, but not for validly bound request-rule failures such as `pageSize=101`, `customerId=0`, or an unsupported sort field. Each fake Provider method is entered and calls `request.Validate()` before incrementing `SearchCalls`, exactly matching the intended production design that Providers invoke request-owned validation for non-HTTP callers. The counter placement makes a Provider invocation look like no invocation and the documentation contradicts `decisions.md`.

Smallest safe correction: change the summary to distinguish the flows: unauthenticated/model-binding failures stop before the Provider; validly bound query-rule failures enter the Provider and fail through the request's own validation before repository access. If the test asserts invocation counts for invalid requests, increment an invocation counter before `request.Validate()` and keep a separate successful-search counter after validation.

### Cycle 2 clean regression scan

- Provider contracts, DI/discovery names, scoped Unit of Work use, transaction/commit ordering, ETag concurrency, generated-view write exclusion, and controller authorization remain unchanged and clean from cycle 1.
- The new hosted tests do not connect to SQL Server and replace exact Provider interfaces only inside the test host.
- OpenAPI/client hash evidence remains unchanged and byte-stable; `git diff --check` passes.
- No new generated EF/T4 edit, database-source change, runtime package, anonymous controller base, duplicated validation owner, or parallel API DTO was introduced.

## Cycle 3 documentation re-review

### Status

**Approved.** The two remaining cycle-2 documentation findings are resolved. No open Task 5 finding remains.

### Historical guidance qualification - resolved

Each exact historical file identified in cycle 2 now begins with a prominent `superseded 2026-08-04` Provider/API note and links to `beacon-ar-provider-api-conventions/decisions.md`:

- `beacon-ar-database-views/decisions.md`
- `beacon-ar-database-views/review-feedback.md`
- `beacon-ar-domain-ownership/implementation-plan.md`
- `beacon-ar-domain-ownership/decisions.md`
- `beacon-ar-web-api/implementation-plan.md`
- `beacon-ar-api-contract/implementation-plan.md`

The notes accurately state the current split: repositories materialize canonical views; entities own intrinsic invariants; requests own query/proposed-input rules; Providers invoke those rules and own collaborator-dependent orchestration/transactions; MasterData exposes explicit request-based `IProvider` contracts; and controllers bind one query request and own HTTP concerns. The original historical decisions and validation records remain intact rather than being rewritten as if the later design existed at the time.

### Validation-flow evidence - resolved

`beacon-ar-provider-api-conventions/change-summary.md:21` now distinguishes all three execution boundaries correctly:

1. unauthenticated and MVC model-binding failures stop before Provider invocation;
2. validly bound query-rule failures enter the Provider and execute the request's own `Validate()` behavior;
3. those request failures occur before repository or other collaborator execution.

This wording matches the production Provider design, the current decision document, and the hosted fake implementation. It no longer infers Provider invocation from the post-validation `SearchCalls` counter.

### Final documentation checks

- The targeted source scan found the current-decision link and supersession marker in all six historical files.
- The Task 5 summary contains no remaining claim that every rejected request bypasses Providers.
- `git diff --check` passed; only repository line-ending conversion warnings were emitted.

Task 5 is approved with the cycle-1 and cycle-2 clean areas, deterministic tests, live SQL evidence, cleanup evidence, CLI/package evidence, and compatibility hashes unchanged.
