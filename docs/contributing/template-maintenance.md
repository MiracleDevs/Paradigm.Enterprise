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
state. Packaging inherits the framework version; release synchronization of the
template's dependency versions is not yet automated.

## Validate generation

Restore and build `templates/web-api/src/Paradigm.Web.ApiTemplate.slnx` before
packaging. Install the resulting `.nupkg` with `dotnet new install`, inspect a
`dotnet new paradigm-webapi --dry-run`, and generate a fresh named solution.
Verify that exactly one `Directory.Packages.props` exists at its root, all project
references resolve, every package reference gets a central version, and the
canonical solution includes the central file under `00.SolutionItems`.

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
