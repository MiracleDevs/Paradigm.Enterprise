# Example catalog reorganization implementation plan

## Objective

Replace the repository's single, generically named `example/` directory with an extensible `examples/` catalog. Move the existing product inventory CRUD compatibility sample to `examples/inventory-crud/`, give its solution, projects, namespaces, and documentation an `InventoryCrud` identity, update every repository-owned reference to the new location, and keep the sample independently restorable, buildable, testable, and valid against the locally packed Paradigm packages.

The current baseline is healthy: `dotnet test example/ExampleApp.sln --configuration Release --no-restore` passes both existing tests. The implementation must preserve this behavior while changing names and paths.

## Scope

This task includes only the catalog reorganization and rebranding of the existing sample. It does not implement Beacon AR, add SQL Server persistence, modernize the sample's business behavior, change its dependency versions, or add a second example. Those are separate tasks that can use the new catalog structure.

## Target layout

```text
examples/
|-- README.md
`-- inventory-crud/
    |-- README.md
    |-- TUTORIAL.md
    |-- Directory.Build.props
    |-- InventoryCrud.sln
    |-- InventoryCrud.Interfaces/
    |-- InventoryCrud.Domain/
    |-- InventoryCrud.Data/
    |-- InventoryCrud.Providers/
    |-- InventoryCrud.WebApi/
    |-- InventoryCrud.Tests/
    `-- docs/
        `-- example-catalog-reorganization/
            |-- implementation-plan.md
            |-- change-summary.md
            `-- review-feedback.md
```

The task workflow may add or revise the change summary and review feedback beside this plan. The move must preserve this task documentation when the rest of the sample is merged into `examples/inventory-crud/`.

## Implementation steps

### 1. Move the sample into the catalog

1. Use Git-aware moves so history remains traceable: move the contents of `example/` into the already-created `examples/inventory-crud/` directory without overwriting `docs/example-catalog-reorganization/`.
2. Remove the empty `example/` directory after all tracked content has moved.
3. Add `examples/README.md` as the catalog landing page. List the Inventory CRUD sample by name, explain that each example is self-contained, and link to `inventory-crud/README.md`.
4. Keep the sample's layer dependency direction unchanged: `Interfaces <- Domain <- Data <- Providers <- WebApi`; tests continue to reference the domain project directly.

### 2. Rebrand the moved sample

1. Rename `ExampleApp.sln` to `InventoryCrud.sln`.
2. Rename every `ExampleApp.<Layer>/` directory and its `.csproj` to `InventoryCrud.<Layer>/InventoryCrud.<Layer>.csproj` for Interfaces, Domain, Data, Providers, WebApi, and Tests.
3. Update the solution's display names and relative project paths. Preserve the existing project GUIDs because this is an identity-preserving rename, not creation of new projects.
4. Update all project references to the renamed sibling project files while preserving the existing dependency graph and package references.
5. Replace the `ExampleApp.*` namespaces and corresponding `using` directives with `InventoryCrud.*`. Rename the Web API `.http` file and its host variable consistently. Review launch settings, resource namespaces, and any assembly-derived configuration for remaining old identifiers.
6. Do not rename business concepts such as `Inventory`, `Product`, or `InventoryDbContext`; they already express the sample's bounded context.
7. Run a case-sensitive repository search for `ExampleApp` after the rename. No live sample identifier should remain. Update the current changelog wording to the new sample name rather than leaving a stale brand reference.

### 3. Repair relocation-sensitive paths and sample documentation

1. Change `examples/inventory-crud/Directory.Build.props` to import `../../build/Paradigm.Version.props`. This is required for `$(ParadigmEnterpriseVersion)` to continue resolving from the sample projects after the directory becomes two levels below the repository root.
2. Update commands in the sample README to use `examples/inventory-crud/InventoryCrud.sln`.
3. Update links from the sample README and tutorial to repository documentation by adding the extra parent traversal (`../../docs/...`). Verify every relative Markdown link from its new file location.
4. Retitle and describe the sample consistently as the **Inventory CRUD compatibility example**. Correct the existing stale claim that it is pinned to historical `1.0.0`: the projects consume `$(ParadigmEnterpriseVersion)` from `build/Paradigm.Version.props` and therefore track the repository's current package line. Retain the warning that the in-memory database and small manually composed API are instructional/compatibility coverage, not production architecture.
5. Keep the catalog README concise and route readers to the sample README for build instructions and limitations.

### 4. Update repository-level references

Update all consumers atomically so no committed command points to the removed directory or old solution name:

- `README.md`: link to the catalog and/or named Inventory CRUD README and use the new display name.
- `docs/tests.md`: replace sample test and CLI-check commands with `examples/inventory-crud/InventoryCrud.sln`.
- `build/quality.sh`: update restore, build, test, package check/audit, validation, and semantic-check paths.
- `.github/workflows/quality.yml`: update every sample solution command to the new path.
- `.github/workflows/documentation.yml`: change path filters from `example/**` to `examples/**` and update restore/build commands.
- `CHANGELOG.md`: replace the current-version `ExampleApp` wording with `Inventory CRUD example` so the documented name matches the repository.

After editing, search tracked files (excluding `.git`, `bin`, and `obj`) for `example/`, `example\\`, `ExampleApp`, and the old `example/**` workflow filter. Classify any match rather than blindly replacing ordinary prose that uses the singular word "example." No operational reference to the old location or name may remain.

### 5. Validate the reorganized catalog

Run validation from the repository root in this order:

1. Confirm `git status --short` shows renames where Git can detect them, the new catalog README, task documentation, and intentional reference edits only.
2. Pack the current framework exactly as CI does, verify the packages, and restore the renamed sample using the packed artifacts as an additional source:

   ```powershell
   dotnet restore src/Paradigm.Enterprise.slnx
   dotnet build src/Paradigm.Enterprise.slnx --configuration Release --no-restore
   dotnet pack src/Paradigm.Enterprise.slnx --configuration Release --no-build --output artifacts
   pwsh build/verify-packages.ps1 -ArtifactsDirectory artifacts
   dotnet restore examples/inventory-crud/InventoryCrud.sln --property:RestoreAdditionalProjectSources="$PWD/artifacts"
   ```

3. Build and test the renamed solution without another restore:

   ```powershell
   dotnet build examples/inventory-crud/InventoryCrud.sln --configuration Release --no-restore
   dotnet test examples/inventory-crud/InventoryCrud.sln --configuration Release --no-build
   ```

   The two existing product behavior tests must still pass.

4. Restore the repository-local tool manifest and run the same deterministic sample checks used by CI:

   ```powershell
   dotnet tool restore
   dotnet tool run paradigm packages check --project examples/inventory-crud/InventoryCrud.sln
   dotnet tool run paradigm packages audit --project examples/inventory-crud/InventoryCrud.sln
   dotnet tool run paradigm validate --project examples/inventory-crud/InventoryCrud.sln
   dotnet tool run paradigm checks run --project examples/inventory-crud/InventoryCrud.sln
   ```

5. Run `dotnet docfx docs/docfx.json --warningsAsErrors` to catch broken repository documentation links.
6. Run `bash build/quality.sh` when Bash is available to prove the local full-quality entry point follows the relocated sample successfully.
7. Repeat the stale-reference search after builds while excluding generated `bin/` and `obj/` directories. Verify that `example/` no longer exists and that `examples/inventory-crud/InventoryCrud.sln` is the only catalog solution introduced by this task.

## Review checklist

- The reorganization contains no functional API, persistence, package, or test-behavior changes.
- Project references still enforce the original layer direction, and every package still uses the centralized Paradigm version.
- The relocated `Directory.Build.props` resolves the root version file.
- All project, solution, namespace, HTTP-client, documentation, workflow, and quality-script names agree on `InventoryCrud` and `examples/inventory-crud`.
- Git history is preserved through moves where practical, with no duplicated old sample left behind.
- Root and sample documentation accurately describe the current package line and the sample's in-memory/compatibility limitations.
- CI path filters include the entire `examples/**` catalog so future examples trigger documentation validation.
- Restore, build, tests, Paradigm package checks/audit/validate/checks, DocFX, and the full quality script pass from the new location.

## Rollback boundary

If the renamed solution cannot restore or build, first inspect the centralized props import, solution project paths, and project references; those are the only expected relocation-sensitive build inputs. Revert only the catalog-reorganization changes as a unit rather than adding compatibility shims, duplicate solutions, forwarding projects, or an `example/` junction. The repository should have one authoritative path for this sample.
