# Beacon AR Provider and API conventions - decisions

## The current generic edit base is not the Beacon write contract

The framework generic edit base accepts and maps a generated view and exposes single/bulk Add, Update, Save, and Delete. Beacon MasterData writes instead accept allow-listed commands and require an expected row version, actor/time audit stamping, duplicate/reference lookups, Operations audit facts, cancellation, and an explicit transaction. The current base has no command or expected-version abstraction, and its bulk surface cannot silently inherit the single-write protocol.

The four MasterData Providers therefore stop inheriting `EditProviderBase` and their interfaces stop inheriting `IEditProvider`. The request-based Create/Update/Delete methods remain as explicit use cases. This is not a preference for custom CRUD: it is the smallest safe contract supported by the installed framework. The existing pattern of inheriting the base while overriding all eight edit methods to throw is removed because it advertises capabilities the Provider intentionally does not implement.

No Beacon-local generic base is introduced. A future migration requires an official command-oriented framework base that supports allow-listed mapping, optimistic concurrency, cancellation, transaction/audit participation, safe bulk semantics, and hook ordering. Until then, generated views remain response models only.

## Framework controller bases remain unsuitable for protected routes

The Paradigm API controller bases carry `[AllowAnonymous]`. Standard `[Authorize]` metadata on a derived controller does not override that metadata. Beacon's eight business controllers remain direct ASP.NET Core `ControllerBase` types with their existing policies. The RDX solution demonstrates generic controllers and Provider bases, but its authorization and mutable-view assumptions are not copied where they conflict with the current framework security guidance and Beacon's stronger write boundary.

## Existing search requests are the query DTOs

Product, Customer, Carrier, Address, Quote, and SalesOrder already have purpose-built search request types with their query-shape rules. Each controller binds its request with one `[FromQuery]` parameter and passes it intact to the Provider. A second WebApi model would duplicate fields and mapping for no contract benefit.

ASP.NET Core attributes stay out of Domain. `[FromQuery]` is placed on the action parameter; existing camel-case property names preserve the public query keys. The two positional Sales search records remain records. Real hosted ASP.NET Core tests prove that MVC constructs them with the intended omitted-value defaults, binds every public camel-case query key, rejects malformed enum values through the established validation Problem Details contract, and passes the exact request instance to the Provider. There is therefore no contract or implementation reason to replace them with mutable classes. Any later implementation-shape change must preserve those hosted tests, OpenAPI, and the generated client.

## Validation remains split by ownership

Entity invariants and transitions stay in co-located Domain partials. Search paging/filter/sort rules stay on the search request. Verified identity input validates itself. Providers invoke those contracts and own only facts that require repositories, identity/policy, current time, concurrency state, persistence classification, or cross-aggregate coordination.

Positive route identifiers, strong `If-Match` syntax, content type/size, and idempotency-key syntax are HTTP transport rules. `ApiContract.EnsurePositiveId`, `ETagCodec`, middleware, and `CreationIdempotencyService` may enforce them at the WebApi boundary. They must not be mislabeled as entity validation or moved into Providers merely to remove a controller line. Any later centralization must preserve the exact existing Problem Details status/code contract.

Calling `request.Validate()` in a Provider is not Provider-owned validation logic: it executes the query object's own rule before a repository call and protects non-HTTP callers. Providers must not reimplement those rules.

## Custom workflow and infrastructure services stay in Providers

`MasterDataMutationCoordinator`, `SalesWorkflowCoordinator`, and `CreationIdempotencyProvider` coordinate application/infrastructure collaborators and therefore remain in the Providers project. They do not move to Domain, do not become service locators, and do not absorb entity rules. `DashboardProvider` remains a narrow query boundary so the controller does not depend on a repository.

Quote, SalesOrder, and conversion operations remain custom because they mutate aggregates, status histories, audit rows, or more than one aggregate and require explicit transaction/conflict behavior. Address default reassignment is likewise multi-entity. These are not candidates for a cosmetic Save/Create/Update replacement.

## Compatibility is a release gate

The controller signature refactor is internal to binding. Public paths, verbs, operation IDs, authorization, query names/defaults, response types/statuses, ETags, `If-Match`, idempotency, and Problem Details remain stable. The checked OpenAPI document and generated TypeScript client are authoritative compatibility evidence. Both are regenerated twice; the strict consumer must still compile with its current calls.

No package is added and no database object changes in this task. SQL Server remains the example database.

## Disposable SQL validation must reproduce the Aspire database resource

The bootstrap opens the configured target catalog before publishing. Aspire creates that database resource before starting the bootstrap, while a raw SQL Server container creates only system databases. A manual disposable gate must therefore create an empty, explicitly named target database first, then run the bootstrap with `Database__PublishOnStart=true`, `Database__Mode=Managed`, and SQLCMD 18-compatible encrypted connectivity (`Encrypt=True;TrustServerCertificate=True`).

Task-owned Docker resources use a unique label and explicit names, a user-defined network for container DNS, no volume, and a bounded lifetime. Cleanup targets only those exact resources. Pre-existing containers and volumes are evidence, not cleanup candidates.

## Guidance is judgment-based plus a local deterministic fixture

Whether a generic base can express a particular write protocol requires API and threat-model judgment, so the durable rule belongs in Provider/review skills. Beacon can deterministically assert its resolved contract: no MasterData `IEditProvider`, no persistence view mutation parameter, and exactly one complex query object on each six search endpoints. A universal CLI rule is deferred because safe exceptions cannot be identified from metadata alone.
