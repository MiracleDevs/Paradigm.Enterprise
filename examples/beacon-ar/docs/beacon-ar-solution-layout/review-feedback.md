# Beacon AR solution layout review feedback

## Result

Changes requested. The canonical `.slnx` layout, project inventory, SQL Server ownership, and semantic architecture enforcement are correct, but one P1 operational regression blocks merge.

## Findings

### P1 - The canonical solution migration makes both the documented doctor flow and quality workflow fail deterministically

Evidence:

- `examples/beacon-ar/start.sh:81` now runs `dotnet tool run paradigm doctor --project BeaconAr.slnx` as the first substantive command in `./start.sh doctor`.
- `examples/beacon-ar/README.md:46-47` presents `./start.sh doctor` as the required local prerequisite check, and `examples/beacon-ar/README.md:75` presents `dotnet tool run paradigm validate --project BeaconAr.slnx` as a current verification command.
- `.github/workflows/quality.yml:156` runs `.paradigm/tools/paradigm validate --project examples/beacon-ar/BeaconAr.slnx` without a guard or fallback, so its nonzero exit stops the deterministic quality-check job.
- `examples/beacon-ar/docs/beacon-ar-solution-layout/change-summary.md:40` already records that both solution-wide commands exit with `PE1002` because the CLI tries to read `src/database/bin/Release/BeaconAr.Database.dacpac` as a .NET metadata assembly.
- Reviewer reproduction confirmed both failures: `dotnet tool run paradigm doctor --project BeaconAr.slnx` exited `1` with `PE1002`, and `dotnet tool run paradigm validate --project BeaconAr.slnx` exited `1` with the same DACPAC `Unknown file format` error after discovering all 16 projects.

Consequence:

The branch retires the only solution path for which these active workflows previously worked, then points local onboarding and CI at a known-failing replacement. `./start.sh doctor` cannot reach the Aspire doctor step, the README verification block cannot complete, and the repository quality workflow will be red. This violates the task's no-runtime/operational-drift constraint and its acceptance requirement for usable, truthful active guidance.

Smallest safe correction:

Keep `BeaconAr.slnx` as the canonical restore/build/test/package/database solution, but do not invoke the affected metadata commands against it until the CLI correctly excludes non-.NET DACPAC outputs. For this task, route `doctor`/`validate` through supported consuming `.csproj` boundaries that cover the application (and retain the separate strict database validator for `BeaconAr.Database.sqlproj`), or fix the CLI's `.slnx` SQL-project classification in a separately reviewed in-scope change. Update `start.sh`, the quality workflow, README, and change summary together, then prove every copyable command exits successfully. Do not remove the SQL project from `02.Modules` to hide the CLI defect.

## Verified implementation qualities

- `BeaconAr.slnx:2-37` contains exactly the six required top-level folders and exactly 16 projects. The responsibility mapping matches the implementation plan and the reviewed Microsoft solution example.
- `BeaconAr.ServiceDefaults` is the sole current shared project; no speculative HealthChecks or Telemetry projects were added.
- `src/database/BeaconAr.Database.sqlproj` remains the SQL Server schema source and is classified under `02.Modules`; `BeaconAr.DatabaseBootstrap` remains a finite tool under `04.Tools`. No PostgreSQL artifacts or database-engine changes were introduced.
- `dotnet sln BeaconAr.slnx list` succeeded and reported all 16 projects once. Comparing the legacy and new inventories after resolving their different solution-relative roots found no added or missing project.
- `tests/BeaconAr.Architecture.Tests/LayerBoundaryTests.cs:455-554` parses `.slnx` as XML and enforces the exact folder set, exact project-to-folder mapping, uniqueness, solution-item membership, file existence, SQL-project ownership, and legacy-solution absence without relying on order, whitespace, or project-GUID text counts.
- A reviewer rebuild of `BeaconAr.Architecture.Tests` passed with zero warnings/errors, and the final architecture run passed all 26 tests.
- The diff does not change a `.csproj`, `.sqlproj`, package/project reference, database object, generated persistence source, or physical project directory. `git diff --check` passed.
- Remaining `src/BeaconAr.sln` references outside the migration task are confined to historical records with a file-level notice directing readers to root `BeaconAr.slnx`; active build, restore, and database-validation consumers use the canonical path.

## Remaining validation gaps

- The full canonical-solution run cited in the change summary was not repeated after final closeout; the recorded result was 124 passed, 41 environment-gated integration tests skipped, and zero failed.
- SQL Server integration and authenticated HTTP acceptance tests remain unexecuted without `ConnectionStrings__DatabaseConnection` and the required external identity/test environment.
- Solution-wide `paradigm checks run` remains subject to the documented Aspire design-time compilation limitation; the workflow continues to use supported application-project checks.
- No further broad validation was run after reproducing the blocking doctor/validate failure; the final implementer should rerun restore, Release build, all test modules, package checks/audit, supported validation/checks, and strict database validation after correcting the operational commands.

## Final implementer remediation

The original P1 finding above is preserved as the observed pre-fix behavior and is resolved by a focused framework correction:

- `ProjectResolver.ReadSlnx` now forwards only `.csproj` entries to application metadata commands. The SQL project remains a canonical solution member and continues to be validated through `paradigm database validate`; it is no longer misclassified as a managed application project.
- The correction is deterministic by project type and does not inspect output names, suppress `PE1002`, special-case Beacon, remove the SQL project, or reduce CI/start-script coverage.
- A resolver-level test protects mixed `.slnx` classification. An end-to-end test creates a mixed solution with a built managed project and a present malformed DACPAC, then proves both `doctor` and `validate` succeed, report the managed assembly, and emit neither a DACPAC reference nor `PE1002`.

Verification after remediation:

- Focused mixed-solution CLI tests: 2 passed, 0 failed.
- CLI non-packaging-integration suite: 82 passed, 0 failed.
- Freshly built CLI against `BeaconAr.slnx`: `doctor` exit `0` with 15 C# projects; `validate` exit `0`; both reported `success` with no DACPAC/`PE1002` diagnostic.
- `BeaconAr.Architecture.Tests`: 26 passed, 0 failed, 0 skipped.
- The broader CLI invocation without its prepared-package environment ran 95 tests, with 90 passed and 5 expected setup failures identifying the absent `PARADIGM_CLI_INTEGRATION_PACKAGE` / `PARADIGM_CLI_INTEGRATION_EXECUTABLE` variables. This is an integration-harness prerequisite, not a product/test assertion failure caused by the remediation.

Result: the P1 operational blocker is resolved without changing `BeaconAr.slnx`, `start.sh`, the quality workflow, or database-validation coverage. A subsequent reviewer should retain the original finding as history and verify this remediation diff and evidence.

## Guidance evolution re-review

Result: no remaining guidance defect was found in the final guidance delta.

- The six-folder taxonomy and its ownership rules have one authoritative source at `.agents/references/solution-layout.md`; setup, review, and guidance-evolution skills link to it without repeating the long policy.
- The reference explicitly distinguishes solution-folder responsibility from physical directory layout, classifies the schema project as a module/data asset, finite bootstrap and code generation as tools, Web API/AppHost as hosts, and solution items as a curated existing-file list.
- It requires one canonical solution and preserves the mixed-solution boundary: application metadata commands inspect `.csproj` projects, while strict database validation handles `.sqlproj` and verifies solution membership.
- The deterministic skill validator requires every folder name, the key responsibility and mixed-project signals, and links from all three owning skills.
- Skill validation, plugin validation, and Docfx warnings-as-errors all passed. No package, runtime source, example project, solution membership, or deployment behavior changed as part of guidance promotion.

## Re-review

Approved. No actionable defects remain in the Task 1 remediation.

The original P1 is resolved without a coverage regression:

- `src/Paradigm.Enterprise.Cli/Infrastructure/Projects/ProjectResolver.cs:41-50` now filters `.slnx` application-command inputs to `.csproj` entries. This matches the resolver's established `.sln` regex and directory-discovery behavior rather than introducing a Beacon-specific exception or suppressing metadata errors.
- `src/Paradigm.Enterprise.Cli.Tests/CommandLineTests.cs:223-253` proves a mixed `.slnx` retains the managed project and excludes its SQL project from application metadata selection.
- `src/Paradigm.Enterprise.Cli.Tests/ArtifactResolutionTests.cs:77-138` proves the failure mode end to end: a present malformed DACPAC still produces `PE1002` when read directly, while mixed-solution `doctor` and `validate` both succeed, inspect `Sample.dll`, and do not mention the DACPAC or suppress a diagnostic after attempting to read it.
- The SQL Server project remains in `BeaconAr.slnx` under `02.Modules`, the architecture test still enforces the complete 16-project solution inventory, and `.github/workflows/quality.yml:161` still invokes strict database validation explicitly. Application `doctor`/`validate` now correctly report the 15 managed C# projects.
- The README, `start.sh`, and quality workflow retain their canonical `.slnx` commands; they no longer need a weaker project-level fallback.
- `CHANGELOG.md:21` documents the reusable mixed-solution behavior for future maintainers.

Reviewer validation:

- Release build of `Paradigm.Enterprise.Cli.Tests`: passed with 0 warnings and 0 errors.
- Focused resolver and mixed-output regression tests: 2 passed, 0 failed, 0 skipped.
- Freshly built CLI `doctor --project examples/beacon-ar/BeaconAr.slnx --format json`: exit `0`, `status: success`, 15 projects, no DACPAC or `PE1002` diagnostic.
- Freshly built CLI `validate --project examples/beacon-ar/BeaconAr.slnx --format json`: exit `0`, `status: success`, no DACPAC or `PE1002` diagnostic.

The previously recorded environment-gated SQL Server/authenticated HTTP tests and prepared-package CLI integration harness remain broader validation gaps, not defects in this focused remediation.

## Final governed-guidance re-review

Approved. No actionable guidance or validation defect remains.

- `.agents/references/solution-layout.md` is a concise authoritative source for the six responsibility folders, curated solution items, one canonical solution, unchanged physical paths, semantic review invariants, and the managed-application/database split in mixed solutions.
- The reference agrees with the existing database and setup practices: the schema remains under `src/database` and in the application solution, the SQL project is a module/data asset, finite publication/bootstrap executables are tools, and database validation remains independent of application metadata validation.
- `paradigm-setup-project`, `paradigm-review-change`, and `paradigm-evolve-guidance` link to the central reference at the points where solution layout is created, reviewed, or evolved. They add only short routing and task-specific application text; they do not copy the taxonomy or create competing policy.
- Other skills remain consistent: application design delegates new-solution setup to `paradigm-setup-project`, database construction already owns physical database placement and explicit solution inclusion, and Aspire setup delegates solution creation to `paradigm-setup-project` while retaining host/bootstrap operational ownership.
- `.github/scripts/validate-skills.py:48-72` appropriately makes deletion or material erosion of the central policy and removal of the three owning-skill links deterministic failures. These stable phrase/link checks complement review for semantic conflicts rather than pretending to prove all prose correctness.
- The Task 1 change summary and review appendices accurately distinguish the original failure, CLI remediation, durable guidance promotion, and final validation evidence. No historical command was rewritten as if it had run after migration, and no task-specific exception was promoted as universal policy.

Final reviewer checks:

- `python .github/scripts/validate-skills.py`: passed; 11 Paradigm skills validated.
- `python .github/scripts/validate-plugin.py`: passed at plugin version `1.1.0`.
- `git diff --check` over the guidance, validator, task documentation, and changelog: passed.

The previously documented environment-gated integration scenarios remain outside this guidance-only delta and do not block Task 1 approval.
