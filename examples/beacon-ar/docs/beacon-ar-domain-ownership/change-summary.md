# Beacon AR domain ownership change summary

## Outcome

Task 4 now gives intrinsic rules to the objects that own them, gives shared database catalogs one cross-application enum owner, and makes generated Sales views the canonical search/read shape. Generated EF entities, views, contexts, and official T4 templates were not hand-edited.

## Changes

- Replaced the broad MasterData and Sales validation utilities with co-located partial behavior on Product, Customer, CustomerAddress, Carrier, ApplicationUser, AuditLog, IdempotencyRequest, Quote, QuoteLine, SalesOrder, SalesOrderLine, and both status-history entities. Existing-object operations normalize and validate a complete proposed state before mutation. Paging, sorting, filtering, and other transport rules remain on their request types; repository-, identity-, and reference-dependent orchestration remains in Providers.
- Added the AuditLog factory and intrinsic JSON/length/actor/time validation. MasterData and Sales coordinators now create audit entries through that factory. Idempotency creation/completion and status-history construction likewise validate their own state.
- Replaced the bypassable public Sales prepare/apply pairs with one atomic `Replace` operation per aggregate. Draft status, reference facts, normalized header values, lines, and audit inputs are all validated before any header assignment; invalid notes/tracking, invalid lines, and non-draft calls leave complete aggregate snapshots unchanged. Providers no longer duplicate intrinsic status-enum validation.
- Made IdempotencyRequest validation total for null runtime values and explicit for all three active catalog states. Added atomic `Complete` and `Fail` transitions, terminal timestamp/actor rules, completed 2xx/resource semantics, failed 4xx/5xx/no-resource semantics, and terminal transition rejection.
- Moved AddressType, IdempotencyState, QuoteStatus, and SalesOrderStatus enums to capability folders in BeaconAr.Interfaces. Members have explicit numeric values and stable `EnumMember` codes, and a test exhaustively compares both enum and SQL seed sets in both directions.
- Split the Roslyn analyzer into the BeaconAr.InterfaceGenerator tool project so BeaconAr.Interfaces can be referenced as a normal runtime contract assembly without loading Microsoft.CodeAnalysis into application discovery. The generator source now lives in that tool project; Domain references the contract assembly normally and the generator only as an analyzer.
- Changed Quote and SalesOrder search procedures, wrappers, repositories, Providers, controllers, JSON metadata, OpenAPI, and the generated TypeScript client to use QuoteView and SalesOrderView. Removed the parallel search rows, generated row mappers, and summary DTOs. The stored-procedure mapper generator now supports the DateOnly fields present in the canonical views.
- Detail responses deliberately retain QuoteDto, SalesOrderDto, and SalesLineDto as the named compatibility contract described in decisions.md. Repositories first materialize canonical header/line views and compose those DTOs only at that boundary. The removal trigger is a versioned client contract that accepts canonical header/line composition.
- Added direct Domain tests and deterministic Architecture checks for co-located partial ownership, broad-validator absence, enum/seed parity, reviewed DTO allow-list, generated-view Provider search contracts, and persistence types being excluded from public write parameters. Final-review regressions include exhaustive Quote/SalesOrder snapshots across failed replacements, Customer/Address/Carrier replacement atomicity, null-safe AuditLog/Idempotency validation, every idempotency state shape, invalid timestamps/status bounds, and terminal transition atomicity.
- Updated the canonical coding, database, domain-model, Provider, and review guidance with the evidenced ownership, enum parity, and compatibility-contract rules. Judgment-heavy classification remains review guidance; Beacon-specific facts are enforced in Beacon tests.

## Verification

| Check | Result |
| --- | --- |
| Locked restore | Passed for BeaconAr.slnx, including the new InterfaceGenerator tool project. |
| Release solution build | Passed with 0 warnings and 0 errors. |
| SQL Server database project build | Passed with 0 warnings and 0 errors; DACPAC produced. |
| Strict database source validation | Passed with no diagnostics. |
| Offline solution tests | Passed after final review fixes: 156 succeeded, 42 live SQL/HTTP cases skipped because the disposable connection is absent, 0 failed. |
| Stored-procedure mapper generation | Two consecutive runs produced SHA-256 inventory `920E8C1C0F4EE890FC79D0F46D4EFF271419CE12DE012F8DB58140EFF657E13C`. |
| OpenAPI generation | Two consecutive runs produced `f0398f38eda2976bc7937fcd673d70e1f40c17bef92a7b02c3fe96223ee1b3eb`. |
| TypeScript client generation/compile | Two consecutive runs produced `da1fdfc3f9b83f19d55a9607a578ad4cf3f93737ff6ad16102b64d62777840f0`; strict `tsc --noEmit` passed. |
| Paradigm project diagnostics | `doctor` and `checks run` passed independently for Domain, Data, Providers, and WebApi. Package consistency passed. Package audit reported only existing/newer-version advisories and no vulnerability diagnostic. |
| Skill/plugin validation | Passed: 11 canonical skills and plugin metadata 1.1.0. |
| Worktree hygiene | `git diff --check` passed; no generated EF entity/view/context or T4 file is modified. |

## Evidenced gaps

- `ConnectionStrings__DatabaseConnection` is not configured in this environment. A freshly published disposable SQL Server, the live database/Web API integration cases, and the two-pass EF Core Power Tools persistence regeneration therefore remain unverified here. The checked regeneration script still protects the exact generated manifest and all co-located handwritten partials, including AuditLog.Behavior.cs.
- Whole-solution `paradigm doctor`/`validate` treats the SQL project's DACPAC as a managed assembly and exits with PE1002. Per-project doctor/checks pass. Domain-only `validate` additionally reports PE3002 for the mixed-key Operations context because AuditLog/IdempotencyRequest use long keys while the shared context is necessarily `DbContextBase<int>` for its other entities; changing the repository base to long does not compile. These are CLI model limitations, not suppressed successes.
- Package audit exits with warning status for available version upgrades (including the pre-existing Roslyn analyzer versions moved unchanged into InterfaceGenerator); it reports no security vulnerability. Package upgrades were outside Task 4.
