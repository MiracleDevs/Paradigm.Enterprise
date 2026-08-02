# Beacon AR Web API review feedback

## Result

Changes requested. No P1 defects were found; two P2 gaps remain before this task is ready to merge.

## P2 - Application-role callers do not have a defined actor model

### Evidence

- The implementation plan explicitly requires a decision between delegated-only and delegated-plus-app-only access (`implementation-plan.md`, lines 58-64 and 112), but `decisions.md` records only why scope-less/role-less tokens are allowed to reach the local authorization policies. It does not decide whether service principals are supported.
- `PermissionEvaluator` accepts `roles` claims as read/write grants (`src/BeaconAr.WebApi/Security/PermissionEvaluator.cs`, lines 26-38).
- Every authenticated `/api/v1` request is then converted to an `AuthenticatedIdentity` containing `iss`, `sub`, and a human display name and passed to local-user resolution (`src/BeaconAr.WebApi/Access/CurrentUserMiddleware.cs`, lines 35-46).
- `CurrentUserProvider` rejects a missing display name and otherwise creates or synchronizes an `ApplicationUser` (`src/BeaconAr.Providers/Access/CurrentUserProvider.cs`, lines 35-48 and 55-63).
- The role authorization matrix uses tokens that always contain the same human-shaped `sub`, `name`, and `email` claims as delegated test tokens (`BeaconArTestTokenFactory.cs`, lines 19-49). It therefore does not exercise a client-credentials token or an `idtyp=app` identity.
- The earlier API-contract decision says release-one requires a human-user display claim and app-only/service tokens are outside the actor model (`docs/beacon-ar-api-contract/implementation-plan.md`, line 34).

### Consequence

A real application token can satisfy a documented app role and then either fail later as an incomplete user or, depending on optional claims, be represented and persisted as a human `ApplicationUser`. That makes authorization, actor identity, and audit behavior inconsistent. It also means the passing role tests do not prove the advertised application-role behavior.

### Required fix

Make and document one explicit choice, then test that choice with application-token-shaped claims:

1. For the smallest release-one fix, reject app-only identities deliberately before local-user resolution, using an unambiguous token/actor classification such as `idtyp=app`; retain role handling only if roles are valid for the chosen delegated-token scenario. Cover `/api/v1/me` and representative read/write endpoints.
2. If app-only access is intentional, implement a separate service-principal identity and audit model instead of creating/synchronizing a human `ApplicationUser`, and cover read/write roles end to end.

The implementation plan, `decisions.md`, configuration/runbook guidance, and generated OpenAPI description must describe the same supported caller model.

## P2 - Required production fail-closed configuration paths lack regression coverage

### Evidence

- `Program.cs`, lines 44-54, correctly rejects missing `AzureAd:Instance`, `AzureAd:TenantId`, `AzureAd:ClientId`, or all CORS origins outside local environments, and rejects `Authentication:SigningKey` outside Testing.
- The host-hardening suite tests only the production signing-key rejection (`BeaconArApiHostHardeningTests.cs`, lines 39-47 and 259-274). It always supplies otherwise valid Azure AD and CORS values.
- The task's acceptance criteria explicitly require production startup to reject missing Entra/CORS configuration (`implementation-plan.md`, line 162).

### Consequence

The fail-closed behavior is implemented but can regress without a failing test. This is security-sensitive startup behavior and a stated acceptance criterion, not merely an internal implementation detail.

### Required fix

Add production-environment host tests that independently clear or provide whitespace for each required Azure AD value and omit all allowed CORS origins, asserting startup fails with a safe message. Retain a valid production configuration case so the tests also prove the production graph can start when configuration is complete.

## Verified boundaries

- All eight business controllers derive directly from `ControllerBase`; no Paradigm CRUD base contributes inherited routes or anonymous metadata. The 35 business operations are protected, reads use `business.read`, mutations use `business.write`, and `/api/v1/me` requires authentication.
- `GET /`, `HEAD /`, `/alive`, and `/health` are the deliberate anonymous endpoints. The root response contains only application name and version.
- Swagger JSON/UI are mapped only in Development. The checked artifact remains available without exposing runtime production documentation.
- The response contract uses generated database views, including joined display fields. Writes use purpose-built request contracts, so view/entity properties are not directly overposted.
- JSON contract customization keeps `RowVersion` exposed as `version` and applies the documented address aliases. The concurrency value remains the same base64 byte sequence used by ETag encoding.
- Provider compatibility adapters delegate to the new provider-owned API mapping paths; repositories do not map entities to API views.
- OpenAPI contract tests cover operation count, bearer security, request/concurrency/idempotency headers, Problem Details, examples, schemas, and nullability. Repeated artifact generation was byte-for-byte stable.
- `Microsoft.Identity.Web` 4.14.2 and `Swashbuckle.AspNetCore` 10.2.3 are centrally pinned and locked. Both report MIT licensing; the Web API package graph reports no known deprecated or vulnerable packages.
- The reference API was inspected for composition patterns. Its legacy Azure AD UI authentication, permissive CORS, and custom anonymous-controller behavior were not copied.

## Verification performed

- `dotnet restore examples/beacon-ar/src/BeaconAr.sln --locked-mode` - passed.
- `dotnet build examples/beacon-ar/src/BeaconAr.sln --configuration Release --no-restore` - passed with 0 warnings and 0 errors.
- Focused non-integration Web API tests - 31/31 passed.
- Full solution tests - 116 passed, 41 skipped, 0 failed (157 total). The skips are 22 database integration tests and 19 HTTP acceptance tests because `ConnectionStrings__DatabaseConnection` was not supplied.
- OpenAPI artifact generation twice - passed; initial and both generated SHA-256 values were `51FA9B8A6AF86A1C74447B4C9D740CDB0CE2AAAF8CCB29D6B8F9BF6E42765E83`.
- `dotnet tool run paradigm doctor` - passed.
- `dotnet tool run paradigm packages check` - passed.
- `dotnet tool run paradigm packages audit` - completed successfully; it reported only existing available package updates, with no deprecation or vulnerability finding for the newly added packages.
- `dotnet tool run paradigm validate` for the solution and strict database validation - passed.
- Paradigm semantic checks for Web API, Providers, and Domain - passed.
- `git diff --check` - passed.

## Residual environment gaps

- Database-backed integration and HTTP acceptance behavior was not exercised because no SQL Server connection string was available in the review environment.
- Authentication was validated with isolated signed test tokens, not against a live Microsoft Entra tenant/app registration. The app registration's scopes, roles, assignments, audience, and token shape still require deployment-environment verification after the actor-model decision is made.

## Second review after fixes

### Result

Changes requested. The production startup-configuration finding is resolved. The application-token finding is partially addressed but remains P2 because the fallback classification for tokens without `idtyp` is not based on the documented Microsoft Entra permission-token shape.

### Prior finding dispositions

1. **Application-role callers do not have a defined actor model - partially resolved, P2 remains.** The documents now make the release-one delegated-user-only decision. `DelegatedUserRequirement` is attached to the default, fallback, `business.read`, and `business.write` policies, and `UseAuthorization` runs before `CurrentUserMiddleware`. Explicit `idtyp=app`, unknown identity types, missing identifiers, and equal test `oid`/`sub` values are denied before user resolution. The remaining legacy-token defect is detailed below.
2. **Production fail-closed configuration paths lack regression coverage - resolved.** `ProductionRejectsEachMissingRequiredIdentityOrCorsSetting` forces actual `Program` startup through `factory.Server` for whitespace `AzureAd:Instance`, `AzureAd:TenantId`, and `AzureAd:ClientId`, plus omitted CORS origins. The complete production configuration also constructs the server without issuing an authenticated request or triggering OIDC metadata retrieval. The separate production signing-key rejection remains covered. The startup check additionally rejects whitespace origin entries.

### P2 - A legacy app-only token with distinct `oid` and `sub` can still provision a user

#### Evidence

- `DelegatedUserAuthorizationHandler.cs`, lines 13-25, rejects a non-`user` `idtyp`, but when `idtyp` is absent it succeeds solely when `oid` and `sub` are non-empty and unequal.
- Microsoft documents `sub` as a pairwise identifier and `oid` as the stable object identifier for either a user or service principal. Inequality between them is therefore not proof of a human user: [Microsoft identity platform access-token claims reference](https://learn.microsoft.com/en-us/entra/identity-platform/access-token-claims-reference).
- Microsoft's current ASP.NET Core API tutorial gives the supported fallback when `idtyp` is not enabled: a token with `roles` and no `scp` is app-only; a token with `scp` is delegated; a token with both is also user-delegated: [Build and secure an ASP.NET Core web API](https://learn.microsoft.com/en-us/entra/identity-platform/tutorial-web-api-dotnet-core-build-app).
- `BeaconArTestTokenFactory.CreateApplication`, lines 66-91, assigns the same value to `oid` and `sub`. The supposed legacy case therefore tests only the equality heuristic. It does not test an absent-`idtyp`, roles-only application token with distinct identifiers.
- The delegated role matrix creates role-only tokens with a forced `idtyp=user`. It proves the handler trusts explicit user classification, but it does not model Microsoft's documented legacy user shape, where `scp` distinguishes a delegated token and `roles` can additionally contain roles assigned to that user.
- `implementation-plan.md` line 58, `decisions.md` line 21, the runbook line 7, `change-summary.md` line 11, and the generated OpenAPI bearer description all repeat the unsupported equal-`oid`/`sub` classification assumption.

#### Consequence

A signed client-credentials token with an assigned `business.read` or `business.write` role, no `idtyp`, no `scp`, and distinct pairwise/stable identifiers satisfies both the actor requirement and the permission policy. `CurrentUserMiddleware` can then resolve or provision it as a human `ApplicationUser`, contrary to the documented actor model and audit boundary. The current regression test cannot detect that path.

#### Smallest safe correction

- Treat an explicit `idtyp` as authoritative and accept only `user`.
- When `idtyp` is absent, use the documented permission-type fallback: require `scp` for a delegated user; reject roles-only/no-`scp` and neither-claim token shapes as application/unsupported actors before current-user resolution. Continue evaluating exact user-carried `roles` after the token has been classified as delegated.
- Add a roles-only, absent-`idtyp` application test with deliberately distinct `oid` and `sub`; assert `403` for `/api/v1/me` and representative read/write operations and assert zero current-user resolutions. Model a delegated user carrying a role with `scp` plus `roles`, while retaining the explicit `idtyp=user` coverage if that optional-claim configuration is supported.
- Replace the equality rule in the implementation plan, decision record, runbook, summary, OpenAPI description, and checked artifact with the `idtyp` plus `scp`/`roles` fallback. `oid` and `sub` can remain required identity inputs, but their inequality must not be presented as the actor-type security boundary.

### Second-review verification

- Release solution build - passed with 0 warnings and 0 errors.
- Focused security-matrix and host-hardening tests - 18/18 passed.
- Focused OpenAPI artifact and documentation tests - 3/3 passed.
- `paradigm packages check` - passed.
- Web API deprecated-package and vulnerable-package checks - no findings.
- Checked OpenAPI artifact SHA-256 - `C61256ABEC7D7F577C01E8CA0063DE826597D27DB73F62DB7FA667CA9DCA6005`.
- `git diff --check` - passed; only existing line-ending conversion warnings were emitted.

The same environment limitations from the first review remain: no live Entra tenant was used, and the database-backed integration/HTTP acceptance suites were not rerun without a SQL Server connection string. The passing structural and semantic validators do not exercise real Entra token issuance or prove the unsupported `oid`/`sub` classifier.

## Third and final review after legacy-classification fix

### Result

Approved. Both prior P2 findings are resolved, and no new findings were identified. This section supersedes the earlier requested-change results while retaining them as review history.

### Final dispositions

1. **Delegated-user/application-token actor boundary - resolved.** `DelegatedUserAuthorizationHandler` requires non-empty `oid` and `sub`. An explicit `idtyp` is accepted only when its exact value is `user`; `app` and unknown values fail. When `idtyp` is absent, a non-empty `scp` is required, so roles-only and unclassified legacy token shapes fail regardless of whether their identifiers are equal or distinct. This matches the Microsoft claim semantics cited in the second review.
2. **Production fail-closed identity/CORS configuration coverage - resolved and unchanged.** The production factories still execute `Program` startup for each missing identity/CORS setting, the complete graph starts without an authentication request or metadata fetch, and a configured test signing key remains rejected in Production.

### Security and contract evidence

- The delegated-user requirement is present in the default policy, fallback policy, `business.read`, and `business.write`. `UseAuthorization` remains before `CurrentUserMiddleware`, so a failed actor requirement prevents user lookup/provisioning.
- Application-token tests use distinct `oid` and `sub` values. They cover explicit `idtyp=app` plus absent-`idtyp`, roles-only read and write tokens across `/api/v1/me` and representative business operations, and assert zero current-user resolutions.
- Positive and negative delegated coverage includes explicit `idtyp=user` roles, absent-`idtyp` delegated read/write scopes, absent-`idtyp` `scp` plus user-carried `roles`, exact/case-sensitive scope and role evaluation, write-implies-read, unknown identity type, missing `oid`, missing `sub`, and an absent-`idtyp` token with no `scp`.
- The active implementation plan, decision record, change summary, API runbook, README, Swashbuckle filter, and checked OpenAPI artifact consistently describe `oid`/`sub` as required identity inputs rather than an equality-based actor discriminator. They describe explicit `idtyp=user` or the `scp` fallback and roles-only rejection. The earlier equality text remains only in the preserved first/second review history above.
- No controller authorization, anonymous endpoint, middleware-order, view serialization, ETag/idempotency, Swagger exposure, package, or generated-artifact regression was found.

### Final verification

- OpenAPI generation from the isolated helper was run twice. The initial, first, and second SHA-256 values were all `3C828B18A0CA495AC85A1DFE8126A18ED5C212428855D385741BCD83FA423F0C`; no Web API process was left running.
- Release solution build - passed with 0 warnings and 0 errors.
- All non-integration `BeaconAr.WebApi.Tests` - 37/37 passed with 0 skipped and 0 failed. This includes security matrix, production host hardening, discovery, documentation, and artifact coverage.
- `paradigm packages check` - passed.
- `paradigm packages audit` - exited successfully and reported only the previously recorded available-update warnings. It reported no mixed Paradigm versions, vulnerability, deprecation, or expired-suppression error.
- Direct Web API deprecated and vulnerable package checks - no findings.
- `git diff --check` - passed; only working-tree line-ending conversion notices were emitted.

### Remaining environment limitations

- No live Microsoft Entra tenant/app registration was available, so issuer metadata, actual tenant token issuance, app-role assignments, and optional-claim configuration remain deployment verification items.
- Database-backed integration and authenticated HTTP acceptance suites were not rerun because no SQL Server connection string was provided. The final changes are isolated to authorization classification, its host tests, documentation, and the deterministic OpenAPI description; the earlier full review recorded 116 passing and 41 environment-skipped solution tests.
