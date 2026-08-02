# Beacon AR final validation decisions

Date: 2026-08-02  
Release owner: Beacon AR maintainers

## Delegated-user-only authentication

- Decision: release one accepts Entra delegated users only. An explicit `idtyp=user` is accepted; the legacy-compatible fallback requires a non-empty `scp`. Roles-only, app-only, and no-scope actors are rejected before application-user provisioning. `oid == sub` is never an actor classifier.
- Requirement/source: user security requirements, `paradigm-build-web-api`, and the API security task.
- Evidence: security-matrix and acceptance tests; the live Web API run passed 56/56 on 2026-08-02.
- Chosen outcome: Microsoft Identity Web bearer validation plus explicit actor classification and `business.read`/`business.write` policies.
- Rejected alternative: client-credential actors and identity inference from claim equality.
- Compatibility/security consequence: daemon access is intentionally unsupported; delegated user tokens fail closed when actor evidence is missing.
- Revisit trigger: a named machine-to-machine use case with a separate identity, provisioning, permission, and audit design.

## Direct protected controllers

- Decision: business controllers inherit directly from `ControllerBase` and declare authorization/permission metadata explicitly.
- Requirement/source: user request to prefer framework controllers where safe, current framework metadata behavior, and `paradigm-build-web-api`.
- Evidence: architecture and endpoint metadata tests passed; no business endpoint inherits `IAllowAnonymous`.
- Chosen outcome: retain direct controllers because current generic API controller bases carry anonymous metadata that conflicts with this API's fail-closed boundary.
- Rejected alternative: inheriting generic controllers and treating endpoint exposure controls as authorization.
- Compatibility/security consequence: CRUD routing remains explicit; no inherited anonymous or generic mutation route is exposed.
- Revisit trigger: framework controller bases provide an authorization-neutral/protected variant with equivalent ETag, request, and response contracts.

## Generated views and write requests

- Decision: EF Core Power Tools generated `ProductView`, `CustomerView`, `CustomerAddressView`, and `CarrierView` are the sole master-data read/API shapes; narrow request records remain the write boundary.
- Requirement/source: user database/entity/data/provider requirements and `paradigm-build-provider`.
- Evidence: duplicate DTOs and `ToDto`/`*ForApiAsync` methods were removed; architecture tests pass 25/25; live database/API suites pass 22/22 and 56/56; OpenAPI and TypeScript use the views.
- Chosen outcome: one compiled read surface shared from database view through repository, provider, controller, OpenAPI, and client.
- Rejected alternative: parallel compatibility DTOs without an external compatibility requirement.
- Compatibility/security consequence: internal provider compatibility methods are removed; reviewed HTTP JSON names remain stable through the app JSON resolver.
- Revisit trigger: an explicitly versioned external contract that cannot use the generated shape.

## Provider and transaction boundaries

- Decision: master data uses official read/edit provider and repository bases. Quote, SalesOrder, and conversion remain custom providers because they coordinate aggregate lines, histories, snapshots, idempotency, concurrency, and explicit transactions.
- Requirement/source: user provider requirement and `paradigm-build-provider` transaction guidance.
- Evidence: provider/architecture tests and live race, rollback, lifecycle, and conversion cases pass. SQL exception classification is behind Domain contracts implemented in Data.
- Chosen outcome: official bases for conventional CRUD, constructor-injected custom orchestration for workflows.
- Rejected alternative: forcing aggregate workflows through generic CRUD or allowing Providers to inspect SQL Server exceptions.
- Compatibility/security consequence: generic master-data operations retain framework behavior; workflow atomicity and conflict translation remain explicit.
- Revisit trigger: framework aggregate/transaction primitives cover the same invariants without weakening behavior.

## SQL Server database-first generation

- Decision: SQL Server, Microsoft.Build.Sql DACPAC publication, and EF Core Power Tools 10.1.1386 with committed config/T4 templates are authoritative.
- Requirement/source: user preference and database/entity requirements; `paradigm-build-database`.
- Evidence: locked restore, SQL project build, strict database validation, disposable first/restart bootstrap, 22/22 live database tests, two EFPT runs with identical hash `1e2e044adf938857f790d9efb04800a250e307907d534d6c4bcecfdf453279a7`, and recovery/redaction fixtures.
- Chosen outcome: retain the regenerated context's precise column-level collation metadata. Run one reconciled one stale generated file; run two was byte-identical.
- Rejected alternative: manual entity/context edits, host SQLCMD/SqlPackage, or generation against the ignored external `.env` connection.
- Compatibility/security consequence: generated ownership is reproducible and credentials never enter source/output; business-key comparison metadata matches the published schema.
- Revisit trigger: schema, EFPT version/template, database provider, or collation policy changes.

## Visual Studio SQL-project identity

- Decision: retain Visual Studio 18's restored `TargetDatabaseSet` and SQL `ProjectGuid` metadata, and align the solution project entry, all 12 configuration mappings, and its solution-folder mapping to GUID `{A09E027B-4428-49A6-9A92-C43CF35D9206}`.
- Requirement/source: user requirement that the SQL project load completely in Visual Studio and direct observation during final validation.
- Evidence: with the project loaded, Visual Studio restored both properties within 500 ms after two attempted removals. After alignment, there are zero old-GUID hits, one project identity, no duplicate solution project GUID, the final newline remains stable, and SQL build/strict validation pass.
- Chosen outcome: accept the IDE-owned stable identity and document it as design-time metadata; keep explicit folders plus the non-duplicated `Build`/`None` item strategy.
- Rejected alternative: repeatedly removing metadata the active Visual Studio project system deterministically restores, or leaving the solution and project identities mismatched.
- Compatibility/security consequence: Visual Studio and command-line builds address the same database project; no runtime or schema behavior changes.
- Revisit trigger: a Microsoft.Build.Sql/Visual Studio upgrade stops requiring/restoring the metadata, or the solution is regenerated with a deliberately new project identity.

## OpenAPI and authentication libraries

- Decision: use Swashbuckle.AspNetCore 10.2.3 for OpenAPI/Swagger and Microsoft.Identity.Web 4.14.2 for bearer authentication. Restored NuGet metadata declares MIT licenses and their official GitHub repositories.
- Requirement/source: user API requirements and the approved package graph.
- Evidence: locked restore, package consistency/audit, OpenAPI tests, and two canonical generations at SHA-256 `dd12b0365570e8bd1662ae0040a6384f969792717dc7fb95a7456ea2a1650150`. The artifact regression requires zero CR bytes, exactly one final LF, valid JSON, and the reviewed hash. Restored package metadata identifies the official repositories.
- Chosen outcome: a single Swashbuckle pipeline and current Microsoft identity middleware.
- Rejected alternative: deprecated `Microsoft.AspNetCore.Authentication.AzureAD.UI`, custom token middleware, or a second OpenAPI pipeline.
- Compatibility/security consequence: standard bearer/OpenAPI interoperability; Swagger document/UI exists only in Development.
- Revisit trigger: package deprecation/security finding or a supported replacement with equivalent contract generation.

## Root, CORS, health, and secrets

- Decision: `/` anonymously returns project name/version; `/alive` and `/health` remain bounded anonymous probes; business CORS is an explicit allow-list; secrets are process/Aspire parameters and never defaults or logs.
- Requirement/source: user API/Aspire requirements and `paradigm-setup-aspire`.
- Evidence: host-hardening, root, health, CORS, documentation tests, `start.sh doctor`, Aspire restore, and secret-safe disposable bootstrap.
- Chosen outcome: narrow public operational endpoints and explicit protected business routes.
- Rejected alternative: root `401`, allow-any CORS, embedded credentials, or database publication from API startup.
- Compatibility/security consequence: the service can be identified/health-checked without a token while business data stays protected.
- Revisit trigger: deployment platform requires authenticated probes or a different origin/secret provider.

## Verification limitations and disposition

- Real Entra metadata discovery and token issuance were not contacted. The real middleware pipeline was exercised with isolated test signing keys. Owner: security/deployment team. Revisit before production tenant acceptance.
- A complete `aspire run --isolated` was not pointed at the ignored external `.env` target because its disposability was not authorized. The bootstrap image, first/restart publication, SQL tests, doctor, restore, AppHost build, and resource-graph tests were verified with task-owned resources. Owner: deployment team. Revisit when a dedicated Aspire smoke environment is supplied.
- No Azure deployment target was selected, so Bicep publication was not generated or deployed. Owner: deployment team. Revisit after Container Apps/App Service and subscription/environment choices are approved.
- Solution-wide `paradigm checks run` exits 3 because the semantic checker cannot resolve Aspire's generated `Projects` namespace in AppHost. Focused Domain/Data/Providers/WebApi checks, full compilation, and AppHost tests pass. Owner: Paradigm CLI maintainers. Revisit when the checker supports Aspire generated project references.
- `npm ci` warns that local Node 24.13.0 is below Angular 22.1.0's supported 24.x patch floor (24.15.0); install, audit, generation, and strict TypeScript compilation pass. Owner: developer environment/CI. Revisit on the next Node refresh.
- `start.sh doctor` reports one local older HTTPS development-certificate warning and zero failures. No certificate mutation was authorized. Owner: developer workstation. Revisit if local HTTPS trust fails or certificates are intentionally rotated.
