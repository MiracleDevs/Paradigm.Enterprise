# Example catalog reorganization change summary

## Outcome

The repository's compatibility sample now lives in an extensible `examples/` catalog as the **Inventory CRUD compatibility example**. The solution, project directories, project files, namespaces, HTTP-client file and variable, and in-memory database name use the `InventoryCrud` identity. The original solution project GUIDs and layer dependency direction are preserved.

The old `example/` tree was removed. `examples/inventory-crud/InventoryCrud.sln` is the only solution in the catalog.

## High-level changes

- Added `examples/README.md` as the catalog landing page and linked it from the repository README.
- Moved the existing sample into `examples/inventory-crud/` with Git-aware file moves, preserving the task documentation already present under `docs/example-catalog-reorganization/`.
- Renamed all six projects and their namespaces from `ExampleApp.*` to `InventoryCrud.*` while retaining the dependency graph `Interfaces <- Domain <- Data <- Providers <- WebApi`; tests still reference Domain directly.
- Updated the relocated `Directory.Build.props` import to `../../build/Paradigm.Version.props`. MSBuild resolves `ParadigmEnterpriseVersion` to `1.1.0` from the new location.
- Updated the sample README and tutorial to use the new path, repair repository-documentation links, describe the current centralized package line accurately, and retain the in-memory/manual-composition production warning.
- Updated repository documentation, CI workflows, documentation path filters, the local quality script, and the changelog to use `examples/inventory-crud/InventoryCrud.sln` and the Inventory CRUD name.
- Normalized `build/quality.sh` to LF line endings so its edited path lines do not leave a mixed-line-ending shell script.

## Validation

The following checks passed from the repository root:

- `git diff --check HEAD`
- `dotnet sln examples/inventory-crud/InventoryCrud.sln list` (six renamed projects)
- `dotnet restore src/Paradigm.Enterprise.slnx`
- `dotnet build src/Paradigm.Enterprise.slnx --configuration Release --no-restore` (zero warnings and errors)
- `dotnet pack src/Paradigm.Enterprise.slnx --configuration Release --no-build --output artifacts`
- `build/verify-packages.ps1 -ArtifactsDirectory artifacts` (13 package readmes validated)
- `dotnet restore examples/inventory-crud/InventoryCrud.sln` with the packed artifacts as an additional source
- `dotnet build examples/inventory-crud/InventoryCrud.sln --configuration Release --no-restore` (zero warnings and errors)
- `dotnet test examples/inventory-crud/InventoryCrud.sln --configuration Release --no-build` (2 passed, 0 failed)
- Paradigm `packages check`, `packages audit`, `validate`, and `checks run` against the renamed solution, invoked through the repository CLI project because the local tool manifest does not define a `paradigm` command
- `dotnet docfx docs/docfx.json --warningsAsErrors` (zero warnings and errors)
- Direct existence checks for every relative Markdown link in the catalog and sample documentation
- Post-build stale-reference search excluding generated `bin/` and `obj/` directories

The package audit completed successfully with warnings for pre-existing outdated versions of MSTest, Microsoft.EntityFrameworkCore.InMemory, Microsoft.NET.Test.Sdk, Microsoft.OpenApi, and Swashbuckle.AspNetCore. Dependency changes were explicitly outside this task's scope.

## Remaining environment gap

The full `bash build/quality.sh` entry point could not run in this Windows workspace. The available Bash is a WSL environment that does not expose the installed Windows .NET SDK under the required `dotnet` command, so the script exits at its prerequisite check. Its relocated sample commands were exercised individually as listed above. CI's Bash environment remains the authoritative full entry-point run.

The post-build stale-reference matches occur only in this change record and `implementation-plan.md`, where they document the before-state or acceptance criteria; no live source, solution, project, script, workflow, or documentation command references the old identity or path.
