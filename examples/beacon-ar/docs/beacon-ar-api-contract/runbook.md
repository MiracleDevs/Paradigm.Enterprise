# Beacon AR API runbook

## Identity registration

Register one Entra API application and expose delegated scopes and, if needed, roles assignable to signed-in users whose configured values map to `business.read` and `business.write`. Configure `AzureAd__Instance`, `AzureAd__TenantId`, and `AzureAd__ClientId` for `Microsoft.Identity.Web`; keep `AzureAd__AllowWebApiToBeAuthorizedByACL=true` so authenticated tokens without either grant reach the application's exact authorization policies and receive `403` rather than being rejected as malformed credentials. Register the Angular SPA as a public client using Authorization Code with PKCE and its exact redirect URIs. Never configure a client secret in the SPA or API, and never send an ID token to this resource server.

Release one accepts Microsoft Entra v2 delegated user access tokens issued for the Beacon AR API audience; it never accepts an ID token as an API credential. Microsoft Identity Web validates signature, issuer, tenant, audience, and lifetime. The token must contain non-empty `oid` and `sub` claims, which are identity inputs rather than the actor-type discriminator. If Entra emits `idtyp`, it must be `user`; `app` and unknown values receive `403`. If `idtyp` is absent, the token must contain non-empty delegated `scp`; roles-only and no-scope shapes receive `403` before local-user lookup or provisioning. A token containing both `scp` and `roles` is delegated, and exact roles assigned to that user can grant configured permissions. Do not grant daemon/application permissions for this API until a separate service-principal audit actor is implemented. A display value in `name` or `preferred_username` is also required before a delegated user can be provisioned; `email` remains optional.

The application rejects `Authentication__SigningKey` outside the isolated `Testing` environment, and production always uses OIDC signing metadata. Hosted tests configure their symmetric validation key in the test host rather than application startup. Never place a test key in application settings or commit one.

## Browser, HTTPS, and proxy configuration

Set `Cors__AllowedOrigins__0` (and subsequent indices) to exact HTTPS SPA origins. Wildcards, reflected origins, credentials, and unlisted headers are not enabled. The browser may read `ETag`, `Location`, and `Idempotency-Replayed`.

Outside Development/Testing, HTTP business requests are rejected with `https_required`, not redirected with their bearer token. Terminate TLS only at a configured trusted proxy and preserve the effective HTTPS scheme. The current host does not trust arbitrary forwarded headers.

## Database and startup

Use `./start.sh doctor` before starting managed mode. Managed mode builds the repository-owned database-bootstrap image and publishes the DACPAC through its finite container; SQLCMD and SqlPackage are image-owned tools rather than workstation prerequisites. External mode needs an encrypted `ConnectionStrings__DatabaseConnection` and never imports the optional local BACPAC. `/alive` reports process liveness. `/health` includes the bounded SQL readiness check and returns no connection or exception detail.

## Calling the API

Acquire an access token for the API audience, then call `GET /api/v1/me`. For a safe update:

1. `GET /api/v1/products/{id}` and retain its quoted `ETag` response header.
2. Send the full replacement to `PUT /api/v1/products/{id}` with `If-Match: "..."`.
3. On `412 concurrency_conflict`, fetch the current representation and reconcile; never blindly overwrite.

For a retriable creation, generate one opaque 1-128 visible-ASCII `Idempotency-Key` and reuse it only for an exact retry by the same user and operation. Exact retries return the original `201` representation and `Idempotency-Replayed: true`; changed payloads return `409 idempotency_key_reused`. The SQL ledger stores hashes and resource identity, not bodies or raw keys, and defaults to a 24-hour expiration timestamp. Cleanup must retain completed rows through their expiry and must not guess the outcome of unexpired `in_progress` rows.

Quote conversion uses `PUT /api/v1/quotes/{id}/sales-order` without `If-Match`: the first conversion returns `201`, and every sequential or concurrent replay returns the same order with `200`.

## OpenAPI and client generation

Run `./scripts/generate-openapi.ps1` to build the API in Release, start an isolated Development host, and regenerate the checked `artifacts/openapi/beacon-ar-v1.json` contract. The script validates the JSON and always stops the host it created. Runtime OpenAPI JSON at `/openapi/v1.json` and Swagger UI at `/swagger` are available anonymously only in Development; neither is served in Testing or production. Run the `openapi-typescript` CodeGenerator target and the pinned strict TypeScript probe exactly as shown in the example README. CI publishes both files as `beacon-ar-api-v1`.

## Diagnosis

Use the response `correlationId` (the W3C trace ID) to find structured server telemetry. Logs intentionally exclude tokens, idempotency keys, request bodies, customer contact/address data, SQL, and configuration secrets. A readiness failure means the process is alive but SQL is unavailable or unhealthy; check database availability, TLS, credentials in the secret store, and DACPAC publication before restarting. A generic `internal_error` should be investigated from protected telemetry; stack traces are never sent to callers.
