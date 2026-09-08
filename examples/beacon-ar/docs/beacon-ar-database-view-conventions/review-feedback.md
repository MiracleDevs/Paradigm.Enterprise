# Beacon AR database view conventions review feedback

> **Historical persistence note (superseded 2026-08-03):** This document records an earlier implementation stage. Current persistence ownership is defined by docs/beacon-ar-context-boundaries: four disjoint Access, MasterData, Operations, and Sales contexts/configurations; there are no Receivables or Reporting application roots.

## Findings

### Medium — the claimed reusable T4 identifier adaptation is still application-specific and incomplete

- `src/BeaconAr.Data/CodeTemplates/EFCore/DbContext.t4:52` hard-codes `DbContextBase<int>`, and `tests/BeaconAr.Architecture.Tests/LayerBoundaryTests.cs:61` locks that literal into the template test. The entity template otherwise derives identifier types from EF metadata, so this one fixed type prevents the official-derived template from being reusable for an application whose actor identifier is `long`, `Guid`, or another supported `TId`. It also conflicts with the framework rule to carry the selected identifier type consistently.
- `src/BeaconAr.Data/CodeTemplates/EFCore/EntityType.t4:36-42` derives the audit actor type only from `CreatedByUserId`. It checks that `ModifiedByUserId` exists but never checks that its non-nullable CLR type matches `CreatedByUserId`. A schema with mismatched actor columns would emit `IAuditableEntity<TDate,TActorId>` from only one side and then fail compilation or publish a false audit contract.
- The current Beacon output is correct: `AuditLog`/`IdempotencyRequest` and their views use `long` entity IDs, their actor columns remain `int`, and the template deliberately omits the incompatible current audit interface. The defect is in the documented reusable template boundary, not those generated Beacon types.
- Smallest safe correction: derive the context actor ID from an unambiguous model/configuration input (for Beacon, `ApplicationUser.Id`) instead of a literal, require both audit actor columns to resolve to the same underlying type before emitting the audit interface, and add metadata-driven template fixtures for `int`, `long`, and mismatched entity/actor IDs. Keep the Beacon assertion on the generated `ReceivablesDbContext` being `DbContextBase<int>`, but do not assert that the T4 source itself contains the literal.

### Medium — `PEDB112` does not enforce the documented rule for all PostgreSQL view forms

- `src/Paradigm.Enterprise.Cli/Database/DatabaseProjectValidator.cs:343` recognizes only `CREATE [OR REPLACE] VIEW`. PostgreSQL `CREATE MATERIALIZED VIEW` (and the legal `TEMP`/`TEMPORARY` modifier on ordinary views) does not match, so a non-suffixed view in those forms receives no `PEDB112` diagnostic even though `docs/cli.md` and the database/review guidance say every PostgreSQL view object ends in `View`.
- The new SQL Server and PostgreSQL tests at `DatabaseProjectValidatorTests.cs:99-113` and `:287-301` cover only ordinary `CREATE VIEW`/`CREATE OR REPLACE VIEW`. The existing valid-project fixtures prove the positive ordinary-view path, warning/error promotion, and repeated-result determinism, but not these false-negative boundaries.
- Smallest safe correction: extend object-kind parsing to recognize the supported PostgreSQL view modifiers/materialized-view form, define whether materialized views share `PEDB112` in the diagnostic documentation, and add compliant/noncompliant fixtures for each accepted grammar. If materialized views are intentionally out of scope, narrow the CLI and skill wording explicitly instead of claiming every PostgreSQL view object.

### Medium — active EFPT task records contradict the new generation ownership boundary

- `docs/beacon-ar-ef-power-tools/implementation-plan.md:14,48` still says the T4 templates own collection trackers and the `CustomerAddress` cardinality correction, while this change deliberately moves trackers into domain partials and the relationship correction into `src/BeaconAr.Data/Receivables/ReceivablesDbContext.Relationships.cs`.
- `docs/beacon-ar-ef-power-tools/decisions.md:17` says `DomainTracker<TEntity>` members are emitted by generation for two named aggregates. They are now handwritten in `Domain/Sales/Quote.Behavior.cs` and `SalesOrder.Behavior.cs`; `EntityType.t4:54` makes the generated aggregate set permanently empty.
- `LayerBoundaryTests.cs:115` is consequently named `GeneratedCollectionTrackersRespectAggregateOwnership` even though it only observes the final compiled partial types and cannot establish that the members were generated. This is exactly the kind of stale guidance the task was intended to remove for the next person or agent.
- Smallest safe correction: add an explicit superseding note to the earlier task records (preserving their historical decisions) and rename/reframe the test around final aggregate ownership. State that EFPT emits persistence navigations, domain partials own aggregate trackers/behavior, and the context partial owns the relationship correction.

### Low — the generic template retains unreachable aggregate-generation machinery

- `EntityType.t4:51-54` creates `aggregateNavigations` with `collectionNavigations.Where(_ => false)`, after which the template retains unreachable branches for private aggregate collections, recursive validation, add/remove methods, and trackers at `:188-197`, `:231-238`, and `:260-281`.
- This is not a runtime defect in the generated Beacon model, but it is a confusing maintenance trap in a template now documented as the reusable official-derived source. It makes the compatibility delta larger and obscures the actual ownership decision.
- Smallest safe correction: remove the dead aggregate-emission branches and related unused variables, or replace them with a documented generic opt-in seam plus positive/negative tests. Do not restore Beacon type-name checks.

### Low — a public test is placed inside the private-member region

- `src/Paradigm.Enterprise.Cli.Tests/DatabaseProjectValidatorTests.cs:236` opens `#region Private Methods`, but `Postgresql_view_suffix_is_a_deterministic_policy_diagnostic` is a public test method at `:287`. This violates the repository's required member-region ordering and makes the new test easy to miss.
- Smallest safe correction: move the PostgreSQL test beside the other public test methods before the `Public Methods` region closes; leave only helpers in `Private Methods`.

## Verified

- Parsed inventory contains exactly 14 SQL view files/objects, and every file/object ends in `View`. All are under `src/database/views`; no other `CREATE VIEW` source was found.
- `QuotePricingView` and `SalesOrderPricingView` are schema-bound, preserve their grouping keys/totals, and are the only helper dependencies used by the stable schema-bound `QuoteView` and `SalesOrderView`. No live old-name alias or code/SQL reference remains; documentation mentions the old names only to explain the rename.
- `AuditLogView`, `IdempotencyRequestView`, and `IdempotencyStateView` retain their base mapping surfaces, use only unique-key many-to-one joins, and are schema-bound. Required foreign keys make the inner joins row-preserving; nullable audit-user joins are left joins. `FoundationSmoke.sql:56-61` checks duplicate IDs and base/view count parity.
- Operations projections remain internal. Static source inspection found no controller/serializer registration, and the checked OpenAPI and TypeScript artifacts contain none of the three view names or `keyHash`, `requestHash`, or `metadataJson`. The architecture/OpenAPI assertions cover this boundary.
- The generated output carries the early EFPT ownership marker and compiled `GeneratedCode("EFCorePowerTools", "10.1.1386")` metadata. The five added/renamed keyless mappings use the canonical `ToView` names. `AuditLogView.Id` and `IdempotencyRequestView.Id` are `long`; the generated interfaces inherit `IEntity<long>`.
- The official local source repository is clean at revision `522906b9151d566a5dddd2609b66da36726368a7`; its upstream T4 hashes exactly match `PROVENANCE.md`. No Beacon/entity/relationship whitelist token exists in either current T4 file.
- The `Customer`/`CustomerAddress` partial configuration produces one non-unique FK with the `CustomerAddresses` collection; the metadata/relationship behavior test passes.
- The SQL project uses one explicit model glob per category with default SQL items disabled. The Operations folder is visible metadata only and introduces no duplicate `Build` item.

## Validation run during review

- Focused CLI analysis/database-validator tests: 25 passed, 0 failed.
- Beacon architecture tests: 27 passed, 0 failed.
- Beacon Release solution build: succeeded with 0 warnings/errors.
- SQL Server database project Release build: succeeded with 0 warnings/errors and produced the DACPAC.
- Source CLI strict database validation: succeeded with an empty diagnostics array.
- Source CLI Domain and Data checks: succeeded.
- Regeneration failure-recovery/redaction fixtures: succeeded.
- Static old-alias and API/OpenAPI/client exposure searches: clean.
- `git diff --check`: clean.

## Unverified acceptance evidence and DACPAC risk

- I did not rerun EFPT against a fresh live SQL Server or the complete live database integration suite. The disposed validation container and connection are not available in the worktree.
- `change-summary.md:35,41` states that two EFPT generations were byte-stable and that the first DACPAC deployment report contained no alerts/table rebuild/data loss, but no sanitized deployment report or generated hash manifest is retained. The regeneration script performs one generation per invocation; its checked fixture proves recovery/redaction and exact ownership, not second-run byte stability. Reproduce those two checks before final acceptance.
- The documented second DACPAC report proposed drop/recreate operations for seven existing check constraints. It reports no table rebuild/data loss, but the report should be re-inspected and retained long enough for final review before any non-disposable publication. Do not characterize the second publish as a no-op.

## Review disposition

Changes requested. The database/view implementation itself is sound, but the generic T4 contract, complete `PEDB112` grammar boundary, and stale generation-ownership guidance should be corrected before this task is accepted and its learnings are promoted as reusable framework guidance.

## Final implementer remediation

All requested code and guidance findings were addressed for re-review:

- `DbContext.t4` now derives one context audit-actor identifier type from complete audited entity metadata and emits `DbContextBase<TActorId>` without a literal application type. `EntityType.t4` resolves both actor columns and emits a clear generation error when their normalized types differ.
- Architecture coverage no longer locks the T4 source to `int`. Metadata fixtures cover matching `int`, matching `long`, and mismatched actor types, while compiled Beacon metadata still verifies its correct `DbContextBase<int>` outcome and its long-entity/int-actor Operations boundary.
- `PEDB112` has separate SQL Server and PostgreSQL creation grammars. PostgreSQL coverage includes ordinary, `OR REPLACE`, `TEMP`, `TEMPORARY`, replace-plus-temporary, `MATERIALIZED VIEW`, and materialized `IF NOT EXISTS` forms, with compliant/noncompliant and repeat-order assertions. A SQL Server regression test proves PostgreSQL-only modifiers are not parsed as SQL Server view declarations.
- The active EFPT plan, decisions, historical summary note, provenance, and architecture test now assign persistence navigations to T4, aggregate trackers/behavior to Domain partials, and the `Customer`/`CustomerAddress` correction to the context partial.
- Removed the unreachable aggregate-generation variables, private collection branches, recursive validation branch, and add/remove/tracker emission from `EntityType.t4`.
- Restored test class ordering: all public PostgreSQL tests are in the public-method region; only helpers remain in the private-method region.
- Added `validation-hashes.sha256` for the exact final database sources, templates/configuration, regeneration script, and generated output. A current Release DACPAC was built and hashed, but no live deployment report was recreated because the disposable database had been removed. This remains an explicit final deployment-review gap before any non-disposable publication.

Focused remediation validation:

- CLI analysis/database-validator tests: 26 passed, 0 failed.
- Beacon architecture tests: 28 passed, 0 failed through `dotnet test --project` and Microsoft.Testing.Platform.
- Beacon Release solution build: 0 warnings, 0 errors.
- SQL project Release build: 0 warnings, 0 errors; DACPAC SHA-256 `b88a715b17e117d8ed6849aeed54d3423314b8b36c2db727d52cf2c589e21af2`.
- Source CLI strict database validation: success, empty diagnostics.
- Source CLI Domain, Data, and Providers checks: success, empty diagnostics.
- Regeneration failure-recovery and redaction fixtures: passed.
- One legacy positional architecture-test invocation failed at runner selection because the example requires Microsoft.Testing.Platform's `--project` form; the supported invocation above passed all tests.

Disposition after remediation: ready for reviewer re-evaluation. The live sanitized deployment-plan artifact remains deliberately unclaimed and must be reproduced before a real database publication.

## Re-review

### Remaining findings

#### Medium — PostgreSQL recursive views still bypass `PEDB112`

- `src/Paradigm.Enterprise.Cli/Database/DatabaseProjectValidator.cs:343-347` now correctly separates SQL Server and PostgreSQL creation grammars and recognizes the ordinary, replace, temporary, and materialized forms added by the remediation. However, the PostgreSQL grammar has no optional `RECURSIVE` modifier before `VIEW`.
- PostgreSQL's documented syntax is `CREATE [ OR REPLACE ] [ TEMP | TEMPORARY ] [ RECURSIVE ] VIEW`. Consequently, declarations such as `CREATE RECURSIVE VIEW "LegacyProjection" ...` and `CREATE OR REPLACE TEMP RECURSIVE VIEW "LegacyProjection" ...` are not parsed and receive no `PEDB112`, despite the durable guidance requiring every PostgreSQL view to end in `View`. See the official PostgreSQL `CREATE VIEW` synopsis: `https://www.postgresql.org/docs/current/sql-createview.html`.
- `DatabaseProjectValidatorTests.cs:225-259` exercises eight ordinary/temporary/materialized forms but has no recursive compliant or noncompliant fixture. The test and `docs/cli.md:187` therefore describe only a subset of legal ordinary-view grammar while the higher-level skill/reference wording remains universal.
- Smallest safe correction: allow optional `RECURSIVE` after the optional temporary modifier and before `VIEW`, add ordinary/replace/temporary recursive positive and negative fixtures, and list the modifier in the CLI grammar summary. Keep the SQL Server negative-boundary test.

#### Medium — the validation hash manifest does not cover the exact regeneration inputs it claims to cover

- `validation-hashes.sha256` is internally valid: all 52 listed paths exist and every current SHA-256 matches. It covers 31 generated Domain files, the generated context, 14 view sources, the SQL project/smoke script, the two T4 files, EFPT config, and the regeneration script.
- It does not include any of the 17 table sources explicitly selected by `src/BeaconAr.Data/efcpt-config.json:3-20`, nor `.config/dotnet-tools.json`, which owns the exact EFPT version. A table-column/key/nullability change or tool-pin change can therefore alter future generation inputs without invalidating this manifest.
- This contradicts `change-summary.md` under **Final review remediation**, which calls the manifest coverage “the exact database inputs,” and the final-implementer note above, which calls it “the exact final database sources.” It also means the manifest cannot by itself support the stated deterministic-regeneration evidence; it records one output snapshot, not the complete input boundary or a two-run comparison.
- Smallest safe correction: include the local tool manifest and every explicitly selected table/view SQL source (and any other generator input used by the script) in a deterministically sorted manifest. Narrow the wording if the file is intended only as a partial changed-file snapshot. Continue to treat second-run byte stability and live deployment-plan inspection as separate evidence.

### Resolution of the original findings

- **Generic T4 actor/identifier inference:** resolved. `DbContext.t4` normalizes both actor columns across complete audited table metadata, requires exactly one context actor type, and emits `DbContextBase<TActorId>` without an `int` literal. `EntityType.t4` compares both normalized actor types and stops generation on a mismatch. Compiled Beacon metadata still proves the expected `DbContextBase<int>` and mixed `long` entity/`int` actor outcome. The metadata tests cover matching `int`, matching `long`, and mismatched types.
- **Original `PEDB112` ordinary/temp/materialized gap:** resolved for every form named in the first review, with separate engine grammars, strict/warning behavior, compliant/noncompliant cases, deterministic ordering, and a SQL Server isolation test. The recursive-view finding above is an additional legal PostgreSQL form discovered during the complete grammar re-review.
- **Stale EFPT ownership guidance:** resolved. The active plan/decisions and historical superseding note now consistently assign persistence navigations to T4, aggregate trackers/behavior to Domain partials, and the `CustomerAddress` relationship correction to the context partial. The architecture test was renamed and checks the partial source ownership.
- **Unreachable aggregate template machinery:** resolved. The always-empty aggregate list and all unreachable collection/tracker/recursive-validation/add-remove emission branches are gone; no application type-name whitelist was reintroduced.
- **CLI test member-region ordering:** resolved. All public tests precede `#region Private Methods`; only helpers remain in the private region.

### Re-review validation

- Focused CLI analysis/database-validator tests: 26 passed, 0 failed.
- Beacon architecture tests using the required Microsoft.Testing.Platform `--project` form: 28 passed, 0 failed.
- Hash-manifest verification: 52 entries, 0 missing files, 0 mismatches; omissions are described above.
- Static inspection reconfirmed current generated ownership markers, metadata-derived mixed identifiers, canonical 14-view mappings, relationship partial ownership, and absence of Beacon/type-name whitelists or dead aggregate generation in T4.
- Live EFPT regeneration, database integration, and a sanitized DACPAC deployment report were not rerun in this re-review; the previously documented disposable environment remains unavailable.

### Re-review disposition

Changes requested. All five original code/documentation findings were materially remediated, but the universal PostgreSQL view policy still has a deterministic recursive-view false negative and the new manifest overstates its coverage of the regeneration input boundary.

## Second final implementer remediation

Both re-review findings were corrected for another reviewer pass:

- The PostgreSQL grammar now accepts the documented `CREATE [OR REPLACE] [TEMP|TEMPORARY] [RECURSIVE] VIEW` modifier order. The fixture set adds compliant and noncompliant recursive ordinary, replace, temporary, and replace-plus-temporary declarations while preserving strict/warning counts and deterministic repeated diagnostics. The separate SQL Server grammar and PostgreSQL-only-modifier regression remain unchanged.
- `docs/cli.md` now states the complete supported ordinary-view grammar including `RECURSIVE`.
- `validation-hashes.sha256` now has 78 unique, deterministic entries. It adds all 17 table sources selected by `efcpt-config.json`, `.config/dotnet-tools.json`, `global.json`, central build/dependency pins, solution and relevant Data/Domain/Interfaces project wiring, and the interface generator source. All 14 selected view sources, generation controls, and 32 owned generated outputs remain covered.
- `validation-evidence.md` defines the bounded manifest categories and generation procedure and requires exact entry/uniqueness/order, existence, hash, selection-parity, output-count, and tool-manifest checks. It explicitly does not elevate the snapshot into second-run byte-stability or live deployment-plan evidence.

Second-remediation validation:

- Focused CLI analysis/database-validator tests: 26 passed, 0 failed.
- Beacon architecture tests with Microsoft.Testing.Platform: 28 passed, 0 failed.
- Hash contract: 78 entries, 78 unique paths, 0 missing files, 0 hash mismatches, exact parity with 17 selected tables and 14 selected views, 31 generated Domain files, one generated context, and the local tool manifest present.
- `git diff --check`: clean apart from Git's informational LF-to-CRLF worktree notices.

Disposition after second remediation: ready for reviewer re-evaluation. The separately documented live deployment-plan and second-run EFPT evidence gaps remain unchanged and are not claimed by the expanded manifest.

## Final re-review

### Findings

No actionable defects remain in the second remediation.

### Verified second-remediation corrections

- `DatabaseProjectValidator.cs:343-347` now uses separate SQL Server and PostgreSQL creation grammars. The PostgreSQL branch accepts the documented `CREATE [OR REPLACE] [TEMP|TEMPORARY] [RECURSIVE] VIEW` modifier order as well as `CREATE MATERIALIZED VIEW [IF NOT EXISTS]`; the SQL Server branch does not recognize PostgreSQL-only modifiers.
- `DatabaseProjectValidatorTests.cs:225-264` covers compliant and noncompliant ordinary, replace, temporary, recursive, replace-plus-recursive, temporary-recursive, replace-plus-temporary-recursive, and materialized forms. It verifies warning/error promotion and deterministic repeated results. The SQL Server isolation fixture remains at `:117-129`.
- `docs/cli.md:187` now states the same complete supported PostgreSQL grammar, including `RECURSIVE`, while the durable database/review guidance retains the universal `View` suffix rule.
- `validation-hashes.sha256` satisfies its documented bounded contract: 78 entries, 78 unique paths, deterministic `Sort-Object -Unique` order, zero missing files, and zero SHA-256 mismatches.
- Manifest selection parity is exact: all 17 tables and all 14 views selected by `efcpt-config.json`, all 31 generated Domain files, the generated context, and `.config/dotnet-tools.json` are present. It also covers the stated SDK/dependency/build pins, solution/project wiring, interface generator, SQL project/smoke evidence, and generation script/config/T4 controls.
- `validation-evidence.md` accurately limits the manifest to one exact input/output snapshot. It no longer claims that the manifest proves a second EFPT run, identifies the live SQL Server image, or substitutes for DACPAC deployment-plan review.

### Final focused validation

- Focused CLI analysis/database-validator tests: 26 passed, 0 failed.
- Manifest contract: 78 entries, 78 unique paths, deterministic order, 0 missing files, 0 hash mismatches, exact selected-table/view and generated-output parity.
- `git diff --check`: no whitespace errors; only informational worktree line-ending notices were emitted.

### Remaining environment/acceptance gaps

- This final re-review did not recreate the disposed SQL Server environment, rerun EFPT twice, rerun live database integration, or generate a sanitized DACPAC deployment plan.
- Those gaps are now stated consistently and are not represented as evidence supplied by the manifest. A fresh deployment-plan inspection remains required before any non-disposable database publication, and a fresh two-run generation comparison remains the strongest way to re-establish byte stability after future input changes.

### Final disposition

Approved. The original findings and both follow-up findings are resolved, the focused deterministic checks pass, and no actionable code, test, documentation, or manifest defect remains within Task 2. Approval does not waive the explicitly documented live deployment-plan gate before a real database publication.
