# Beacon AR context boundaries — review feedback

## Outcome

Changes requested. The four capability roots, context registrations, scalar cross-context relationships, typed routine wrappers, mapper placement, and repository SQL removal are directionally correct, and the non-live build/test surface is green. The branch is not ready to merge because one required live contract test is statically inconsistent with the schema, the architecture-test rewrite deleted substantial unrelated coverage, and the claimed regeneration recovery/determinism guarantees are not actually exercised or complete.

## Findings

### P1 — The required routine-contract test will fail against the database schema

- **Evidence:** `tests/BeaconAr.Database.IntegrationTests/FoundationSchemaTests.cs:60-67` expects the four Sales reference routines to return lengths/types that do not match their selected columns. The schema declares `Customer.Name` as `nvarchar(120)`, `AddressType.Code` as `nvarchar(32)`, address `Label/City/State/PostalCode` as `nvarchar(120/120/120/32)`, `Country` as `nchar(2)`, `Product.Sku/Name` as `nvarchar(32/120)`, and `Carrier.Name` as `nvarchar(120)`. The test instead expects `nvarchar(200)`, `nvarchar(30)`, `nvarchar(100)`, `nvarchar(30)`, `char(2)`, `nvarchar(80)`, and other incorrect widths.
- **Observed behavior:** the test was among the live SQL tests skipped because no connection string was present (`change-summary.md:33,43`). `sys.dm_exec_describe_first_result_set_for_object` reports the actual selected-column metadata, so this is a deterministic failure once a disposable database is supplied.
- **Consequence:** the branch cannot pass its own required exact-signature acceptance suite, and the stated verification of all 14 routine signatures/result order is not established.
- **Smallest safe correction:** align the expected result metadata to the actual SQL Server schema, add the moved `GetDashboardSummary` result contract promised by `implementation-plan.md:182`, publish the DACPAC to a disposable database, and run `ContextBoundaryRoutineContractsAreExact` plus the routine execution/concurrency tests before approval.

### P1 — `LayerBoundaryTests` lost broad, unrelated architecture protections

- **Evidence:** the base version contained 26 architecture tests; the replacement at `tests/BeaconAr.Architecture.Tests/LayerBoundaryTests.cs:11-80` contains only five narrow checks. Removed coverage includes the project-reference dependency graph, Domain isolation from ASP.NET/SQL Server, official T4 signatures/provenance/application-name exclusions, exact generated entity/view scalar interfaces and nullability, aggregate tracker ownership, Operations persistence-view HTTP exclusion, audit identifier rules, customer-address relationship cardinality/indexes, unsupported generator targets, OpenTelemetry logging preservation, provider discovery/counts, typed CRUD bases and mutation guards, absence of local Provider bases and SQL-driver exception classification, one-round-trip search and mapper ownership/determinism, coordinator injection, database project identity/folders, canonical six-folder solution membership, infrastructure leakage, Sales transport/aggregate ownership, and Sales search/dashboard routine invariants.
- **Observed behavior:** the new ten-test architecture project passes, but that success is achieved after deleting safeguards that are independent of the context rename. This contradicts `implementation-plan.md:178-181`, which says every existing test remains in scope and specifically requires retaining the official T4/provenance checks.
- **Consequence:** future regressions in solution layout, dependency direction, generated contracts, provider safety, API exposure, telemetry, and SQL routine behavior can now pass CI. This also erases protections established by earlier reviewed tasks.
- **Smallest safe correction:** restore the prior architecture tests, adapt only their Receivables/Reporting paths, namespaces, context construction, and exact expected counts to the four-context model, then keep the five new boundary checks as additions. Where an assertion genuinely moved, point to an equivalent test with the same semantic coverage rather than deleting it.

### P1 — Regeneration failure recovery is a synthetic copy test and can leave a dirty mixed boundary

- **Evidence:** `build/regenerate-persistence.ps1:159-168` never invokes EFPT or a failing generation pass; it appends to one owned file and immediately restores it. `Restore-Backup` at `:120-125` restores only the 35 expected generated paths. If a partial EFPT pass creates an unexpected generated-marker file, or creates the known readme before failing at `:140-144`, the catch at `:190-192` does not remove it. The handwritten preservation hash at `:178,187-188` covers only `*.Behavior.cs` and omits the co-located `MasterDataDbContext.Relationships.cs` partial.
- **Observed behavior:** `-TestFailureRecovery -TestRedaction` passes, but it proves only that `Copy-Item` restores one deliberately modified owned file. It does not prove the `change-summary.md:12,26` and README claim that every boundary and partial is restored after any generation failure.
- **Consequence:** a failed multi-config regeneration can leave stale/new generated files or a byproduct in the working tree, and the only handwritten context partial is outside the byte-preservation assertion. That violates the exact-file recovery policy added by this task.
- **Smallest safe correction:** make the recovery fixture exercise the same failure path as generation (with a deterministic injected failure after at least one boundary), snapshot all generated-manifest paths plus every handwritten partial, record the pre-run set of generated-marker/byproduct files, and on failure restore the manifest while removing only newly created recognized generator outputs. Assert full path/hash parity afterward.

### P2 — The new boundary tests do not prove the exact ownership and mapper guarantees claimed by the plan

- **Evidence:** `GeneratedPersistenceTests.cs:23-40` checks only that four JSON files contain 31 distinct selections; it does not compare the exact 17 table/14 view inventory, validate the expected object-to-capability mapping, or verify that generated CLR/interface ownership is complete and unique. `:61-78` samples only a few present/absent navigations. `:82-89` counts files rather than asserting the exact generated/partial pairs. `LayerBoundaryTests.cs:54-62` checks only that four registrar filenames exist, and `:66-79` searches routine names recursively rather than asserting their exact capability paths or the complete routine inventory.
- **Missing promised coverage:** `implementation-plan.md:180` also requires four generated interface namespaces, mapper generation determinism, and regeneration failure recovery across all boundaries. None is exercised by the current architecture suite. There is no test that all typed parameter/data-reader mappers are registered, stale/parallel mapper outputs are absent, or a second mapper generation is byte-stable.
- **Consequence:** swapped config ownership, a wrong-but-still-31 object selection, stale generated interfaces/mappers, an incorrectly placed routine, or a missing mapper registration can pass the non-live suite.
- **Smallest safe correction:** assert exact per-capability object/file/type/interface sets derived from a reviewed manifest, enumerate all cross-context FK navigation exclusions and required same-context relationships, verify exact routine paths, inspect the complete mapper registration surface, and run the mapper generator twice with path/hash comparison and failure recovery.

## Verified strengths

- The Release mixed solution builds with zero warnings/errors.
- Architecture tests pass 10/10; Domain tests pass 37/37; Provider tests pass 19/19; non-live Web API tests pass 37/37 with 19 database-backed API cases skipped.
- Focused source scans found zero `Receivables`/`Reporting` production roots or namespaces and zero forbidden raw-SQL API/literal matches in production repository folders.
- The four contexts are registered with the same scoped `SqlServerDbContextConnectionProvider` name, and `PersistenceSession` clears Access, MasterData, Operations, and Sales trackers.
- Generated Sales navigations inspected are same-context only; cross-context references are scalar IDs. The official T4 files are unchanged from `new-example-from-skills` and contain no Beacon-specific whitelist tokens.
- The 14 new routine sources are in the intended Access/MasterData/Operations/Sales folders; typed wrappers use the context-owned connection, pass the Unit of Work, have 30-second timeouts, and generated capability registrars are reached by the module initializer.
- `git diff --check` reports no whitespace errors. The 15-entry routine source manifest has no missing paths or hash mismatches.

## Validation and remaining gaps

- Executed successfully:
  - `dotnet build BeaconAr.slnx --configuration Release --no-restore`
  - focused Architecture, Domain, Provider, and Web API tests
  - `build/regenerate-persistence.ps1 -TestFailureRecovery -TestRedaction` (subject to the finding above)
  - repository SQL, obsolete-root/namespace, T4-diff/token, manifest, and whitespace scans
- No disposable SQL Server connection was available. Consequently, routine metadata/execution, lock serialization, concurrent first-use/idempotency, cross-context save failure rollback, late participant enlistment, and authenticated API persistence remain unverified in this review. The existing live test inventory is substantial, but it must actually run after the routine-contract expectations are fixed.
- The example manifest pins `Paradigm.Enterprise.Cli` 1.1.0. The task documents that package's DACPAC/identifier diagnostics separately from the current repository-source CLI. That distinction is correct: installed-package lag must not be presented as an application defect, and source-CLI success must not be represented as validation by the pinned tool. A bounded combined installed-CLI audit/check run was terminated after producing no result, so the implementer's recorded CLI evidence was inspected but not independently reproduced here.

## Review decision

Not approved. Resolve all P1 findings, restore the lost architecture coverage, complete the exact ownership/mapper fixtures, and run the live SQL Server acceptance matrix before the next review pass.

## Re-review

### Verdict

Not approved. The routine contract and architecture coverage findings are resolved, but the required persistence failure-recovery proof remains incomplete and the recorded disposable-environment cleanup is contradicted by the live Docker state.

### Resolved findings

- The live routine metadata expectations now match the SQL sources, including the complete `Operations.GetDashboardSummary` result contract and the corrected Sales reference widths/types.
- All 26 established layer-boundary tests are restored in `EstablishedLayerBoundaryTests.cs` with capability-aware expectations. Together with the five focused boundary tests and nine generated-persistence tests, the targeted architecture suite now passes 40/40.
- Exact manifests now cover per-capability database selections, generated files and CLR/interface ownership, the complete navigation matrix, exact routine paths, the 36-file mapper inventory and registrations, mapper second-run determinism, and recognized persistence output paths.
- Independent non-live verification passed: `dotnet build BeaconAr.slnx --configuration Release --no-restore` completed with zero warnings/errors, and `BeaconAr.Architecture.Tests` passed 40/40 with no skips.

### P1 - The failure fixture still simulates generation and cannot recover a changed handwritten partial

- **Evidence:** `build/regenerate-persistence.ps1:228-242` does not invoke EF Core Power Tools or `Invoke-GenerationPass`. It appends a comment directly to one generated context and writes an injected marker file/readme; the source itself labels this `simulated EFPT boundary output`. This proves the production wrapper can undo those manual mutations, but it does not prove recovery after a real completed capability generation as claimed in `change-summary.md:53`.
- **Evidence:** `New-Backup` and `Restore-Backup` at `build/regenerate-persistence.ps1:137-154` copy only `Get-OwnedPaths` (the 35 generated files). `Invoke-RecoverableWorkflow` hashes handwritten partials, but does not back them up. If a generator changes a `*.Behavior.cs` file or `MasterDataDbContext.Relationships.cs`, the success check throws, then recovery restores only generated files; the protected-hash comparison also throws and leaves the handwritten partial modified. The current fixture never mutates a partial, so it cannot expose this failure.
- **Consequence:** the script detects, but does not recover from, one of the exact shared-directory boundary violations it promises to recover. The documented statement that it "restores every boundary" and proves partial preservation after failure is stronger than the implementation/test.
- **Smallest safe correction:** back up and restore the exact protected manifest, including every enumerated handwritten partial, while continuing to delete only newly introduced recognized generated/byproduct files. Add a fixture mutation of a handwritten partial and assert byte/path parity. For the live acceptance path, inject failure only after an actual EFPT capability invocation has completed, then prove the same parity; keep the fast manual mutation fixture as a separate non-live wrapper test if useful.

### P2 - The documented disposable SQL Server cleanup did not occur

- **Evidence:** `change-summary.md:46` states that the disposable containers, image, and database were removed. Live inspection found `sqlserver-a91e0506`, created for `beaconar.apphost`, still present as an exited SQL Server 2022 container with a persistent `beaconar.apphost-a91e05069e-sqlserver-data` volume reference. The image necessarily remains addressable by that container. No durable TRX/log artifact was found; the live pass counts are recorded only in `change-summary.md`.
- **Consequence:** a persistent database resource remains after a validation run described as disposable, and the evidence document is inaccurate about cleanup.
- **Smallest safe correction:** remove the task-owned container and persistent volume after confirming their exact ownership, or revise the evidence to identify this as an intentionally retained/pre-existing Aspire resource and avoid calling it disposable/removed. Preserve a secret-free test result artifact or concise command transcript if the live matrix must be auditable after cleanup.

### Re-review decision

Not approved. Correct the recovery implementation and exercise partial plus real-generation failure recovery, then reconcile the SQL Server cleanup/evidence statement. The remaining ownership, navigation, routine, mapper, determinism, restored-coverage, and routine-metadata requirements reviewed here are satisfactory.

## Second remediation implementation evidence

The two remaining re-review findings were addressed for the next reviewer pass:

- `regenerate-persistence.ps1` now declares the exact 13-file handwritten partial manifest and backs up/restores it together with the exact 35 generated files. Pre-existing recognized generator/byproduct paths are also backed up and hashed; failure cleanup deletes only newly introduced recognized outputs before restoring the snapshot.
- The fast fixture now mutates a generated context and a protected behavior partial, creates a recognized generated-marker output and EFPT readme, injects failure, and proves complete path/hash parity after the production recovery wrapper runs.
- `-TestLiveFailureRecovery` invokes actual EFPT for Access against the published database, allows Access generation to complete, injects failure before MasterData, and proves complete generated/protected/recognized-output path/hash parity through the same production catch/recovery path.
- Fast and live fixtures both passed with state fingerprint `A9A8494DB003348634A3DCFEDA0230D9BBAE2999A0CC9F3860C9B439DEB39DDE`.
- A freshly labeled Task 3 environment completed governed DACPAC publish/probe, live database tests (24/24), live Web API tests (19/19), Architecture tests (40/40), and a zero-warning/error Release build. The exact task-owned containers and bootstrap image were removed.
- Docker inspection established that `sqlserver-a91e0506` and `beaconar.apphost-a91e05069e-sqlserver-data` were created on 2026-08-02 as a persistent Aspire resource, before this remediation environment. They were correctly left untouched. Secret-free commands, identifiers, counts, and hashes are recorded in `live-validation.md`.

This section records implementation evidence, not reviewer approval; the reviewer must independently verify the remediation.

## Final re-review

### Verdict

Approved. The second remediation resolves the remaining recovery and validation-evidence findings. No actionable defect remains within Task 3's context-boundary scope.

### Verified remediation

- `Get-ProtectedPaths` now comprises the exact 35 generated persistence outputs plus the exact 13 handwritten partials. `Assert-Inputs` also compares the declared 13-file partial manifest with discovered `*.Behavior.cs` files and `MasterDataDbContext.Relationships.cs`, preventing silent manifest drift.
- `Invoke-RecoverableWorkflow` backs up every protected path and every pre-existing recognized generator/byproduct path. On failure it removes only newly introduced recognized outputs, restores the complete backup, and verifies protected hashes plus recognized path/hash parity.
- The fast fixture mutates a generated context, a handwritten behavior partial, a new generated-marker file, and the recognized EFPT readme. An independent rerun passed recovery and reproduced state fingerprint `A9A8494DB003348634A3DCFEDA0230D9BBAE2999A0CC9F3860C9B439DEB39DDE`.
- `-TestLiveFailureRecovery` uses the same production wrapper, invokes the actual Access EFPT configuration, and injects failure only after Access succeeds and before MasterData begins. `live-validation.md` records matching pre/post fingerprint evidence from the published disposable SQL Server run.
- The artifact hashes recorded in `live-validation.md` match the current regeneration script, DACPAC, and routine manifest exactly.
- Independent Architecture execution passed 40/40 with zero failures or skips; that suite also executes the fast recovery/redaction fixture.
- Current Docker inspection returns no resources with label `paradigm.task=beacon-ar-context-boundaries-r2` and no `beacon-ar-task3-r2-bootstrap` image. The remaining `sqlserver-a91e0506` container carries Aspire's persistent-resource labels and an earlier creation timestamp, consistent with the documented unrelated-resource boundary.

### Validation note

The live SQL Server environment was intentionally removed, so this reviewer did not recreate the full publish/integration/live-EFPT run. The secret-free command record, exact resource ownership, matching artifact/fingerprint hashes, current cleanup state, and independently reproduced fast/architecture checks provide coherent evidence for that run.

### Final decision

Approved for Task 3. The corrected routine metadata, restored 26-test baseline, exact ownership/navigation/routine/mapper manifests, mapper determinism, 35+13 protected recovery, real Access EFPT failure recovery, and task-owned Docker cleanup satisfy the findings from both prior review passes.
