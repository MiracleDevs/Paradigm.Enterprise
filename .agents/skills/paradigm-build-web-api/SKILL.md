---
name: paradigm-build-web-api
description: Implement or review Paradigm.Enterprise Web API controllers, routes, authorization, endpoint exposure, exception translation, JSON contexts, multipart handling, and dependency registration. Use when exposing a Provider through HTTP or securing an existing Paradigm endpoint.
---

# Build a Paradigm Web API

Read and apply [Paradigm Good Coding Practices](../../references/good-coding-practices.md) before creating or editing source. Re-check current primary security guidance when authentication, browser token handling, or another security feature is in scope.

## Choose security before a base

For protected endpoints, derive directly from ASP.NET Core `ControllerBase`, inject the Provider interface, and apply/test the host's authorization policy.

Paradigm controller bases inherit `AllowAnonymous`. `[Authorize]` and fallback policies do not override it. Use those bases only for deliberately anonymous routes or when an independently enforced and tested filter such as `ApiAuthorizationAttribute` supplies the required authorization.

Read [controller patterns](references/controller-patterns.md) before selecting a generic base.

## Keep controllers thin

Own routing, binding, transport validation, authorization metadata, cancellation, response status, and exception-to-response integration. Call Providers for use cases. Never resolve a repository or `DbContext`; never duplicate domain rules.

Expose only required actions. `ExposeEndpoint` plus `AddEndpointExposureControl` limits route exposure but is not authentication or authorization.
Prefer purpose-built request/command types for writes. Do not bind persistence entities or broad read views when doing so permits overposting; map only allowed fields at the boundary or in the Provider.
When a search or filter action has several related query values, bind one purpose-built `[FromQuery]` request object and pass it intact to the Provider. Keep ASP.NET Core attributes on the action parameter rather than in Domain, keep paging/filter/sort rules on the request, and regression-test the flat public query names, defaults, OpenAPI document, and generated client.

## Complete host integration

- Register database/context/Unit of Work and configured services before convention discovery.
- Pass explicit module assemblies when they are not reachable from the entry assembly.
- Register exception matchers and safe fallback responses; do not disclose stack traces, SQL, secrets, or personal data.
- If reflection JSON metadata is disabled, add every request, response, and nested type to a registered application `JsonSerializerContext`.
- Configure request sizes, content types, streaming, and file inspection in the host.
- Prefer an encrypted `Secure`/`HttpOnly` authentication cookie and CSRF protection for browser clients instead of storing tokens in `localStorage`; use bearer tokens when a non-browser client or API threat model requires them.
- For Microsoft Entra bearer APIs, decide whether the application actor model permits delegated users,
  service principals, or both before provisioning an application user. Treat an explicit `idtyp` as
  authoritative. For legacy tokens without `idtyp`, use permission claims: `scp` identifies delegated
  authorization, while `roles` without `scp` is application-only. Never infer a human actor by comparing
  `oid` and `sub`; both claims can exist and differ for a service principal.
- Fail closed outside local/test environments when required identity and CORS configuration is absent.
  Regression-test each missing setting through actual host startup, plus the fully configured production
  path without depending on a live metadata endpoint.
- Configure structured logging, standard trace propagation, useful metrics, and separate liveness/readiness health checks. Prefer framework and already-approved host integrations.

Framework reflection-based discovery is permitted. Avoid ad hoc reflection in runtime business code.

## Verify

Run `dotnet tool run paradigm validate --project <solution>` and `dotnet tool run paradigm checks run --project <solution>`. Pass request cancellation only to Provider methods whose contracts accept a token; current generic CRUD methods do not. Integration-test anonymous, underprivileged, valid, invalid, missing, concurrent, conflicting, oversized, cancelled custom operations, CSRF defenses for cookie-authenticated writes, trace propagation, health states, and unexpected-error cases through the real middleware pipeline.
