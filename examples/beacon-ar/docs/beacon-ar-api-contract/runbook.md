# Beacon AR API runbook

## Identity registration

Register one Entra API application and expose delegated scopes/app roles whose configured values map to `business.read` and `business.write`. Set `Authentication__Authority`, `Authentication__Issuer`, and `Authentication__Audience` to the tenant v2 issuer and this API's application-ID URI. Register the Angular SPA as a public client using Authorization Code with PKCE and its exact redirect URIs. Never configure a client secret in the SPA or API, and never send an ID token to this resource server.

The application rejects `Authentication__SigningKey` outside the isolated `Testing` environment, and production always uses OIDC signing metadata. Hosted tests configure their symmetric validation key in the test host rather than application startup. Never place a test key in application settings or commit one.

## Browser, HTTPS, and proxy configuration

Set `Cors__AllowedOrigins__0` (and subsequent indices) to exact HTTPS SPA origins. Wildcards, reflected origins, credentials, and unlisted headers are not enabled. The browser may read `ETag`, `Location`, and `Idempotency-Replayed`.

Outside Development/Testing, HTTP business requests are rejected with `https_required`, not redirected with their bearer token. Terminate TLS only at a configured trusted proxy and preserve the effective HTTPS scheme. The current host does not trust arbitrary forwarded headers.

## Database and startup

Use `./start.sh doctor` before starting managed mode. Managed mode publishes the DACPAC once through `BeaconAr.DatabaseBootstrap`; external mode needs an encrypted `ConnectionStrings__DatabaseConnection`. `/alive` reports process liveness. `/health` includes the bounded SQL readiness check and returns no connection or exception detail.

## Calling the API

Acquire an access token for the API audience, then call `GET /api/v1/me`. For a safe update:

1. `GET /api/v1/products/{id}` and retain its quoted `ETag` response header.
2. Send the full replacement to `PUT /api/v1/products/{id}` with `If-Match: "..."`.
3. On `412 concurrency_conflict`, fetch the current representation and reconcile; never blindly overwrite.

For a retriable creation, generate one opaque 1-128 visible-ASCII `Idempotency-Key` and reuse it only for an exact retry by the same user and operation. Exact retries return the original `201` representation and `Idempotency-Replayed: true`; changed payloads return `409 idempotency_key_reused`. The SQL ledger stores hashes and resource identity, not bodies or raw keys, and defaults to a 24-hour expiration timestamp. Cleanup must retain completed rows through their expiry and must not guess the outcome of unexpired `in_progress` rows.

Quote conversion uses `PUT /api/v1/quotes/{id}/sales-order` without `If-Match`: the first conversion returns `201`, and every sequential or concurrent replay returns the same order with `200`.

## OpenAPI and client generation

Build `BeaconAr.WebApi` in Release to produce `artifacts/openapi/beacon-ar-v1.json`. Run the `openapi-typescript` CodeGenerator target and the pinned strict TypeScript probe exactly as shown in the example README. CI publishes both files as `beacon-ar-api-v1`.

## Diagnosis

Use the response `correlationId` (the W3C trace ID) to find structured server telemetry. Logs intentionally exclude tokens, idempotency keys, request bodies, customer contact/address data, SQL, and configuration secrets. A readiness failure means the process is alive but SQL is unavailable or unhealthy; check database availability, TLS, credentials in the secret store, and DACPAC publication before restarting. A generic `internal_error` should be investigated from protected telemetry; stack traces are never sent to callers.
