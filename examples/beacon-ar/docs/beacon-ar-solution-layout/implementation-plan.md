# Beacon-ar solution layout implementation plan

## Objective

Replace the legacy `src/BeaconAr.sln` presentation with the canonical root solution `BeaconAr.slnx`. The new solution must expose the standard folders `00.SolutionItems`, `01.Shared`, `02.Modules`, `03.Hosts`, `04.Tools`, and `05.Tests`, while preserving every physical project path, project name, dependency, and the existing SQL Server database project.

This is a solution-organization change. It must not move project directories, create speculative projects, change the database engine, regenerate application source, or alter runtime behavior.

## Evidence and constraints

- The reference `Microsoft.DemoManagementSystem.slnx` uses the six required root folders and direct `<Project Path="..." />` entries.
- Beacon currently uses `src/BeaconAr.sln`. Its core projects are not grouped, its existing folders are named `01.Host`, `02.Tools`, and `03.Tests`, and its SQL Server project is currently presented as a tool.
- Beacon has sixteen solution projects: ten product/infrastructure projects and six test projects.
- `BeaconAr.Architecture.Tests/LayerBoundaryTests.cs` directly reads `src/BeaconAr.sln` and counts a database project GUID. That assertion is coupled to the legacy `.sln` representation and must become a semantic `.slnx` membership/layout assertion.
- Project files and test files remain at their current physical locations beneath `src/` and `tests/`.
- `src/database/BeaconAr.Database.sqlproj` remains the SQL Server schema source of truth. No PostgreSQL project or second database solution is introduced.
- Preserve the dependency direction `Interfaces <- Domain <- Data <- Providers <- WebApi`; solution folders are navigation metadata and must not be treated as dependency boundaries.

## Target solution inventory

Create `examples/beacon-ar/BeaconAr.slnx` with the following classification and root-relative paths:

| Solution folder | Project or item | Rationale |
| --- | --- | --- |
| `00.SolutionItems` | Existing repository-level configuration files used to build, restore, format, test, or run the example | Make the example's governing configuration visible without creating or relocating configuration. Include only files that exist. |
| `01.Shared` | `src/BeaconAr.ServiceDefaults/BeaconAr.ServiceDefaults.csproj` | Shared health, resilience, service-discovery, and telemetry defaults consumed by hosts. |
| `02.Modules` | `src/BeaconAr.Interfaces/BeaconAr.Interfaces.csproj` | Cross-layer public contracts. |
| `02.Modules` | `src/BeaconAr.Domain/BeaconAr.Domain.csproj` | Domain state and behavior. |
| `02.Modules` | `src/BeaconAr.Data/BeaconAr.Data.csproj` | Persistence and query implementation. |
| `02.Modules` | `src/BeaconAr.Providers/BeaconAr.Providers.csproj` | Application use cases and orchestration. |
| `02.Modules` | `src/database/BeaconAr.Database.sqlproj` | SQL Server schema owned by the application modules, not a developer tool. |
| `03.Hosts` | `src/BeaconAr.WebApi/BeaconAr.WebApi.csproj` | HTTP host. |
| `03.Hosts` | `src/BeaconAr.AppHost/BeaconAr.AppHost.csproj` | Aspire orchestration host. |
| `04.Tools` | `src/BeaconAr.CodeGenerator/BeaconAr.CodeGenerator.csproj` | Finite development-time code-generation executable. |
| `04.Tools` | `src/BeaconAr.DatabaseBootstrap/BeaconAr.DatabaseBootstrap.csproj` | Finite database publication/bootstrap executable invoked by orchestration. |
| `05.Tests` | `tests/BeaconAr.Domain.Tests/BeaconAr.Domain.Tests.csproj` | Domain tests. |
| `05.Tests` | `tests/BeaconAr.Architecture.Tests/BeaconAr.Architecture.Tests.csproj` | Architecture and convention tests. |
| `05.Tests` | `tests/BeaconAr.Database.IntegrationTests/BeaconAr.Database.IntegrationTests.csproj` | SQL Server integration tests. |
| `05.Tests` | `tests/BeaconAr.DatabaseBootstrap.Tests/BeaconAr.DatabaseBootstrap.Tests.csproj` | Bootstrap tests. |
| `05.Tests` | `tests/BeaconAr.Providers.Tests/BeaconAr.Providers.Tests.csproj` | Provider tests. |
| `05.Tests` | `tests/BeaconAr.WebApi.Tests/BeaconAr.WebApi.Tests.csproj` | Web API tests. |

Do not add placeholder HealthChecks or Telemetry projects. `BeaconAr.ServiceDefaults` is the current shared project and should remain intact until a separate, evidence-backed extraction is requested.

## Implementation sequence

### 1. Establish a clean baseline

1. Confirm the task branch is based on `new-example-from-skills` and record the pre-change commit.
2. Run the current solution's restore/build and the directly affected architecture tests before changing solution metadata. Record any pre-existing failure instead of attributing it to this task.
3. Inventory existing example-root configuration files and all references to `src/BeaconAr.sln`. Classify references as executable/current guidance or historical evidence before editing them.

### 2. Create the canonical `.slnx`

1. Add `examples/beacon-ar/BeaconAr.slnx` as an XML solution modeled on the reviewed reference.
2. Add exactly the six canonical folder names, including leading and trailing slashes in `.slnx` folder names.
3. Add every project exactly once using the root-relative paths in the target inventory. Preserve project names and physical files.
4. Under `00.SolutionItems`, add the existing example-root configuration files that govern restore, build, formatting, tooling, test execution, or environment defaults. Do not add nonexistent files and do not use this folder as a miscellaneous documentation bucket.
5. Verify the `.slnx` loads and `dotnet sln BeaconAr.slnx list` reports all sixteen projects once.

### 3. Make layout enforcement semantic

Update the direct architecture test in `tests/BeaconAr.Architecture.Tests/LayerBoundaryTests.cs`:

1. Read `BeaconAr.slnx` from the example root and parse it as XML.
2. Assert the exact six canonical folder names.
3. Assert the exact project-to-folder classification in the target inventory, with normalized `/` separators.
4. Assert every expected project occurs once, every referenced project file exists, and no project appears outside a canonical folder.
5. Replace the legacy solution-GUID occurrence count with a semantic assertion that `src/database/BeaconAr.Database.sqlproj` occurs once under `02.Modules`.
6. Retain the independent SQL project identity and database-folder assertions only where they still protect a real Visual Studio or database-project invariant.
7. Assert the legacy `src/BeaconAr.sln` is absent after migration so dual solution sources cannot drift.

The test should compare normalized sets/mappings and provide a useful failure message; it should not depend on XML element order or incidental formatting.

### 4. Update consumers and documentation

1. Change active scripts, workflows, root README commands, validation instructions, and tooling invocations from `src/BeaconAr.sln` to root `BeaconAr.slnx`.
2. Correct relative database validation arguments to use `--solution BeaconAr.slnx` while keeping `--project src/database/BeaconAr.Database.sqlproj`.
3. Update current implementation guidance that users or automation may copy. For historical change summaries, retain the original command as historical evidence and add a concise note identifying `BeaconAr.slnx` as the current canonical solution; do not rewrite a past command so it appears to have run against a file that did not yet exist.
4. Run a repository-scoped reference scan. Any remaining `src/BeaconAr.sln` occurrence must be an explicitly labeled historical statement or the migration test that asserts its absence.
5. Add a short README statement describing the six-folder convention, the root solution path, preservation of physical project paths, and SQL Server ownership.

### 5. Retire the legacy solution

1. Remove `examples/beacon-ar/src/BeaconAr.sln` only after the `.slnx`, direct test, scripts, and active documentation all point to the canonical root solution.
2. Do not retain a compatibility `.sln`, generated copy, redirect, or second database-only solution. One checked-in solution is the source of truth.
3. Review the final diff to confirm no `.csproj`, `.sqlproj`, source directory, project reference, package reference, or database asset moved as a side effect.

### 6. Validate

Run from `examples/beacon-ar` unless a command states otherwise:

```powershell
dotnet sln BeaconAr.slnx list
dotnet restore BeaconAr.slnx --property:RestoreAdditionalProjectSources=../../artifacts
dotnet build BeaconAr.slnx --configuration Release --no-restore
dotnet test --solution BeaconAr.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 1
dotnet tool run paradigm doctor --project BeaconAr.slnx
dotnet tool run paradigm packages check --project BeaconAr.slnx
dotnet tool run paradigm validate --project BeaconAr.slnx
dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution BeaconAr.slnx --strict --format json
```

Also run the focused architecture test immediately after its change. Run `paradigm checks run` against the canonical solution if its AppHost semantic-compilation limitation has been resolved; otherwise run the established supported consuming-project checks and document the exact limitation rather than weakening acceptance criteria silently.

## Acceptance criteria

- `examples/beacon-ar/BeaconAr.slnx` is the only checked-in Beacon solution.
- It contains exactly the six canonical solution folders and all sixteen existing projects exactly once.
- Every project path resolves to its unchanged physical location.
- Shared, module, host, tool, and test classifications match the target inventory.
- Existing governing configuration files are visible in `00.SolutionItems`; no fake or duplicate configuration is created.
- The SQL Server project remains at `src/database/BeaconAr.Database.sqlproj`, is grouped under `02.Modules`, and passes strict database validation.
- The direct architecture test parses `.slnx` semantically and prevents folder, membership, duplicate, missing-file, and legacy-solution regressions.
- Active scripts, workflows, README instructions, and tooling use `BeaconAr.slnx` from the example root.
- Historical evidence is clearly labeled where it still mentions `src/BeaconAr.sln`.
- Restore, Release build, all test modules, Paradigm doctor/package/validation checks, and strict database validation pass or have a precisely documented pre-existing/environment-gated result.
- No project/source directory, project/package reference, generated file, database object, engine, or runtime behavior changes in this task.

## Reviewer checklist

- Compare the `.slnx` structure with the reviewed Microsoft reference, not with the legacy Beacon folder numbering.
- Verify classification by responsibility: shared defaults, application modules/schema, deployable hosts, finite tools, tests.
- Confirm `BeaconAr.DatabaseBootstrap` is not mistaken for the database schema project and `BeaconAr.Database` is not presented as a tool.
- Confirm the test asserts semantic XML mappings rather than `.sln` GUID counts or line formatting.
- Search for stale active `src/BeaconAr.sln` references and ambiguous instructions.
- Confirm SQL Server remains the selected engine and no PostgreSQL artifacts or dependencies were introduced.
- Confirm the change is limited to solution metadata, direct enforcement tests, consumers, and documentation.

