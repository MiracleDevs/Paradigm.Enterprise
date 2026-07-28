# Secure host configuration

Paradigm.Enterprise supplies integration points, not a complete security model. The host must choose authentication, define authorization policy, protect configuration, and test negative cases. Security should be designed around the application's assets, actors, trust boundaries, and data flows rather than added after endpoint implementation.

## Threat model and responsibility

Identify where untrusted data enters, where caller identity is established, which actions cross a privilege boundary, and where sensitive information is stored or transmitted. Revisit that model when an endpoint, integration, or deployment boundary changes.

The framework can bind a request, call a provider, persist data, and translate exceptions. It does not decide who may perform a use case, which fields are sensitive, whether a repeated request is safe, or how an uploaded file is inspected. Turn those decisions into host policy, provider checks, domain rules, and negative tests at the boundary that owns them.

## Controller authorization

`ApiControllerBase`, `ReadApiControllerBase`, and `EditApiControllerBase` carry inherited `AllowAnonymous` metadata. ASP.NET Core authorization middleware bypasses authorization when an endpoint has that metadata. Adding `Authorize` to a derived controller or action, or configuring a fallback policy, does not override `AllowAnonymous`.

Use the library controller bases only for endpoints that are deliberately public. A protected endpoint needs a controller that derives directly from ASP.NET Core's `ControllerBase`, injects its provider, and carries the application's authorization metadata. Changing the attributes on the library bases would change framework behavior and is outside the scope of host configuration.

Test unauthenticated and unauthorized requests for every exposed route. A successful authenticated request does not prove that a caller with the wrong permissions is rejected.

## Endpoint exposure is not authorization

`AddEndpointExposureControl` enables a filter that hides actions without `ExposeEndpoint`. It is useful for preventing inherited or accidental actions from becoming routable. It does not establish caller identity and does not evaluate permissions.

Use exposure control and authorization for their separate purposes. Returning not found for an unexposed route is not evidence that the route is secured. Enabling exposure control does not counteract `AllowAnonymous`.

## CORS and Swagger

The template may start with permissive CORS and development-only Swagger. Replace permissive origins, methods, and headers with the smallest production policy required by known clients. Keep Swagger access aligned with the application's threat model and environment.

Swagger security definitions describe how the UI sends credentials. They do not configure ASP.NET Core authentication.

## Secrets and managed identity

Keep database, Redis, storage, and email credentials out of source-controlled settings. Prefer managed identity where the target service supports it. Use user secrets only for local development and use the deployment platform's secret store for hosted environments.

When a service supports both a connection string and managed identity, document which source takes precedence. The cache service uses a named connection string when present and otherwise attempts its managed-identity configuration.

Apply least privilege to the host identity, database account, service credentials, deployment identity, and operational access. Private networking and encrypted transport reduce exposure, but they do not replace authentication or authorization. Rotate and revoke secrets through the owning platform rather than embedding credential lifecycle logic in providers.

## Serialization and input handling

Source-generated JSON metadata reduces reflection and makes the serialized surface explicit. It does not validate business meaning. Combine transport validation, domain validation, request-size limits, content-type checks, and file-content inspection where appropriate.

For multipart streaming, configure server and proxy limits together. The library attributes do not scan uploads or make arbitrary file content safe.

Minimize the fields accepted and returned by each contract. A persistence entity is rarely an appropriate public request or response merely because it serializes successfully. Treat mass assignment, overexposure, replay, and resource exhaustion as design concerns, then verify the chosen protections with integration tests.

## Safe failures and telemetry

Return stable, minimal error information and keep detailed exceptions in protected logs. Avoid logging tokens, connection strings, personal data, file content, or full request bodies by default.

Health endpoints should reveal only the information required by the intended caller. The template's HTML health response is a presentation example, not a safe public diagnostic contract.

Defense in depth may also require rate limits, request timeouts, network policy, encryption, audit records, and platform monitoring. These are host or platform controls. Paradigm.Enterprise does not enable them automatically.

The [Secure delivery](secure-delivery.md) guide explains how threat modeling, pipeline controls, artifact promotion, observability, and recovery fit around the running host.
