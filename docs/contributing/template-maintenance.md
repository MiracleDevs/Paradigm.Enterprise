# Maintain the Web API template

The [Web API template](https://github.com/MiracleDevs/Paradigm.Enterprise/tree/main/templates/web-api)
lives in `templates/web-api`. Its canonical source is a normal buildable solution
under `src`, with central package versions in `src/Directory.Packages.props` and
native template metadata in `src/.template.config/template.json`.

The native template engine creates solution-root files as well as projects. This
allows `Directory.Packages.props` to be generated before the first restore.
Project files contain versionless package references; update package versions in
the central file and keep every Paradigm reference tied to its
`ParadigmEnterpriseVersion` property.

## Build the package

From the Paradigm.Enterprise repository root:

```powershell
dotnet pack templates/web-api/Paradigm.Enterprise.Templates.WebApi.csproj --configuration Release --output templates/web-api/artifacts/packages
```

The Bash wrapper supports `--no-install` for packaging and `--install-only` for
installation of an existing package. The old synchronization step and Visual
Studio ZIP installation have been replaced by native NuGet template packaging.
The package includes the source template and excludes build artifacts and IDE
state. Packaging inherits the framework version and rejects central-version drift.

## Synchronize release versions

After writing the release notes in `CHANGELOG.md`, run
`pwsh build/set-version.ps1 -Version <new-version>` using PowerShell 7. It updates
the shared release version, plugin metadata, and template central versions. A
failed synchronization restores the original version metadata.

For shared dependency changes, run `pwsh build/templates/update-templates.ps1`. Its `-Check`
mode detects drift without writing files. The synchronizer takes the release
version from `build/Paradigm.Version.props` and shared package versions from
`src/Directory.Packages.props`. Template-only pins remain owned by the template.
Shared entries must have unambiguous, unconditional literal versions for automatic
synchronization. All template Paradigm references use `$(ParadigmEnterpriseVersion)`.

`build/verify-version.ps1`, direct template packaging, PR quality checks, and tagged
releases enforce synchronization. The release workflow includes the validated
template package in the same NuGet artifact as the framework packages.

## Validate generation

Build and pack the framework, then run the maintained validation pipeline:

```powershell
dotnet pack src/Paradigm.Enterprise.slnx --configuration Release --output artifacts
pwsh build/templates/verify-templates.ps1 -FrameworkPackages artifacts
pwsh build/templates/tests/template-maintenance.tests.ps1
```

The pipeline builds the authoring source and installs the packed native template
into an isolated hive. It exercises a dry run, generates a named solution, checks
the package payload, root CPM file, versionless references, canonical folders and
solution items, and builds the generated solution. Each resolved Paradigm package
must match the version and exact package bytes built by the current checkout.
The generated API must serve Scalar and OpenAPI in Development and disable its
documentation routes in Production.

The generated app restores into a fresh cache with Paradigm packages restricted
to the supplied local feed. This prevents a previously cached package of the same
version from hiding a compatibility failure. Successful runs remove their scratch
directory; `-KeepWorkDirectory` retains it, and failures always preserve it.

Restore and build the generated solution as well as the authoring source. When
checking an unpublished framework version, restore against packages packed from
the same framework checkout. Compare resolved package versions before and after
packaging to verify CPM discovery and preserve dependency compatibility.

Add a local connection string, reverse engineer a minimal database selection,
build generated interfaces, and run the host. Check Scalar and OpenAPI routes in
Development and their absence in Production. Install the Paradigm CLI through a
local manifest and exercise generation modes after their input assemblies exist.

Review host defaults before publishing a template version. CORS, Scalar/OpenAPI
documentation, authentication scaffolding, health output, JSON reflection
settings, and middleware order are application policy.
