# Beacon AR API contract change summary

## Delivered

- Added all 35 required `/api/v1` operations across identity, dashboard, four master-data resources, quotes, singleton conversion, and sales orders with stable operation IDs and exact runtime statuses.
- Added JWT/OIDC resource-server validation, exact `scp`/`roles` permission evaluation, read/write policies, atomic local `(issuer, subject)` provisioning, inactive-user denial before profile synchronization, and one request audit/correlation context. Symmetric signing is rejected outside the isolated test environment and configured only by hosted test factories.
- Added strong ETag output and strict `If-Match` parsing, including `428`, malformed-tag rejection, stale `412`, and conversion's intentional no-precondition contract.
- Added SQL-ledger creation idempotency using hashed keys and canonical fingerprints in the same Unit-of-Work transaction as creation and audit writes, including replay, incompatible reuse, in-progress, rollback, and concurrent-winner behavior.
- Added unified safe RFC 7807 handling, camel-case/string-enum source-generated JSON, UTC timestamp output, 1 MiB bodies, explicit CORS, non-redirecting production HTTPS enforcement, and anonymous status-only liveness/readiness.
- Added deterministic OpenAPI 3.0 output with bearer security, stable operation IDs, success headers, typed Problem Details, validation/nullability/decimal metadata, and request/response examples. The current host generates the document and Development-only Swagger UI through Swashbuckle; `scripts/generate-openapi.ps1` refreshes the checked artifact. Failure responses are operation-specific: JSON-body transport failures, conditional preconditions, conflicts, validation, and not-found cases appear only where runtime can produce them. An artifact-level verifier loads the emitted JSON and enforces the exact sets before NSwag generation and strict TypeScript compilation.
- Added an exhaustive hosted security and operations matrix covering all 35 endpoint policies, bearer-token validation and exact scope/role implications, CORS and preflight, production HTTPS, healthy/unhealthy liveness and readiness, every Quote and Sales Order lifecycle branch, conditional mutations, protected deletion, all six idempotent creation routes under cross-host concurrency and changed payloads, every quote-conversion source state, and the ten end-to-end acceptance scenarios.
- Updated package pins, DI discovery, solution membership, safe configuration, README, runbook, and CI artifact generation.

## Dependency review

The current direct host packages are `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Identity.Web` 4.14.2, `Microsoft.OpenApi` 2.11.0, and `Swashbuckle.AspNetCore` 10.2.3; hosted tests use `Microsoft.AspNetCore.Mvc.Testing`, and the client workflow retains the centrally pinned `NSwag.CodeGeneration.TypeScript`. The packages and their upstream repositories use MIT licensing. Swashbuckle brings `Microsoft.Extensions.ApiDescription.Server` transitively; the host no longer directly references the competing `Microsoft.AspNetCore.OpenApi` pipeline.

## Verification evidence

- `BeaconAr.WebApi` and `BeaconAr.WebApi.Tests` build with warnings as errors.
- A complete Release run against a freshly published SQL Server 2025 LocalDB database passed all 21 database integration tests and all 46 hosted Web API tests.
- OpenAPI generation now produces `artifacts/openapi/beacon-ar-v1.json` with 18 paths and 36 unique operations: the 35 protected versioned operations plus the deliberately anonymous root. The verifier enforces exact operation-specific response sets, including 14 JSON-body `413`/`415` pairs, 14 conditional `412`/`428` pairs, and 21 conflict-capable mutations, together with security, headers, generated-view schemas, examples, bounds, nullability, dates, and fixed-precision decimals.
- NSwag produced the Angular client and `npm run check` passed under strict TypeScript.
- Source-built Paradigm semantic checks pass for WebApi, Providers, and Domain. `paradigm validate` reports only the documented generated-entity setter warnings.
