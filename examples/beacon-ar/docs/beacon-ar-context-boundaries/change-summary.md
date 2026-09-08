# Beacon AR context boundaries — change summary

## Result

Beacon AR now has four capability-owned SQL Server persistence models—Access, MasterData, Operations, and Sales—with no Receivables or Reporting application roots. All 17 tables and 14 views have one generated CLR/context owner, and cross-context foreign keys remain SQL Server constraints represented by scalar IDs.

## Implemented changes

- Replaced `ReceivablesDbContext` with `AccessDbContext`, `MasterDataDbContext`, `OperationsDbContext`, and `SalesDbContext`; registered all four against the same scoped `DatabaseConnection` provider and retained one Unit of Work.
- Split the generated Domain types and Data repositories by capability, co-located the 12 behavior partials with their generated halves, retained only same-context navigations, and moved dashboard ownership from Reporting to Operations.
- Replaced the single EFPT configuration with four credential-free, disjoint configs covering exactly 17 tables and 14 views. The existing official-derived T4 templates remain generic and unchanged.
- Reworked persistence regeneration around an exact 35-file manifest. It preserves co-located partials, restores every boundary after a failure, validates ownership and selection parity, and requires a byte-stable second run.
- Added 14 typed SQL Server routines for get-or-create, locking, sequence allocation, Sales reference reads, and MasterData cross-capability reference checks. The database project owns one routine per file.
- Removed handwritten SQL query/command text from production repositories. Bounded CRUD/read operations use EF; concurrency, cross-capability, reporting, and performance-sensitive operations use typed stored-procedure wrappers.
- Changed stored-procedure mapper generation to emit atomic capability-owned bundles and registrars derived from namespaces. Two consecutive generations produced the same 36 files and hashes.
- Updated `PersistenceSession` to clear all four change trackers after rollback, updated host/test registrations and namespaces, and added architecture regressions for ownership, raw SQL, generated metadata, navigations, and mapper placement.
- Updated the Beacon README, the database-first tutorial, setup/repository/review guidance, and shared coding guidance. Earlier one-context documents are explicitly marked historical rather than silently presenting superseded instructions as current.

## Verification

- Release solution build: passed.
- Architecture tests: 40 passed, including the 26 adapted pre-existing layer-boundary tests and exact generated-file, navigation, routine, mapper, determinism, and failure-recovery manifests.
- Domain, Provider, Web API, and integration-test project builds: passed.
- SQL Server database project build: passed.
- Strict Paradigm database validation: passed.
- Persistence failure-recovery and secret-redaction fixtures: passed through the same recoverable workflow used by real generation. Recovery preserves the exact 35-file generated manifest and all handwritten partials, while removing only recognized outputs introduced by the failed run.
- Mapper generation second-run byte stability: passed for the exact 36-file mapper manifest.

Final verification completed as follows:

- `dotnet restore BeaconAr.slnx --locked-mode`: passed.
- Release solution build: passed with zero warnings/errors.
- A disposable SQL Server 2022 database was created and published through the governed bootstrap/DACPAC path; publish, readiness, and bootstrap probes passed.
- Live database integration tests: 24 passed, zero skipped or failed. This covers exact routine parameter/result metadata, routine execution, concurrency and locking, and cross-context enlistment/rollback behavior.
- Live Web API integration tests: 19 passed, zero skipped or failed.
- The complete EFPT workflow ran against the disposable database. Both generation passes, the Release build, generated-byte stability, and handwritten-partial preservation passed.
- Full solution tests with the disposable connection: 181 total, 181 passed, zero skipped or failed.
- Shared scoped connection identity and four-tracker cleanup test: passed without opening a database connection.
- `paradigm packages check`: passed; `packages audit` exited successfully with pre-existing available-update warnings and no reported vulnerability.
- Built-in checks for Domain, Data, and Providers: passed. Whole-solution checks from both the pinned installed CLI and repository-source CLI stop on Aspire's generated `Projects` symbol during semantic compilation, so the consuming application layers were checked directly.
- Strict database validation and SQL Server database project build: passed.
- The 14 new SQL routine sources and owning SQL project are captured in `routine-validation.sha256` as a secret-free 15-entry SHA-256 manifest.
- `dotnet tool run aspire restore`: passed.
- The example's pinned installed CLI package (`1.1.0`) makes `doctor` and whole-solution `validate` read the freshly built DACPAC as a managed assembly (`PE1002`); its older validation also folds the `int` audit-actor/context type into the deliberate `long` Operations entity IDs (`PE3002`). The CLI built from this repository's current source passes both `doctor` and whole-solution `validate`, correctly sees 15 managed projects, ignores the DACPAC as an assembly, and reports the Operations IDs as `long`. The installed-package lag is recorded in `decisions.md`.
- `bash ./start.sh doctor` could not see the installed Windows .NET SDK from the available Bash environment; the native PowerShell restore/build/CLI checks above used .NET 10 successfully.
- The exact Task 3 containers and images used for validation were removed after each run. The pre-existing persistent Aspire resource `sqlserver-a91e0506` and volume `beaconar.apphost-a91e05069e-sqlserver-data`, both created before this task's validation environment, were inspected and deliberately left untouched. No connection secret was persisted in source or documentation.

## Reviewer remediation cycle 1

- Corrected the live SQL metadata assertions to the published schema widths/types and added the complete `GetDashboardSummary` result-set contract.
- Restored all 26 established layer-boundary tests, adapting only their expected capability paths and context ownership, while retaining the new context-boundary coverage.
- Replaced partial/representative persistence assertions with exact object, file, interface, navigation, routine, mapper, registrar, determinism, and recovery manifests.
- Made the regeneration failure fixture cross a real generation boundary through the production recovery wrapper, simulate a deterministic failure after the first capability, and prove full protected-hash and recognized-output parity after rollback.
- Re-ran the governed publish, full generation, live integration suites, architecture suite, Release build, and full solution test suite successfully before removing the disposable environment.

## Reviewer remediation cycle 2

- Replaced partial discovery with an exact protected handwritten manifest containing all 12 entity behavior partials plus `MasterDataDbContext.Relationships.cs`. Backup and recovery now cover the 35 generated files and all 13 handwritten files.
- Extended recovery to snapshot and restore pre-existing recognized generator/byproduct files and their hashes, while deleting only recognized outputs introduced by the failed run.
- Added a fast fixture that mutates a generated context, a protected behavior partial, a generated-marker file, and the recognized EFPT readme, then proves complete path/hash parity after production recovery.
- Added `-TestLiveFailureRecovery`. It executes the actual Access EFPT configuration successfully against a published SQL Server database, injects failure before MasterData, routes through the production catch/recovery path, and proves generated, protected, and recognized-output path/hash parity.
- The fast and live recovery paths produced the same state fingerprint: `A9A8494DB003348634A3DCFEDA0230D9BBAE2999A0CC9F3860C9B439DEB39DDE`.
- Re-ran the governed DACPAC publish/probe, live recovery, Architecture tests (40/40), database integration tests (24/24), Web API integration tests (19/19), and Release build (zero warnings/errors). Secret-free commands, hashes, resource names, counts, and cleanup evidence are recorded in `live-validation.md`.

## Deliberately deferred

- Task 6 owns the reusable CLI semantic diagnostic for handwritten SQL in production repositories. This task adds the skill policy and a Beacon architecture regression but does not preempt that CLI implementation.
- Entity-validation relocation, system-enum movement, manual DTO review, Provider CRUD simplification, and controller query DTOs remain the later Domain/Provider/API tasks.
