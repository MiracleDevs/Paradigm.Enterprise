# Beacon AR solution layout change summary

## Delivered

- Added root `BeaconAr.slnx` as the single canonical Beacon solution with exactly `00.SolutionItems`, `01.Shared`, `02.Modules`, `03.Hosts`, `04.Tools`, and `05.Tests`.
- Classified all 16 existing projects by responsibility without moving a project, changing a project reference, adding a package, or altering runtime source. `BeaconAr.ServiceDefaults` is under `01.Shared`; the SQL Server schema remains at `src/database/BeaconAr.Database.sqlproj` under `02.Modules`; AppHost and WebApi are hosts; CodeGenerator and DatabaseBootstrap are tools; all six test projects are under `05.Tests`.
- Curated the checked-in build, restore, tooling, source-control, Aspire, and environment-default configuration under `00.SolutionItems`. Local `.env` secrets and general documentation are not solution items.
- Removed legacy `src/BeaconAr.sln` so the example has one solution source of truth.
- Updated the root README, EF Core Power Tools regeneration script, Aspire start/doctor script, repository quality workflow, and current copyable implementation guidance to use root `BeaconAr.slnx`. Historical change summaries and reviews retain their original commands behind an explicit pre-migration notice.
- Replaced the database-project GUID text count with a semantic XML architecture test. The test asserts the exact folder set and project-to-folder mapping, uniqueness, physical file existence, the curated solution items, SQL Server module ownership, and absence of the retired solution without depending on XML ordering.

## Scope controls

- The task branch started at commit `4f030ea` and is a descendant of `new-example-from-skills`.
- No `.csproj`, `.sqlproj`, package reference, project reference, database object, generated persistence file, source directory, or SQL Server setting changed.
- `git diff --check` passed.
- A repository-scoped legacy-path scan leaves only the migration assertion and historical records whose file-level notice directs readers to root `BeaconAr.slnx`; active scripts, workflow steps, README commands, and current implementation plans have no legacy solution reference.

## Validation

Pre-change baseline against `src/BeaconAr.sln`:

- Restore passed.
- Release build passed with 0 warnings and 0 errors.
- Focused architecture tests passed: 25 succeeded, 0 failed, 0 skipped.

Canonical solution:

- `dotnet sln BeaconAr.slnx list` passed and reported all 16 projects exactly once.
- `dotnet restore BeaconAr.slnx --property:RestoreAdditionalProjectSources=../../artifacts` passed; all projects were up to date.
- `dotnet build BeaconAr.slnx --configuration Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test tests/BeaconAr.Architecture.Tests/BeaconAr.Architecture.Tests.csproj --configuration Release --no-build --no-restore` passed after the final edits: 26 succeeded, 0 failed, 0 skipped.
- An earlier full canonical-solution test run passed: 165 discovered, 124 succeeded, 41 environment-gated SQL Server/authenticated API integration tests skipped because `ConnectionStrings__DatabaseConnection` was not set, and 0 failed. It was not repeated during final closeout.
- `dotnet tool run paradigm packages check --project BeaconAr.slnx` passed.
- `dotnet tool run paradigm packages audit --project BeaconAr.slnx` exited successfully with pre-existing package-update advisories and no reported vulnerability error.
- `dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution BeaconAr.slnx --strict --format json` passed with `status: success` and zero diagnostics.

## Tool limitation

`paradigm doctor --project BeaconAr.slnx` and `paradigm validate --project BeaconAr.slnx` both discovered the complete 16-project solution but exited with `PE1002`: the CLI attempted to read `src/database/bin/Release/BeaconAr.Database.dacpac` as a .NET metadata assembly and reported `Unknown file format`. The same SQL project builds normally and passes strict database validation with zero diagnostics. This is recorded as a CLI `.slnx`/SQL-project metadata-classification limitation; the layout task does not change CLI runtime behavior or remove the database project from the canonical solution to hide it.

## Final remediation

The recorded limitation was fixed in the Paradigm CLI rather than weakening the Beacon solution or its active validation commands:

- `.slnx` application-command resolution now selects listed `.csproj` files and ignores non-C# project types. This matches the existing `.sln` behavior and the documented CLI contract that solution application commands process every listed C# project.
- `BeaconAr.Database.sqlproj` remains in `BeaconAr.slnx` under `02.Modules`; strict database validation remains the independent database-project boundary.
- Added a resolver regression test for a mixed `.slnx` containing `.csproj` and `.sqlproj` entries, plus an end-to-end regression test proving `doctor` and `validate` inspect the managed assembly without reading a present malformed `.dacpac`.
- Updated the framework changelog so future maintainers can discover the mixed-solution behavior.

Remediation evidence:

- Focused CLI regression tests: 2 passed, 0 failed.
- CLI tests excluding packaging integration tests that require prepared package environment variables: 82 passed, 0 failed.
- An unprepared full CLI test invocation ran 95 tests: 90 passed and 5 packaging integration tests failed only because `PARADIGM_CLI_INTEGRATION_PACKAGE` / `PARADIGM_CLI_INTEGRATION_EXECUTABLE` were not set. The two new regression tests passed separately.
- Freshly built CLI `doctor --project BeaconAr.slnx`: exit `0`, `doctor: success`, 15 managed C# projects inspected, and no DACPAC/`PE1002` diagnostic.
- Freshly built CLI `validate --project BeaconAr.slnx`: exit `0`, `validate: success`, and no DACPAC/`PE1002` diagnostic.
- Beacon architecture tests: 26 passed, 0 failed, 0 skipped.

## Governed guidance evolution

Task 1's durable solution-organization lessons are now governed centrally:

- Added `.agents/references/solution-layout.md` as the authoritative concise policy for the six canonical folders, responsibility-based classification, curated solution items, one canonical solution, database/host/tool ownership, physical-path preservation, semantic review assertions, and mixed `.slnx` application/database validation boundaries.
- Linked the reference from `paradigm-setup-project`, `paradigm-review-change`, and `paradigm-evolve-guidance`. The taxonomy remains in one reference instead of being copied into multiple skills.
- Extended `.github/scripts/validate-skills.py` to require the authoritative policy signals and all three workflow links. Future deletion or drift of these requirements now fails deterministic skill validation.

Guidance validation:

- `python .github/scripts/validate-skills.py`: passed; 11 Paradigm skills validated.
- `python .github/scripts/validate-plugin.py`: passed; plugin metadata validated at `1.1.0`.
- `dotnet docfx docs/docfx.json --warningsAsErrors`: passed with 0 warnings and 0 errors.
- `git diff --check`: passed after the guidance and audit-document changes.
