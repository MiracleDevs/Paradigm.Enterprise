---
name: paradigm-build-web-api
description: Implement or review Paradigm.Enterprise Web API controllers, routes, authorization, endpoint exposure, exception translation, JSON contexts, multipart handling, and dependency registration. Use when exposing a Provider through HTTP or securing an existing Paradigm endpoint.
---

# Build a Paradigm Web API

## Choose security before a base

For protected endpoints, derive directly from ASP.NET Core `ControllerBase`, inject the Provider interface, and apply/test the host's authorization policy.

Paradigm controller bases inherit `AllowAnonymous`. `[Authorize]` and fallback policies do not override it. Use those bases only for deliberately anonymous routes or when an independently enforced and tested filter such as `ApiAuthorizationAttribute` supplies the required authorization.

Read [controller patterns](references/controller-patterns.md) before selecting a generic base.

## Keep controllers thin

Own routing, binding, transport validation, authorization metadata, cancellation, response status, and exception-to-response integration. Call Providers for use cases. Never resolve a repository or `DbContext`; never duplicate domain rules.

Expose only required actions. `ExposeEndpoint` plus `AddEndpointExposureControl` limits route exposure but is not authentication or authorization.
Prefer purpose-built request/command types for writes. Do not bind persistence entities or broad read views when doing so permits overposting; map only allowed fields at the boundary or in the Provider.

## Complete host integration

- Register database/context/Unit of Work and configured services before convention discovery.
- Pass explicit module assemblies when they are not reachable from the entry assembly.
- Register exception matchers and safe fallback responses; do not disclose stack traces, SQL, secrets, or personal data.
- If reflection JSON metadata is disabled, add every request, response, and nested type to a registered application `JsonSerializerContext`.
- Configure request sizes, content types, streaming, and file inspection in the host.

Framework reflection-based discovery is permitted. Avoid ad hoc reflection in runtime business code.

## Verify

Run `dotnet tool run paradigm validate --project <solution>`. Pass request cancellation only to Provider methods whose contracts accept a token; current generic CRUD methods do not. Integration-test anonymous, underprivileged, valid, invalid, missing, concurrent, conflicting, oversized, cancelled custom operations, and unexpected-error cases through the real middleware pipeline.
