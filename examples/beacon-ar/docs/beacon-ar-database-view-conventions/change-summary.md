# Beacon AR database view conventions change summary

## Outcome

Task 2 implements the database-view naming, Operations projection, generation provenance, deterministic policy, and validation requirements while retaining SQL Server. Every database view now ends in `View`; the generated persistence model follows those names; Operations projections remain internal and are not exposed through the API.

## High-level changes

- Renamed `QuotePricing` and `SalesOrderPricing` to `QuotePricingView` and `SalesOrderPricingView`, including their files, parent-view dependencies, EF types, `DbSet` properties, `ToView` mappings, and repository usage. No nonconforming compatibility aliases remain.
- Added schema-bound `AuditLogView`, `IdempotencyRequestView`, and `IdempotencyStateView` projections with one-row-per-base-row semantics and useful user/state display values.
- Extended `FoundationSmoke.sql` to require all 14 views, verify schema binding and uniqueness, and compare Operations base/view row counts.
- Added CLI policy `PEDB112` for SQL Server and PostgreSQL view names that do not end in `View`. PostgreSQL coverage includes the complete documented ordinary-view modifier order—`OR REPLACE`, `TEMP`/`TEMPORARY`, and `RECURSIVE` in all relevant combinations—plus materialized forms; its grammar is isolated from SQL Server. Tests cover compliant input, legacy warning mode, strict error mode, and deterministic repeated results.
- Corrected CLI identifier analysis so an inherited non-generic `IEntity` marker does not create a false legacy `int` candidate when a closed `IEntity<TId>` contract exists.
- Rebased Beacon's T4 ownership on the official `Paradigm.Web.ApiTemplate` templates at revision `522906b9151d566a5dddd2609b66da36726368a7`, documented source hashes and reusable compatibility adaptations, and removed Beacon-specific type/relationship whitelists.
- Updated generation for metadata-derived identifiers, current generic entity/mapper contracts, view mappings, generated-code ownership, and safe atomic regeneration. The regeneration script now backs up the exact generated set, clears only verified owned paths, restores on failure, and prevents obsolete renamed output from surviving.
- Regenerated the context and entity/view model from a disposable published SQL Server database. The five new/renamed view types are generated artifacts, not manual copies.
- Kept aggregate ownership/validation in domain partials and corrected the reverse-engineered `Customer`/`CustomerAddress` relationship through a context partial hook instead of application-specific T4 conditions.
- Added architecture, OpenAPI, generated-client, and live SQL/EF assertions for Operations mapping, identifier types, row parity, display expansion, binary hashes, nullable audit fields, and non-exposure at the HTTP boundary.
- Updated shared database, review, CLI, tutorial, changelog, and Beacon guidance so future work starts with the universal view suffix, official-template provenance, reusable adaptations only, and deterministic regeneration expectations.

## Final review remediation

- `DbContext.t4` no longer contains `DbContextBase<int>`. It identifies complete audited table metadata, normalizes both actor columns, requires one unambiguous actor identifier type across the model, and emits `DbContextBase<TActorId>` from that metadata. Compiled Beacon metadata still verifies the expected `DbContextBase<int>` result.
- `EntityType.t4` independently inspects `CreatedByUserId` and `ModifiedByUserId`, normalizes nullable types, and stops generation with a type-specific error when they disagree. Deterministic metadata fixtures cover matching `int`, matching `long`, and mismatched actor types; the existing long-entity/int-actor Operations case proves the current incompatible audit interface is omitted.
- Removed all unreachable aggregate-navigation generation variables and branches. The template emits only persistence collections; Domain partials own trackers/behavior, and the context partial owns the relationship correction. Active EFPT records and the architecture test name now state that ownership accurately.
- Restored CLI test member ordering by keeping every public PostgreSQL test above the private-method region.
- Added `validation-hashes.sha256` and `validation-evidence.md`, a secret-free 78-entry contract covering the tool/SDK/dependency/build pins, solution and relevant project wiring, regeneration script/config/T4, all 17 selected table sources, all 14 selected view sources, the generated context/model, and focused SQL validation evidence.

## Second re-review remediation

- Extended the PostgreSQL parser for legal `CREATE [OR REPLACE] [TEMP|TEMPORARY] [RECURSIVE] VIEW` syntax. Nonconforming and conforming fixtures now cover recursive ordinary, replace, temporary, and replace-plus-temporary combinations while retaining repeated-result determinism and the SQL Server isolation test.
- Expanded the hash manifest from 52 to 78 unique, deterministically sorted paths. It now contains every table and view selected by `efcpt-config.json`, `.config/dotnet-tools.json`, the SDK/dependency/build pins and relevant project wiring, generation controls, and all owned generated outputs.
- Documented an executable verification contract: exactly 78 unique sorted entries, zero missing files, zero hash mismatches, exact selection parity for 17 tables and 14 views, 31 generated Domain files, one generated context, and presence of the local tool manifest. The manifest remains explicitly a one-run snapshot, not evidence of a second EFPT run or a substitute for a live deployment plan.

## Validation evidence

The following checks passed on the Task 2 branch:

- `Paradigm.Enterprise.Cli.Tests`: 17 focused database-validator tests passed; after the identifier correction, the focused analysis plus database-validator set passed 25 tests with zero failures.
- Final remediation focused CLI analysis/database-validator suite: 26 passed, zero failed.
- Final remediation Beacon architecture suite through the example's Microsoft.Testing.Platform runner: 28 passed, zero failed.
- Second re-review focused CLI suite: 26 passed, zero failed; architecture suite: 28 passed, zero failed.
- Expanded hash contract verification: 78 entries, 78 unique paths, zero missing files, zero mismatches, and exact 17-table/14-view EFPT selection parity.
- Database project Release build: succeeded with zero warnings and zero errors.
- Beacon solution Release build: succeeded with zero warnings and zero errors.
- Beacon test suite against the disposable SQL Server database: 166 passed, zero failed.
- Source CLI `doctor`: succeeded for the mixed solution and correctly treated 15 managed .NET projects while excluding the SQL project from .NET project analysis.
- Source CLI `validate`: succeeded after the generic-marker identifier correction.
- Installed CLI `database validate --strict --format json`: completed with an empty diagnostics array for the current compliant database source.
- Installed CLI package validation and Domain, Data, and Providers checks: passed.
- EF Power Tools regeneration safety fixtures: passed.
- Final remediation source CLI strict database validation and focused Domain, Data, and Providers checks: succeeded with empty diagnostics.
- SQL `FoundationSmoke.sql`: passed against the published disposable database.
- Two consecutive EF Power Tools generations were byte-stable.
- Generated model checks found 31 entity/view files, 31 `DbSet` properties, 14 keyless view mappings, and all 14 canonical `ToView` names.
- Static search across database and code paths found no remaining whole-word legacy pricing-view references, and parsed all 14 view names with zero suffix violations. Historical and task documentation may name the old objects when explaining the rename.
- OpenAPI regeneration was byte-stable and contains no Operations view contracts or sensitive Operations fields.
- TypeScript client regeneration was byte-stable by Git blob hash; the apparent worktree modification is line-ending metadata only and has no content diff when end-of-line differences are ignored.
- Client `npm ci` and contract check passed with zero reported vulnerabilities.
- The first DACPAC deployment report and publication introduced the view changes without alerts, table rebuilds, or data-loss operations.
- Final remediation SQL project Release build: succeeded with zero warnings/errors; its local DACPAC SHA-256 was `b88a715b17e117d8ed6849aeed54d3423314b8b36c2db727d52cf2c589e21af2`.
- Live EF tests queried all three Operations views in one rollback-scoped fixture and verified row parity, display joins, state code, nullable audit display values, exact hash bytes, and metadata JSON.

## Known gaps and environment notes

- The repository's installed Paradigm CLI tool is version 1.1 and therefore does not contain the new `PEDB112` implementation or the source-level generic-marker correction. The current source CLI and focused regression tests are the authoritative validation for those changes; publishing an updated package is outside this example task.
- Running the full CLI test project from inside `examples/beacon-ar` is blocked by that example's `global.json` selection of Microsoft.Testing.Platform while the CLI test project uses VSTest. Focused CLI tests were run successfully from the repository root. No broad rerun was performed during handoff.
- A second DACPAC deployment report was not a complete no-op: DACFx proposed drop/recreate operations for seven existing check constraints. It proposed no table data loss or table rebuild. This pre-existing constraint normalization behavior is recorded in `decisions.md` and is not represented as an idempotent second publish.
- The original sanitized deployment-plan XML was not retained before the disposable database was removed, and final remediation did not recreate live infrastructure merely to reproduce it. The retained hash manifest, clean SQL build, DACPAC hash, static schema/view checks, and prior live results are stronger static evidence, but they do not replace deployment-plan inspection. Recreate and inspect a sanitized plan before any non-disposable publication.
- One architecture invocation using legacy positional `dotnet test <project>` failed because the example opts into Microsoft.Testing.Platform. The supported `dotnet test --project ...` invocation then passed 28/28; this was a runner-mode error, not a test failure.
- The client toolchain emitted a warning that Node 24.13 is below Angular's requested 24.15 patch level; installation and checks still passed.
- Task 2 deliberately retains the single `ReceivablesDbContext`. The four-context and folder migration belongs to Task 3, so this task does not preempt or partially implement it.

## Temporary infrastructure

Validation used a disposable SQL Server container named `beacon-ar-task2-sql`, database `BeaconArTask2`, on local port 11433. It contained validation-only data and is removed after the handoff; that temporary database is not recoverable or part of the repository deliverable.
