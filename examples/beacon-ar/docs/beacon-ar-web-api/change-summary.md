# Beacon AR Web API change summary

## Delivered

- Kept all 35 business operations on secure, explicitly authorized `ControllerBase` controllers while moving the four master-data response bodies and page items to generated `ProductView`, `CustomerView`, `CustomerAddressView`, and `CarrierView` contracts.
- Kept purpose-built create/update/delete request models, strong ETags, conditional writes, idempotency, audit behavior, and safe lifecycle coordination. The master-data Providers inherit the official `EditProviderBase` and now expose generated-view API methods; legacy DTO methods delegate to those methods so existing application callers remain compatible. Mapping remains in Providers, never repositories.
- Added a deliberately anonymous `GET /` response containing only `name` and assembly product `version`, plus a status-only anonymous `HEAD /` endpoint.
- Replaced the built-in ASP.NET OpenAPI pipeline with `Swashbuckle.AspNetCore` 10.2.3, preserving operation IDs, security, Problem Details, request/response examples, validation constraints, ETag and idempotency headers, and the existing custom Swagger styling. Swagger JSON and UI are available anonymously only in Development.
- Added `scripts/generate-openapi.ps1`, which builds a Release host, generates and validates the checked `artifacts/openapi/beacon-ar-v1.json`, and stops its isolated process. Consecutive generation produced identical SHA-256 hashes.
- Replaced custom production bearer configuration with `Microsoft.Identity.Web` 4.14.2 and the `AzureAd` configuration convention. The isolated Testing signing key remains fail-closed outside Testing, and all challenge/forbidden responses retain safe RFC 7807 bodies.
- Made the release-one actor model delegated-user-only. A standard authorization requirement requires `oid` and `sub`, trusts only explicit `idtyp=user`, and otherwise requires delegated `scp`; explicit application/unknown types plus absent-`idtyp` roles-only or no-scope tokens are rejected before `CurrentUserMiddleware`. Tokens with both `scp` and user-carried `roles` retain exact read/write behavior.
- Added generated-view JSON metadata and a resolver modifier that preserves the established transport names `version`, `type`, `defaultBilling`, and `defaultShipping` while retaining joined display fields from the generated views.
- Updated configuration examples, API runbook, package locks, artifact validation, token fixtures, and hosted tests for Entra-compatible tenant/object claims, anonymous discovery, Development-only documentation, and the 36-operation OpenAPI contract.

## Dependencies

- `Microsoft.Identity.Web` 4.14.2: Microsoft-maintained, MIT licensed, compatible with `net10.0`; used for the standard Entra bearer API configuration requested for the example.
- `Swashbuckle.AspNetCore` 10.2.3: MIT licensed and compatible with `net10.0`; used for Swagger generation and UI. Its API description support remains transitive rather than a direct application dependency.
- Removed direct `Microsoft.AspNetCore.OpenApi` and `Microsoft.Extensions.ApiDescription.Server` references so the host has one OpenAPI generation pipeline.

## Verification

- Release build succeeds with zero warnings and zero errors.
- All 37 database-independent hosted Web API tests pass, including the 11-test API contract suite, the 7-test security matrix, the 11-test host-hardening suite, the 2-test root/runtime-documentation suite, and the artifact verifier.
- The generated artifact verifier passes and checks 18 paths and 36 unique operations, including one anonymous root and 35 bearer-protected business operations.
- Consecutive OpenAPI regeneration produces SHA-256 `3C828B18A0CA495AC85A1DFE8126A18ED5C212428855D385741BCD83FA423F0C` and leaves no Beacon AR host process running.
- The full executable suite reports 122 passed, 41 skipped, and 0 failed across 163 tests. The skips are the 22 database-integration and 19 HTTP-acceptance tests when `ConnectionStrings__DatabaseConnection` is not supplied.
- `paradigm validate` succeeds for the complete solution. `paradigm checks run` succeeds for WebApi, Providers, and Domain; its solution-wide invocation is blocked only by the checker's inability to resolve Aspire's generated `Projects` namespace, as recorded in `decisions.md`.
- `paradigm doctor` and `paradigm packages check` succeed. Package audit completes with only pre-existing available-update warnings and no deprecation or vulnerability finding for Microsoft Identity Web or Swashbuckle.

## Guidance promoted

- The Web API skill now requires an explicit delegated-user/service-principal actor decision before current-user provisioning and documents the Microsoft claim fallback: explicit `idtyp`, otherwise `scp` for delegated tokens and roles-without-scope for application tokens.
- Controller guidance now requires the actor requirement on every default/fallback/named policy and tests legacy app tokens with distinct `oid`/`sub`; identifier comparison is explicitly rejected as token classification.
- Review guidance now requires real-host startup tests for every mandatory production identity/CORS setting and the complete production path without a live metadata dependency.
- Skill and plugin metadata validation pass after these changes.
