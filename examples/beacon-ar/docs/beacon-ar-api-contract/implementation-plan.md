# Beacon AR API contract implementation plan

> **Historical Provider/API note (superseded 2026-08-04):** This plan preserves evidence and decisions from an earlier implementation stage. Current ownership is: repositories materialize canonical views; entities own intrinsic invariants; request objects own query and proposed-input rules; Providers invoke those rules and own only collaborator-dependent orchestration and transactions; MasterData exposes explicit request-based `IProvider` contracts rather than `IEditProvider`, `EditProviderBase`, or a fail-closed mutation guard; and controllers bind one `[FromQuery]` request and own HTTP concerns. See [the current Provider/API decisions](../beacon-ar-provider-api-conventions/decisions.md).

> **Historical persistence note (superseded 2026-08-03):** This document records an earlier implementation stage. Current persistence ownership is defined by docs/beacon-ar-context-boundaries: four disjoint Access, MasterData, Operations, and Sales contexts/configurations; there are no Receivables or Reporting application roots.

## Objective

Complete the release-one Beacon AR HTTP boundary on branch `task/beacon-ar-api-contract`. Expose the existing master-data, sales, conversion, and reporting Providers through an authenticated `/api/v1` REST API; make authentication, authorization, concurrency, idempotency, errors, serialization, and generated-client behavior part of one versioned OpenAPI contract; and finish the example's runbook, README, and CI acceptance path.

The result must be usable by the Angular SPA without client-side paging, pricing, authorization, or lifecycle workarounds. Controllers remain transport adapters: they bind and validate HTTP inputs, apply policies, translate successful results, and delegate business behavior to Providers. They must not resolve repositories, `ReceivablesDbContext`, or `IUnitOfWork`, and they must derive directly from ASP.NET Core `ControllerBase`; Paradigm controller bases inherit `AllowAnonymous` and are not safe for these protected routes.

## Inspected baseline

- `BeaconAr.WebApi` currently contains only `Program.cs`, configuration, launch settings, and Swagger assets. It registers controllers, SQL health, the context connection provider, context, and Unit of Work, but it does not register repositories/Providers or expose business controllers.
- Master-data Providers already implement search/detail/create/update/delete. Quote and Sales Order Providers implement search/detail/create/update/delete/transitions. `QuoteConversionProvider` implements the accepted-quote singleton conversion, and `DashboardProvider` returns the five counts in one database operation.
- Provider writes already own validation, transactions, audit facts, rowversion checks, state machines, snapshots, database paging, and conversion uniqueness. The API must reuse those behaviors rather than duplicate them.
- Mutable DTOs already carry a canonical encoded `Version`. Provider update/delete/transition methods accept an expected version. `IQuoteConversionProvider.ConvertAsync` currently also accepts one; this task must remove that conversion precondition because the required singleton conversion contract relies on the accepted state, locking, and unique source-quote relationship instead of `If-Match`.
- `ApplicationUser` and `IdempotencyRequest` tables exist, but no Access/Operations repositories or Providers use them. `IApplicationOperationContext` requires a local integer user ID and correlation ID, so an authenticated-request bootstrap seam is still required before any audited Provider is invoked.
- JSON reflection is disabled in `BeaconAr.WebApi`; no application serializer context is currently registered. The existing template-oriented JSON generator neither discovers every Provider nor owns WebApi-only contracts, so it cannot be treated as complete endpoint metadata.
- Service defaults expose process liveness at `/alive` and dependency-aware readiness at `/health`. Keep these established routes, make their anonymous operational intent explicit, and do not return check names, exceptions, connection details, or other diagnostics.
- The example centrally pins `NSwag.CodeGeneration.TypeScript` but does not directly reference or use it. There is no OpenAPI generation package, Web API integration-test project, generated-client compile probe, or CI-published API artifact.

## Security decisions and current guidance

Security guidance was re-checked on 2026-08-01 against current primary Microsoft documentation:

- A Web API must validate an access token's signature, issuer, audience, and lifetime, and must reject tokens intended for another resource: [Microsoft identity platform access-token validation](https://learn.microsoft.com/en-us/entra/identity-platform/access-tokens).
- Authentication alone is insufficient; delegated scopes and assigned app roles must be checked by the API: [Verify scopes and app roles in a protected Web API](https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-verification-scope-app-roles).
- CORS is not authorization. Origins must be explicitly allow-listed, and wildcard origins must not be combined with credentials: [ASP.NET Core CORS guidance](https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0).
- APIs should not redirect a request containing sensitive data from HTTP to HTTPS; outside local development they should bind HTTPS or reject HTTP: [ASP.NET Core HTTPS guidance](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-10.0).

Apply these choices:

1. Use the ASP.NET Core JWT bearer resource-server handler from the shared framework. Do not store access tokens, implement token parsing/cryptography, add a cookie session, or copy the template's custom header authentication. Configure `Authority`, `Audience`, and the exact accepted issuer from `Authentication`; require HTTPS OIDC metadata outside Development/Testing; retain signature, signing-key, issuer, audience, and lifetime validation; use a small documented clock skew; set `SaveToken=false`; and keep identity-model PII logging disabled.
2. Fail startup outside Testing when authority, audience, issuer, or permission mappings are absent or placeholder values. Configuration and `.env.example` contain identifiers/origins only, never client secrets. The Angular public client uses Authorization Code with PKCE and requests an access token for this API; the API never accepts an ID token as authorization.
3. Read permission values from the validated `scp` claim's space-separated values and the `roles` claim's individual values. Do not infer authorization from display name, email, tenant-wide directory roles, or groups. A shared evaluator is the sole owner of this mapping so policies and `/me` cannot disagree.
4. Register `business.read` and `business.write` policies. `business.read` succeeds for either configured read or write permission; `business.write` succeeds only for the write permission. Thus write implies read. Require a validated issuer and subject plus a human-user display claim for release-one callers; app-only/service tokens are outside the business API actor model.
5. Put `[Authorize]` on `/me`; put `[Authorize(Policy = "business.read")]` on all business `GET`s and `[Authorize(Policy = "business.write")]` on every mutation. Also configure an authenticated fallback policy as defense in depth, but test controller/action metadata directly. `ExposeEndpoint` is not authorization and is not needed for purpose-built controllers.
6. Return `401` for missing, malformed, expired, invalid-signature, wrong-issuer, or wrong-audience bearer tokens. Return `403` for an authenticated user missing the endpoint policy or mapped as an inactive local user. Never return token-validation diagnostics, claims, or expected permission values in those responses.

## Access identity and request operation context

Add a small `Access` application slice rather than making controllers or middleware perform EF work:

- Add `CurrentUserDto` with local positive integer `id`, `displayName`, nullable `email`, and a deterministic ordered list of effective policies.
- Add an immutable authenticated identity input containing validated issuer, subject, display name, safe optional email, and effective policies. Normalize lengths to the existing schema and reject a missing issuer/subject/display name as forbidden rather than manufacturing an identity. Treat names/email as display metadata, never authorization facts.
- Add exact-name `IApplicationUserRepository`/`ApplicationUserRepository` and `ICurrentUserProvider`/`CurrentUserProvider`. Resolve `(issuer, subject)` under a transaction, create the local row on first use, update changed display metadata on later use, handle the unique-key first-login race by reloading the winner, and reject `IsActive=false`. The first row has nullable self-audit FKs; subsequent changes use the local user ID. Do not persist claims, roles, scopes, or tokens.
- Add a scoped, request-owned `ApplicationOperationContext` implementing `IApplicationOperationContext`. It is initialized exactly once with the resolved local user ID and `Activity.Current.Id` (falling back to `HttpContext.TraceIdentifier` only when no Activity exists); access before initialization fails rather than attributing an audit to a sentinel user.
- Add middleware after authentication and before authorization that applies only to authenticated `/api/v1` requests. It evaluates policies, resolves/synchronizes the local user through `ICurrentUserProvider`, initializes the operation context, and stores `CurrentUserDto` in a scoped accessor. `/me` returns that value; all audited Providers receive the same user and correlation values. Keep health and development OpenAPI routes outside this database-backed identity bootstrap.
- Use the standard W3C `traceparent`/`tracestate` flow already instrumented by ServiceDefaults. Do not trust an arbitrary caller-supplied correlation ID or create a second tracing system.

## Required endpoint contract

Implement one sealed `[ApiController]` per resource/capability with literal versioned routes and stable operation IDs. Every action accepts `CancellationToken` and passes it only to Provider methods whose contracts accept it. Positive route IDs are transport-validated. Request bodies omit `id`; the route ID is authoritative, so no overpostable persistence entity or broad view is accepted.

| Controller | Operations and required success response |
| --- | --- |
| `MeController` | `GET /api/v1/me` -> `200 CurrentUserDto`. |
| `DashboardController` | `GET /api/v1/dashboard/summary` -> `200 DashboardSummaryDto`. |
| `ProductsController` | `GET /products`, `GET /products/{id}` -> `200`; `POST /products` -> `201` plus `Location`, representation, and `ETag`; `PUT /products/{id}` -> `200` plus new `ETag`; `DELETE /products/{id}` -> `204`. List query includes shared parameters and `active`. |
| `CustomersController` | Same CRUD/status rules at `/customers`; list query includes `active`. |
| `AddressesController` | Same CRUD/status rules at `/addresses`; list query includes `customerId`, exact `type`, and `usage`. `usage` is `billing` or `shipping`; read `type` remains a string so unknown transported values remain displayable. |
| `CarriersController` | Same CRUD/status rules at `/carriers`; list query includes `active`. |
| `QuotesController` | `GET /quotes`, `GET /quotes/{id}` -> `200`; `POST /quotes` -> `201` plus `Location`, body, `ETag`; `PUT /quotes/{id}` -> `200` plus new `ETag`; `DELETE /quotes/{id}` -> `204`; `POST /quotes/{id}/status-transitions` -> `200` plus new `ETag`; `PUT /quotes/{id}/sales-order` -> `201` with sales-order `Location` and `ETag` on first conversion, `200` with the same order and its current `ETag` on every sequential/concurrent replay. List query includes `status` and `customerId`. |
| `SalesOrdersController` | `GET /sales-orders`, `GET /sales-orders/{id}` -> `200`; `POST /sales-orders` -> `201` plus `Location`, body, `ETag`; `PUT /sales-orders/{id}` -> `200` plus new `ETag`; `DELETE /sales-orders/{id}` -> `204`; `POST /sales-orders/{id}/status-transitions` -> `200` plus new `ETag`. List query includes `status`, `customerId`, and `sourceQuoteId`. |

All paths in the table are relative to `/api/v1` unless already fully shown. `CreatedAtAction`/equivalent must point to the corresponding detail resource, not echo an untrusted host. Detail and mutation representations expose the Provider-returned authoritative snapshots, pricing, audit metadata, and version. Collections use exactly `items`, `pageNumber`, `pageSize`, `totalPages`, and `itemsCount`; no HTTP layer re-paging/filtering is permitted.

### Binding and status behavior

- Accept only `application/json` request bodies; emit `application/json` successes and `application/problem+json` failures. Missing/invalid bodies and malformed JSON are deterministic `400`s. Unsupported media type is `415`; over-limit content is `413`.
- Bind query names exactly as documented: `search`, `pageNumber`, `pageSize`, `sortField`, `sortDirection`, `active`, `customerId`, `type`, `usage`, `status`, and `sourceQuoteId`. Unknown enum text, non-positive IDs/pages, page-size overflow, and unrecognized sort fields return `400` without invoking a Provider mutation. Domain validation remains authoritative for business fields.
- Keep the established maximum page size from the Provider/domain validation and document it as `100`; supported UI sizes remain 10, 20, and 50, but other positive values through 100 are accepted.
- `404` is used only for invisible/missing resources. Duplicate keys, protected references, inactive/new references, invalid lifecycle states, and incompatible idempotency reuse are `409`. Stale rowversions are `412`.
- `PUT` remains full replacement of editable fields. Omitted optional values clear them where the existing Provider contract permits; omitted required values fail validation. There is no PATCH endpoint in release one.

## ETag and `If-Match`

- Encode the existing canonical Provider `Version` as one strong quoted ETag. Use a single boundary codec/parser; never accept an unquoted value, weak tag, wildcard, or multiple candidates. The body retains `version` as required version metadata, while `ETag` is the mutation precondition used by HTTP clients.
- Emit `ETag` on mutable detail responses, successful creates, updates, and transitions, and on both results of quote conversion. List items retain `version`; collections do not receive an aggregate ETag.
- Require `If-Match` on master-data, Quote, and Sales Order `PUT`/`DELETE`, and on both status-transition POSTs. Missing produces `428 Precondition Required` with `code=precondition_required`; malformed produces `400 invalid_if_match`; a well-formed stale value reaches the Provider and produces `412 concurrency_conflict`.
- Do not require `If-Match` on `PUT /quotes/{id}/sales-order`. Remove `expectedVersion` from `IQuoteConversionProvider.ConvertAsync` and its implementation/tests; the Provider must continue locking and validating the source and recovering the source-quote uniqueness race. This is the explicit exception in the functional contract.
- CORS exposes `ETag` and `Location` so the Angular client can read them. OpenAPI documents `If-Match` as required only on the listed operations and documents both conversion outcomes.

## Creation idempotency

Support optional `Idempotency-Key` on the six POST creation routes: Product, Customer, Address, Carrier, Quote, and direct Sales Order. Quote conversion is already idempotent through its singleton PUT contract and does not use this header.

1. Accept one trimmed opaque key of 1-128 visible ASCII characters. Hash the key with SHA-256 immediately; never persist or log the raw value. Build a deterministic SHA-256 request fingerprint from the operation name and the source-generated canonical JSON form of the bound request.
2. Add application-level `IdempotencyDescriptor` and `CreationResult<T>` contracts plus exact-name `IIdempotencyRepository`/`IdempotencyRepository`. Extend the six Provider create methods to accept an optional descriptor and return whether the resource was newly created or replayed. HTTP header concepts do not enter Domain entities.
3. Reserve/read the existing `(UserId, Operation, KeyHash)` ledger inside the same explicit SQL transaction as the resource and audit write. A new key stages `in_progress`, creates the resource, records resource type/ID and status, marks `completed`, and commits once. A rollback removes the reservation with the business write. A concurrent identical request blocks/reloads and returns the winner; it must not run the mutation twice.
4. The same key, user, and operation with a different request hash returns `409 idempotency_key_reused`. A completed exact replay returns the referenced current resource, the original `201` contract and same `Location`, and may include a documented `Idempotency-Replayed: true` response header. An unexpired orphaned `in_progress` row returns `409 idempotency_in_progress`; expiry/cleanup is operator-owned and must not guess whether an external response was observed.
5. Make retention configurable with a safe bounded default (24 hours). Do not put bodies, tokens, personal fields, or request JSON in the ledger. Test exact retry, different-payload reuse, user/operation isolation, rollback, sequential replay, and simultaneous requests through independent scopes against SQL Server.

## Safe RFC 7807 errors

Use ASP.NET Core `AddProblemDetails`, `IProblemDetailsService`, a custom `IExceptionHandler`, an invalid-model-state response factory, and a unified authorization result handler/JWT challenge path so every expected failure has the same shape:

```json
{
  "status": 400,
  "code": "validation_failed",
  "title": "One or more validation errors occurred.",
  "detail": "The request could not be processed.",
  "errors": {
    "lines.0.quantity": ["Quantity must be greater than zero."]
  },
  "correlationId": "00-..."
}
```

- Map `MasterDataValidationException`/`SalesValidationException` to `400 validation_failed`; normalize field keys to request camelCase paths such as `lines.0.quantity`.
- Map safe application codes to the status categories above: `not_found` -> 404; `duplicate_key`, reference conflicts, inactive-reference conflicts, quote/order state conflicts, and idempotency conflicts -> 409; `concurrency_conflict` -> 412. Preserve each stable existing code, including `referenced_record`, `referenced_address`, `invalid_quote_transition`, and `invalid_sales_order_transition`.
- Add boundary codes for `malformed_json`, `invalid_query`, `invalid_if_match`, `precondition_required`, `unsupported_media_type`, `request_too_large`, `unauthorized`, `forbidden`, and `https_required`.
- Add the Activity/trace identifier as `correlationId` to every Problem Details response. Log the exception and stable code with structured fields at the correct level, but never log tokens, idempotency keys, customer contact/address data, snapshot values, SQL, or request bodies.
- Cancellation caused by the disconnected caller is not converted into a fabricated business error. Unexpected exceptions return a generic `500 internal_error`; stack traces, exception messages, SQL details, claim values, and configuration are never serialized, including in Development API responses.

## JSON and generated-contract rules

- Configure System.Text.Json once with camel-case property/dictionary names, case-insensitive input property matching, and string enums using camel-case values with integer enum input disabled. Required wire enum values include `asc`, `desc`, all Quote statuses, all Sales Order statuses, and `billing`/`shipping` usage.
- Keep address `type` as a forward-compatible string on reads and validate the closed release-one values only on writes.
- Serialize `DateOnly` as `YYYY-MM-DD` and describe it as OpenAPI `string/date`. Serialize `DateTimeOffset` as ISO 8601 UTC (`Z`) and describe it as `string/date-time`; add tests that no local offset leaks from `creationDate`, `modificationDate`, or `asOf`.
- Serialize all monetary values as JSON numbers, never locale-formatted strings or binary floating-point values. OpenAPI marks the fixed-precision decimal fields and their constraints; contract tests cover two-decimal derived totals and four-decimal input prices at rounding boundaries.
- Add an application-owned `BeaconArApiJsonContext` containing every request, response, nested line, paging closure, current-user/error type, and collection/dictionary shape. Register it in MVC and HTTP JSON resolver chains. Extend the generator or maintain an explicit reviewed context, but require a clean deterministic regeneration/check and runtime serialization tests with reflection disabled.
- Configure invalid-model errors so serializer paths and domain paths converge on the same camelCase dotted notation. Ensure no persistence entity is reachable from the JSON context.

## Host, DI, CORS, HTTPS, and health

Recompose `Program.cs` in this order: configuration validation; ServiceDefaults; Problem Details/exception handling; controllers/JSON/API behavior; JWT authentication and policies; named CORS; OpenAPI; health checks; Paradigm repository/Provider discovery using explicit Data and Provider assemblies; Unit of Work/context; Access/Operations services and request context; then build. In the middleware pipeline use exception handling, forwarded headers only for explicitly configured trusted proxies, production HTTPS rejection, routing/CORS, authentication, authenticated-user bootstrap, authorization, and endpoint mapping in framework-correct order.

- Call Paradigm registration with explicit assemblies so `BeaconAr.Data` repositories and `BeaconAr.Providers` implementations are reachable. Do not register broad entry-assembly scans or duplicate scoped implementations. Add a DI smoke test resolving every controller and Provider in a scope.
- Add named CORS policy `beacon-spa` from validated `Cors:AllowedOrigins`. No wildcard origins, origin reflection, subdomain wildcard, or credentials. Allow only required methods and `Authorization`, `Content-Type`, `If-Match`, `Idempotency-Key`, `traceparent`, `tracestate`, and `baggage`; expose `ETag`, `Location`, and `Idempotency-Replayed`. Apply the policy to mapped API controllers, not globally to health/OpenAPI.
- Outside Development/Testing, require an HTTPS effective request after trusted forwarded-header processing and return a safe `400 https_required` instead of redirecting a bearer request or CORS preflight. Production deployment must bind TLS or terminate TLS at an explicitly trusted proxy. Local Aspire HTTP remains permitted only in Development.
- Set a documented JSON body limit (1 MiB is sufficient for release-one quote/order payloads), reject oversized requests before buffering, and keep idempotency canonicalization within that bound.
- Preserve `/alive` as anonymous process liveness and `/health` as anonymous dependency-aware readiness with the existing three-second SQL timeout. Restrict them to GET/HEAD, return only status, exclude them from bearer policy, CORS, and the v1 business OpenAPI document, and test ready/unready database states.

## OpenAPI 3 and NSwag-ready output

Use ASP.NET Core's current build-time OpenAPI support, explicitly emit OpenAPI 3.0 for broad NSwag compatibility, and keep the document name `v1`. Current Microsoft guidance supports build-time generation and a configured output directory: [Generate OpenAPI documents](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0).

- Proposed new direct packages are `Microsoft.AspNetCore.OpenApi`, `Microsoft.Extensions.ApiDescription.Server`, and `Microsoft.AspNetCore.Mvc.Testing`, from Microsoft's MIT-licensed `dotnet/aspnetcore` repository. Before changing package files, record alternatives/license/repository/maintenance/transitives and obtain the dependency approval required by repository policy. `NSwag.CodeGeneration.TypeScript` is already centrally pinned; add a direct CodeGenerator reference only after confirming the prior approval covers this use.
- Generate `artifacts/openapi/beacon-ar-v1.json` during Release CI without connecting to or mutating the database. Build-time document startup must register the real endpoints/metadata but skip runtime-only database probing/bootstrap; it must not silently use a different endpoint graph.
- Assign stable explicit operation IDs. Add a JWT bearer security scheme and per-operation security requirement; describe that `business.write` implies `business.read`. Exclude health endpoints. Include tags, summaries, request/response examples, all success/failure status codes, `application/problem+json`, validation-error extensions, paging schemas, enums, limits, nullability, `date`/`date-time`, decimal constraints, `Location`, `ETag`, `If-Match`, and `Idempotency-Key`.
- Add OpenAPI transformers only for metadata that cannot be expressed accurately by controller attributes. Contract tests fail if any `/api/v1` operation is missing, an unexpected operation is exposed, an operation ID is duplicated, a mutation lacks security/error metadata, an `If-Match` requirement is wrong, or conversion lacks distinct 200/201 responses.
- Extend `BeaconAr.CodeGenerator` with an `openapi-typescript` target that reads a supplied v1 document and uses NSwag's Angular/HttpClient template with deterministic settings. It writes only to a requested output path; it does not assume a frontend checkout.
- Add `tests/BeaconAr.ClientContract` with pinned `package.json`/lockfile, Angular-compatible dependencies, strict `tsconfig`, and a small consumer of every generated API group. CI runs `npm ci`, generates the client from the artifact, and runs `tsc --noEmit`. Generated output may remain a CI artifact; the OpenAPI document is the source contract.
- Publish the JSON document and generated TypeScript client as a versioned CI artifact named `beacon-ar-api-v1`; fail CI on generation or strict-compilation errors.

## Test plan

Add `BeaconAr.WebApi.Tests` for cheap boundary/unit tests and `BeaconAr.WebApi.IntegrationTests` for the real middleware pipeline plus SQL Server. Add both projects to root `BeaconAr.slnx`. Use `WebApplicationFactory` with an explicit Testing configuration; use a real signed JWT validation configuration for auth tests rather than replacing authorization with an always-successful fake. SQL integration cases use a freshly published database and independent scopes/connections for races.

### Authentication and authorization

- Missing bearer token; malformed token; expired token; future token; invalid signature; wrong issuer; wrong audience; ID token/wrong resource; missing issuer/subject/display identity; and inactive local user all fail with the specified safe status/body.
- Read-only scope/role can call `/me`, every business GET, and Dashboard but receives `403` for every mutation. Write permission can call reads and writes. Claims are checked for exact values, including space-separated `scp`; substrings/case variants/groups do not grant access.
- First request creates one local user; changed display metadata updates it; simultaneous first requests create exactly one row; two issuers with the same subject do not collide. `/me` returns the same effective policies used by authorization.
- Reflect over all `/api/v1` controller actions to prove explicit authorization metadata and the absence of inherited `AllowAnonymous`. Prove `/alive` and `/health` are the only deliberate anonymous production endpoints.

### Contract, binding, and middleware

- Exercise every endpoint and verb from the catalog with valid requests; assert exact routes, policies, operation IDs, status codes, content types, Location targets, representations, and no extra exposed CRUD actions.
- Verify all search/filter/sort/paging query names and combinations reach the correct Provider/database behavior; invalid values fail before mutation. Assert deterministic page metadata and ID tie-break ordering.
- Verify camelCase properties/dictionaries, exact enum strings, rejected integer/unknown enums, `DateOnly` and UTC timestamp forms, JSON numeric decimals, nullability, authoritative snapshots/totals, and reflection-disabled serialization of every schema.
- Verify malformed JSON, empty body, wrong content type, oversized payload, positive ID checks, validation path conversion, safe 401/403/404/409/412/428/500 problems, correlation propagation, no internal detail, and disconnected-request cancellation.
- Verify CORS preflight/success/rejection for every allowed origin/header/method, exposed headers, wildcard/credential prohibition, HTTPS rejection outside Development, and secret-free live/ready responses including database-unready behavior.

### Concurrency, idempotency, lifecycle, and conversion

- GET/create/update/transition responses expose the expected strong ETag. Missing, weak, wildcard, multiple, malformed, and stale `If-Match` cases produce the specified result and never overwrite newer state. Two users updating the same draft yield one winner and one `412`.
- Each create route is tested with no key, an exact sequential replay, same-key/different-body conflict, same key across users/operations, rollback after reservation, and at least two simultaneous identical calls. Assert one resource/audit/ledger completion and stable Location.
- Through HTTP, cover every legal and illegal Quote transition; draft edit/delete versus non-draft rejection; every Sales Order transition and cancellation edge; shipping with atomic active carrier/tracking versus missing/inactive inputs; and terminal read-only behavior.
- Convert one accepted Quote twice and concurrently without `If-Match`. Assert first response `201`, all replays `200`, all return the same order ID/number/Location, and SQL contains one order, relationship, history set, and conversion audit. Cover every non-accepted/deleted Quote state and an acceptance/conversion race.
- Change/deactivate source customer, address, products, and carrier after conversion and prove detail responses retain transaction snapshots. Verify referenced master-data deletion responses and stale-write behavior remain consistent through the API.
- Execute all ten end-to-end acceptance scenarios from the functional requirements through the hosted API, including Dashboard and read-only manager behavior.

## Documentation, runbook, and CI finalization

- Rewrite `examples/beacon-ar/README.md` from “foundation” to the complete release-one example. Summarize architecture, endpoint/OpenAPI locations, authentication model, prerequisites, local startup, database publication, test commands, client generation, and links to all four task records.
- Add `docs/beacon-ar-api-contract/runbook.md` covering Entra API/SPA registrations, Authorization Code with PKCE, audience/issuer/permission configuration, local non-production signed-token setup, known SPA origins, HTTPS/proxy rules, database/bootstrap, `/alive` and `/health`, obtaining/calling with a token, ETag update flow, idempotent create retry, OpenAPI/client generation, correlation-based diagnosis, safe logging, readiness failure triage, and ledger retention/cleanup. Use placeholders only and never add secrets or real tenant IDs.
- Update `.env.example`, `appsettings.json`, `appsettings.Development.json`, and launch settings with safe documented configuration. Do not weaken production defaults to make local startup easier.
- Extend `.github/workflows/quality.yml` after the existing database publication step to run the Web API integration suite, generate/validate the OpenAPI v1 artifact, generate the NSwag Angular client, perform strict TypeScript compilation, upload the versioned artifacts, and run the complete Paradigm/package/security gates.
- The task workflow later adds `change-summary.md` and `review-feedback.md` in this directory; the implementation plan itself is the only artifact created by the planner.

## Implementation sequence

1. Add Access identity contracts/repository/Provider and the scoped operation-context bootstrap; complete identity race and inactive-user tests.
2. Add durable creation-idempotency contracts/repository and integrate the six Provider create workflows in their existing transactions; remove the conversion `expectedVersion`; complete Provider and SQL race tests.
3. Add WebApi-owned request services, ETag/idempotency parsers, Problem Details/error mapping, source-generated JSON, authentication/policies, CORS, HTTPS enforcement, DI discovery, and health hardening.
4. Implement the seven thin controllers and all catalog actions with explicit authorization, binding, statuses, headers, cancellation, and API-description metadata.
5. Add build-time OpenAPI 3.0 generation/transformers and contract tests. Add deterministic NSwag Angular generation and the strict TypeScript consumer probe.
6. Complete real-host authorization, contract, lifecycle, concurrency, idempotency, conversion, CORS/HTTPS/health, and all ten end-to-end scenarios against freshly published SQL Server.
7. Finalize README, runbook, safe configuration, CI artifact publication, package review records, and deterministic generation checks.

## Verification

Run from `examples/beacon-ar`:

```powershell
dotnet tool restore --add-source ../../artifacts
dotnet restore BeaconAr.slnx --property:RestoreAdditionalProjectSources=../../artifacts
dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release
dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution BeaconAr.slnx --strict --format json
./build/regenerate-persistence.ps1
git diff --exit-code -- src/BeaconAr.Domain/Receivables/Generated src/BeaconAr.Data/Receivables/Generated
dotnet build BeaconAr.slnx --configuration Release --no-restore
dotnet test --solution BeaconAr.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 1
dotnet tool run paradigm doctor --project BeaconAr.slnx
dotnet tool run paradigm packages check --project BeaconAr.slnx
dotnet tool run paradigm packages audit --project BeaconAr.slnx
dotnet tool run paradigm validate --project BeaconAr.slnx
dotnet tool run paradigm checks run --project src/BeaconAr.Domain/BeaconAr.Domain.csproj
dotnet tool run paradigm checks run --project src/BeaconAr.Data/BeaconAr.Data.csproj
dotnet tool run paradigm checks run --project src/BeaconAr.Providers/BeaconAr.Providers.csproj
dotnet tool run paradigm checks run --project src/BeaconAr.WebApi/BeaconAr.WebApi.csproj
./start.sh doctor
dotnet tool run aspire restore
```

Run `BeaconAr.Database.IntegrationTests` and `BeaconAr.WebApi.IntegrationTests` with `ConnectionStrings__DatabaseConnection` targeting a freshly published disposable SQL Server database. Then generate `artifacts/openapi/beacon-ar-v1.json`, run the OpenAPI contract verifier, generate the Angular client, and run `npm ci` plus `npx tsc --noEmit` in `tests/BeaconAr.ClientContract`. A missing SQL Server, OIDC metadata/test signing configuration, Node runtime, or npm restore must be reported as an unverified acceptance gap; do not claim the relevant race, authorization, or generated-client scenarios passed.

## Final acceptance checklist

- Every required `/api/v1` endpoint exists once, no unintended endpoint is exposed, and all controllers are thin `ControllerBase` adapters with correct read/write policy metadata.
- JWT bearer validation checks signature, issuer, audience, lifetime, and exact scopes/roles. Missing/invalid credentials return 401; insufficient/inactive users return 403; `/me` agrees with policy evaluation.
- Every business mutation uses the authenticated local user and W3C correlation value in its existing audit transaction.
- Create/update/action/delete status codes are exact; Location, ETag, `If-Match`, and creation idempotency behave correctly under retry and concurrency. Quote conversion remains a singleton without an `If-Match` requirement and returns 201 once, then 200.
- All expected failures use safe RFC 7807 `application/problem+json` with stable code, request-shaped error paths, and correlation ID; no stack trace, SQL, secret, token, personal field, or raw idempotency key is disclosed or logged.
- JSON is camelCase, enums are camel-case strings, calendar dates are `YYYY-MM-DD`, timestamps are UTC ISO 8601, and decimals remain authoritative JSON numbers. Reflection-disabled serialization covers every nested/generic endpoint type.
- CORS trusts only configured SPA origins, HTTPS is enforced without bearer/preflight redirects outside local development, and `/alive`/`/health` are anonymous, bounded, and secret-free.
- The OpenAPI 3.0 v1 artifact fully describes operations, security, validation, errors, headers, enums, dates, decimals, paging, and examples; the NSwag-generated Angular client compiles with strict TypeScript settings.
- Real-pipeline tests cover auth, every lifecycle edge, reference protection, stale writers, idempotent create races, concurrent/retried conversion, snapshots, Dashboard, health, CORS, HTTPS, and all ten end-to-end scenarios.
- README, runbook, safe configuration, CI quality/security checks, and versioned OpenAPI/TypeScript artifact publication are complete and reproducible.
