# Beacon AR Web API implementation plan (Task 4)

> **Historical Provider/API note (superseded 2026-08-04):** This plan preserves evidence and decisions from an earlier implementation stage. Current ownership is: repositories materialize canonical views; entities own intrinsic invariants; request objects own query and proposed-input rules; Providers invoke those rules and own only collaborator-dependent orchestration and transactions; MasterData exposes explicit request-based `IProvider` contracts rather than `IEditProvider`, `EditProviderBase`, or a fail-closed mutation guard; and controllers bind one `[FromQuery]` request and own HTTP concerns. See [the current Provider/API decisions](../beacon-ar-provider-api-conventions/decisions.md).

## Outcome

Keep the release-one Beacon AR HTTP contract secured and stable while integrating the Task 3 generated `*View` provider/repository migration. The project will use `ControllerBase`-based, policy-protected controllers for all business operations; it will not expose the installed Paradigm generic controller routes for the protected master-data surface. Add a deliberately anonymous API root returning the project name and version, replace the current built-in OpenAPI/transformer path with Swashbuckle for the requested interactive documentation, and move Azure authentication configuration to the current Microsoft Identity Web bearer-API pattern.

No source changes or package additions are part of this plan.

## Evidence and governing decisions

### Installed Paradigm controller behavior

The installed source in `src/Paradigm.Enterprise.WebApi` was checked directly:

- `ApiControllerBase`, `ReadApiControllerBase<...>`, and `EditApiControllerBase<...>` all carry inherited `[AllowAnonymous]`.
- `ReadApiControllerBase` exposes `POST search` and `GET get-by-id`; `EditApiControllerBase` additionally exposes `POST` save and `DELETE` delete. Their contracts use `PaginationParametersBase`, `IReadProvider`/`IEditProvider`, and an entity-derived `TView`.
- The base actions are marked `[ExposeEndpoint]`, but that marker only works after `AddEndpointExposureControl()` is registered, and it controls discoverability (404) rather than authentication.
- Standard `[Authorize]` metadata and fallback policies do **not** override the inherited anonymous metadata. `ApiAuthorizationAttribute` can independently enforce the framework's `x-api-auth`/`ClientSecrets` mechanism, but that is a different client-secret security model and is not Beacon AR's bearer-token model.

Consequently, neither `ReadApiControllerBase` nor `EditApiControllerBase` is safe for Beacon AR's secured business routes. This is consistent with the security test suite, which asserts that every current controller endpoint has no `IAllowAnonymous` metadata.

### Controller decision

Use direct `ControllerBase` controllers for `ProductsController`, `CustomersController`, `AddressesController`, `CarriersController`, `QuotesController`, `SalesOrdersController`, `DashboardController`, and `MeController`.

- Retain `[ApiController]`, `api/v1/...` routes, `[Authorize(Policy = BeaconPolicies.Read)]` at controller/read-action level, and `[Authorize(Policy = BeaconPolicies.Write)]` for mutations.
- Retain the existing fallback authenticated policy, bearer challenge/forbidden Problem Details, CORS, HTTPS enforcement, request limits, ETags/`If-Match`, creation idempotency, explicit response status codes, and operation names.
- Continue to inject provider interfaces only. Controllers never resolve repositories, contexts, or units of work.
- Keep purpose-built create/update/transition request contracts at the transport boundary; do not bind generated entities or broad generated views to writes, because that would reintroduce overposting risk.
- Return the appropriate generated `ProductView`, `CustomerView`, `CustomerAddressView`, and `CarrierView` (or deliberately defined transport view DTOs that are one-to-one adapters) from the affected master-data actions after Task 3. The final selection must preserve the externally documented JSON shape, ETag version source, nullability, and pagination semantics. Quote, sales-order, dashboard, and identity response DTOs remain named workflow/read DTOs.

This preserves the Task 3 fail-closed conclusion: no inherited generic master-data mutation route is mapped. In particular, do not add `AddEndpointExposureControl`, do not add a derived generic CRUD controller, and do not publish unprotected `POST /`, `DELETE /`, `POST /search`, or `GET /get-by-id` routes as an accidental side effect. If a future anonymous or API-key product requires framework CRUD endpoints, create a separate controller, explicitly register/test `ApiAuthorizationAttribute` and endpoint exposure, and perform a dedicated threat-model and route-compatibility review first.

### Reference-project lessons

`C:\Repositories\github\microsoft\rdx-dms-mvp\src\api\Microsoft.DemoManagementSystem.WebApi` demonstrates useful structural patterns: `ControllerBase`/Paradigm controller composition, explicit DI registration before discovery, Swagger configuration, a simple root endpoint, and `UseAuthentication()` before `UseAuthorization()`. It also demonstrates why Beacon AR must not copy the implementation blindly: it relies on the now-legacy `Microsoft.AspNetCore.Authentication.AzureAD.UI`, broad CORS, and a custom authorization filter layered on anonymous controller bases. Beacon AR retains its narrower CORS and JWT/API threat model.

## Azure authentication and authorization

### Authentication migration

Replace the host's hand-configured Entra authority/audience/issuer `JwtBearer` setup with Microsoft Identity Web's current bearer API registration:

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
```

Use a new `AzureAd` configuration section with non-secret identifiers only: `Instance`, `TenantId`, and `ClientId` (and, if required by the verified deployment scenario, a documented audience/app-ID URI). Do not place tenant values, client IDs, issuer values, connection strings, signing keys, client secrets, or fallback credentials in defaults. Continue to fail startup outside Development/Testing/OpenAPI generation when any required identity value or every allowed CORS origin is absent; regression-test each independent missing value.

The local `Authentication:SigningKey` test-only path may remain isolated behind the Testing environment until Entra-token integration tests replace it; it must never be enabled in Development/Production configuration or committed defaults. Tests must configure `JwtBearerOptions` through the registered bearer scheme, rather than weakening production validation.

Do not add `Microsoft.AspNetCore.Authentication.AzureAD.UI`: NuGet marks 6.0.36 as legacy/deprecated/no longer maintained and directs users to Microsoft.Identity.Web.UI. That UI package is also unnecessary for a bearer-only API; use `Microsoft.Identity.Web` only. Microsoft's current protected Web API quickstart uses `AddMicrosoftIdentityWebApi` and an `AzureAd` section, and its authorization guidance distinguishes delegated `scp` from application `roles` claims.

### Authorization migration

Release one supports delegated Microsoft Entra user access tokens only. Reject application-only/client-credentials actors in authorization before `CurrentUserMiddleware`; do not provision a service principal as a human `ApplicationUser`. Require non-empty `oid` and `sub` identity inputs, but do not infer actor type from their relationship. When `idtyp` is present, accept only `idtyp=user`. When `idtyp` is absent, require a non-empty delegated `scp` claim; reject roles-only and no-scope token shapes. A token with both `scp` and `roles` is delegated and its user-carried roles remain eligible for exact permission evaluation. A future machine-to-machine capability requires a separately designed service-principal identity and audit model.

Retain the named `business.read` and `business.write` policies and the `PermissionEvaluator` semantics. Configure those policies to accept delegated scopes and user-assigned roles carried by a delegated user token:

- `business.read`: delegated read scope or a read role assigned to the signed-in user; write remains an implication of read.
- `business.write`: delegated write scope or a write role assigned to the signed-in user.
- `MeController`: authenticated delegated user actor plus the current completeness checks; application-only tokens are forbidden.

Use a testable ASP.NET Core authorization requirement/handler for the actor classification and named policies for the scope/role rule. Do not use a global `RequiredScope`, because delegated tokens can carry user roles and the read/write semantics remain distinct. Entra registration work is an external prerequisite: the API registration must expose the exact delegated scopes and any roles intentionally assignable to users, and client registrations must be granted them. Do not grant release-one application permissions to daemon clients.

Maintain `UseAuthentication()` before `UseAuthorization()`, preserve safe 401/403 Problem Details, keep `IdentityModelEventSource.ShowPII = false`, and never log bearer tokens or identity credentials.

## OpenAPI and Swagger decision

Adopt `Swashbuckle.AspNetCore` for Beacon AR's requested Swagger UI. The current host uses `Microsoft.AspNetCore.OpenApi`, `MapOpenApi`, and `BearerSecurityDocumentTransformer`; migrate their intended contract work to Swashbuckle configuration/filter types rather than running two competing document generators.

- Register `AddSwaggerGen` with the existing title/version (`Beacon AR API`, `v1`), bearer HTTP security definition, global/security-per-operation requirements for protected business endpoints, and XML/annotation/schema configuration as needed.
- Serve Swagger JSON and Swagger UI only in Development (or an explicit non-production documentation environment), preserving the checked-in custom stylesheet/logo. Do not make the UI anonymous in production merely because the JSON endpoint is public in development.
- Preserve a generated OpenAPI artifact under `examples/beacon-ar/artifacts/openapi/beacon-ar-v1.json` and adapt build/test generation to use the selected Swashbuckle output. The public artifact remains a contract, not a reason to expose a runtime documentation endpoint in production.
- Replace `BearerSecurityDocumentTransformer` with focused Swashbuckle document/operation/schema filters, or consolidate its logic into one application-owned configuration path. Preserve current operation IDs, bearer requirements, problem responses, JSON-only bodies, ETag/Location/Idempotency-Key/If-Match documentation, examples, enum/decimal/nullability constraints, and source-generated JSON coverage.
- Add the anonymous root operation to the OpenAPI document without a bearer security requirement; health, readiness, liveness, and the root are intentionally unauthenticated infrastructure/discovery endpoints and must be documented distinctly from business APIs.

Microsoft's current ASP.NET Core documentation notes that built-in OpenAPI is the .NET 9+ default and Swashbuckle is a manually added community package. The user requirement selects Swashbuckle, so the plan uses it intentionally rather than retaining the default in parallel.

## Anonymous root endpoint and version

Add a new application-owned root endpoint file, for example `Endpoints/ApiRootEndpoint.cs` (one semantic type per file), plus an `ApiRootResponse` record if a structured JSON response is selected. Map `GET /` explicitly after authorization middleware with `.AllowAnonymous()` and a fixed `GET`/`HEAD` metadata policy.

Return only the project name and version, e.g. `{ "name": "Beacon AR API", "version": "<assembly/informational version>" }`. Obtain the version from assembly metadata/build properties, not user input and not an environment-specific secret. Define the title/version once in an application options/metadata type used by the root response and Swagger configuration so the two cannot drift. The existing reference API's plaintext root is a useful precedence, but Beacon AR should return its deliberately versioned JSON contract to fit its API conventions.

Keep `/alive` and `/health` unchanged: both remain anonymous and secret-free; `/alive` reports process liveness and `/health` reports dependency readiness. The root endpoint must not disclose configuration, deployment environment, connection information, detailed health, assembly paths, or exception details.

## Exact files and planned changes

| File | Planned responsibility |
| --- | --- |
| `examples/beacon-ar/src/BeaconAr.WebApi/Program.cs` | Replace built-in OpenAPI registration/mapping with Swashbuckle registration/UI; configure Microsoft Identity Web bearer authentication, policies, root mapping, and preserve service registration/middleware order, CORS, health, exception, JSON, and DI behavior. |
| `examples/beacon-ar/src/BeaconAr.WebApi/BeaconAr.WebApi.csproj` | Replace obsolete built-in OpenAPI dependencies only if no longer used; add approved Microsoft.Identity.Web and Swashbuckle references; retain ApiDescription support required by the selected Swagger version. |
| `examples/beacon-ar/Directory.Packages.props` | Add centrally managed, pinned compatible versions for `Microsoft.Identity.Web` and `Swashbuckle.AspNetCore` after package approval; remove obsolete central entries only when no project references them. |
| `examples/beacon-ar/src/BeaconAr.WebApi/appsettings.json` | Replace `Authentication` placeholders with non-secret `AzureAd` configuration shape and keep permissions/origins explicit with no usable defaults. |
| `examples/beacon-ar/src/BeaconAr.WebApi/appsettings.Development.json` and `.env.example` | Document development-only identity/CORS inputs without secrets or tenant/client defaults; preserve secret injection through user secrets, environment variables, or deployment configuration. |
| `examples/beacon-ar/src/BeaconAr.WebApi/Security/PermissionEvaluator.cs` and `BeaconPolicies.cs` | Adapt/retain named read/write policy evaluation for Entra `scp` and `roles`; keep case-sensitive exact grant matching and write-implies-read behavior. |
| `examples/beacon-ar/src/BeaconAr.WebApi/Controllers/ProductsController.cs`, `CustomersController.cs`, `AddressesController.cs`, `CarriersController.cs` | Keep direct secured controller actions; adapt provider calls and response types to the Task 3 generated view contract without changing public routes or permitting generic framework mutations. |
| `examples/beacon-ar/src/BeaconAr.WebApi/Controllers/QuotesController.cs`, `SalesOrdersController.cs`, `DashboardController.cs`, `MeController.cs` | Preserve named custom workflow/read actions and current security; adjust only for shared auth/OpenAPI/root integration. |
| `examples/beacon-ar/src/BeaconAr.WebApi/OpenApi/BearerSecurityDocumentTransformer.cs` | Remove after its complete contract responsibilities are migrated to Swashbuckle filters/configuration; do not delete before equivalent artifact tests pass. |
| `examples/beacon-ar/src/BeaconAr.WebApi/OpenApi/*` | Add narrow Swashbuckle filter/configuration types for bearer security, Problem Details, headers, examples, and schema constraints as needed; keep one semantic type per file. |
| `examples/beacon-ar/src/BeaconAr.WebApi/Serialization/BeaconArApiJsonContext.cs` | Add generated master-data view or root response metadata if the endpoint/result types need it; retain every request/response/nested type required with reflection disabled. |
| `examples/beacon-ar/src/BeaconAr.WebApi/Endpoints/ApiRootEndpoint.cs` and `ApiRootResponse.cs` | New anonymous root mapping and immutable response contract, if not kept as a minimal strongly typed endpoint extension. |
| `examples/beacon-ar/tests/BeaconAr.WebApi.Tests/BeaconArApiSecurityMatrixTests.cs` | Update the business catalog only if contract types change; assert all business routes remain non-anonymous and add explicit root/health/OpenAPI anonymous expectations. |
| `examples/beacon-ar/tests/BeaconAr.WebApi.Tests/BeaconArApiContractTests.cs`, `BeaconArApiAcceptanceTests.cs`, `BeaconArApiHostHardeningTests.cs`, `BeaconArApiTransportMatrixTests.cs` | Update test hosts for Microsoft Identity Web, preserve 401/403/CORS/HTTPS/ETag/idempotency/precondition behavior, and add root/version safety coverage. |
| `examples/beacon-ar/tests/BeaconAr.WebApi.Tests/OpenApiArtifactTests.cs` | Retarget artifact generation/validation to Swashbuckle and extend it for root security/version while preserving existing operation and schema assertions. |
| `examples/beacon-ar/tests/BeaconAr.WebApi.Tests/GeneratedMappingHostTests.cs` | Update expected generated view registrations/mappings after Task 3. |
| `examples/beacon-ar/artifacts/openapi/beacon-ar-v1.json` | Regenerate only after controller and Swagger contract tests verify the intended, reviewed document. |

## Sequenced implementation

1. **Establish package and identity decisions.** Obtain explicit package approval; record the release-one delegated-user-only decision, and align Entra delegated scopes and roles assignable to users with `business.read`/`business.write`. Confirm production secret/configuration delivery and allowed origins.
2. **Create compatibility test scaffolding first.** Add root, anonymous-metadata, protected-route, Entra token-validation, scope/role, and OpenAPI contract assertions before replacing host registrations. Keep deterministic Testing-environment bearer configuration isolated from production.
3. **Add root metadata/endpoint.** Centralize project name/version, map `GET /` anonymously, register serialization metadata, and prove it returns only the allowed fields.
4. **Migrate authentication.** Add Microsoft Identity Web, move to `AzureAd`, retain bearer challenge/forbidden safe responses and the existing named authorization policy behavior; verify scope/role and malformed/expired/wrong-audience token tests.
5. **Integrate Task 3 contracts.** Update the four master-data controllers/tests to use generated views/adapters while retaining the current public routes, write request models, ETags, idempotency, and fail-closed generic base decision. Do not begin this step until Task 3 provider interfaces compile.
6. **Migrate OpenAPI to Swashbuckle.** Port transformer behavior to filters/configuration; generate the artifact, compare operation IDs/routes/security/schema/header responses, and only then remove built-in OpenAPI code and `BearerSecurityDocumentTransformer`.
7. **Harden and validate.** Exercise the real middleware pipeline and full build/analysis suite; update committed lock files only as a consequence of approved package restore.

## Package, license, and compatibility decision record

| Package | Decision | License / compatibility / rationale |
| --- | --- | --- |
| `Microsoft.Identity.Web` | Add after explicit approval | MIT; NuGet currently lists net10.0 compatibility. It is Microsoft's current bearer Web API guidance and replaces the obsolete AzureAD UI approach. |
| `Microsoft.AspNetCore.Authentication.AzureAD.UI` | Do not add | NuGet marks it legacy, deprecated, and no longer maintained; its listed 6.0.36 package is inappropriate for this net10.0 bearer API. |
| `Microsoft.Identity.Web.UI` | Do not add | The legacy package suggests it for web-app UI migration, but Beacon AR is an API and has no Razor/OIDC sign-in UI requirement. |
| `Swashbuckle.AspNetCore` | Add after explicit approval | MIT; NuGet lists net10.0 compatibility and the required ApiDescription dependency. It satisfies the requested Swagger UI/document generation. |
| `Microsoft.AspNetCore.OpenApi` / current transformer | Remove only after migration | Built-in OpenAPI is the current ASP.NET Core default, but keeping it alongside Swashbuckle would produce competing document pipelines and drift. |

Before adding packages, inspect the resolved transitive graph and run the repository's package checks/audit. Pin approved versions centrally; do not use floating ranges. The package decision is a plan, not permission to edit project files.

## Compatibility and migration guarantees

- Preserve all 35 current protected business operations, their `api/v1` routes, HTTP verbs, route names/operation IDs, authorization policies, response statuses, and Problem Details semantics unless a separately approved API-versioning change is made.
- Preserve write request DTOs and conditional/idempotent behavior; generated persistence views are read/output shapes, not a reason to widen write binding.
- Preserve existing bearer JWT clients during the Identity Web migration by configuring the same Entra tenant/audience and testing existing scope/role claims. Publish the `AzureAd` configuration migration and app-registration prerequisite before deployment.
- Keep OpenAPI JSON artifact location/version stable, but treat small document-generator rendering differences as reviewed contract changes. Compare the old/new document in CI for routes, operation IDs, security, status responses, schemas, headers, and examples.
- Do not add API versioning middleware or change the `v1` URL shape in this task. The root's assembly version is informational and independent of route version `v1`.

## Tests and validation

Run the following after implementation, with any external Entra integration test gated by test-tenant availability:

- `dotnet restore examples/beacon-ar/BeaconAr.slnx`
- `dotnet build examples/beacon-ar/BeaconAr.slnx --no-restore`
- `dotnet test examples/beacon-ar/BeaconAr.slnx --no-build`
- `dotnet tool run paradigm doctor --project examples/beacon-ar/BeaconAr.slnx`
- `dotnet tool run paradigm packages check --project examples/beacon-ar/BeaconAr.slnx`
- `dotnet tool run paradigm packages audit --project examples/beacon-ar/BeaconAr.slnx`
- `dotnet tool run paradigm validate --project examples/beacon-ar/BeaconAr.slnx`
- `dotnet tool run paradigm checks run --project examples/beacon-ar/BeaconAr.slnx`

Acceptance coverage must include:

- `GET /` is anonymous, returns only project name/version, and has no secret/configuration leakage.
- `/alive`, `/health`, and development-only Swagger/OpenAPI endpoints are anonymous only where deliberately mapped; every business controller endpoint is authenticated and lacks inherited `IAllowAnonymous` metadata.
- Missing, malformed, expired, future, invalid-signature, wrong-issuer, wrong-audience, scope-less, and role-less tokens return safe 401/403 responses as appropriate; exact delegated scopes and roles carried by delegated users produce the correct read/write matrix, with write implying read. Application-only/client-credentials actors are forbidden before user resolution.
- No `ReadApiControllerBase`/`EditApiControllerBase` generic routes are present, including their inherited search/get/save/delete actions.
- Master-data route behavior remains protected and preserves validation, request limits, CORS, ETag/If-Match, idempotency, and generated-view response serialization.
- Quote/sales-order workflow routes retain their custom transaction/status/idempotency semantics and authorization.
- Swagger artifact contains the root separately from protected operations; every protected operation declares bearer security, required headers and Problem Details remain documented, and JSON contexts serialize all updated view/root types with reflection disabled.
- Production startup rejects missing required Entra/CORS configuration and rejects test signing-key configuration; no secret defaults or tokens appear in logs/errors/OpenAPI.

## Primary-source references

- Microsoft Entra protected Web API quickstart: [Microsoft.Identity.Web bearer registration, AzureAd configuration, and protected-controller guidance](https://learn.microsoft.com/en-us/entra/msidweb/getting-started/quickstart-webapi).
- Microsoft Entra authorization guidance: [scope, application-role, and named-policy patterns](https://learn.microsoft.com/en-us/entra/msidweb/authentication/authorization).
- NuGet: [`Microsoft.AspNetCore.Authentication.AzureAD.UI` is legacy/deprecated and directs migration away from it](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.AzureAD.UI).
- NuGet: [`Microsoft.Identity.Web` licensing and net10.0 compatibility](https://www.nuget.org/packages/Microsoft.Identity.Web).
- NuGet: [`Swashbuckle.AspNetCore` licensing and net10.0 dependency compatibility](https://www.nuget.org/packages/Swashbuckle.AspNetCore).
- Microsoft ASP.NET Core documentation: [built-in OpenAPI is the .NET 9+ default; Swashbuckle remains a manually added option](https://learn.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger?view=aspnetcore-7.0).

## Non-goals

- Do not convert protected controllers to Paradigm generic controller bases.
- Do not enable `ApiAuthorizationAttribute`/`ClientSecrets` as a second authorization system.
- Do not add a browser cookie/OIDC UI, `Microsoft.Identity.Web.UI`, or AzureAD UI packages to this bearer API.
- Do not alter routes, API versioning, database schema, provider transaction ownership, sales workflow semantics, or generic CRUD exposure merely to adopt generated views.
- Do not publish Swagger UI or OpenAPI documents anonymously in production without a separately approved operational/security decision.
- Do not commit identity secrets, signing keys, token defaults, tenant IDs, client IDs, or permissive CORS defaults.
