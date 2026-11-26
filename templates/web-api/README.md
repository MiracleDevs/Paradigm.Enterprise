# Paradigm Web API template

The Web API template lives in `templates/web-api` in Paradigm.Enterprise. It was
imported from MiracleDevs/Paradigm.Web.ApiTemplate at commit
`2039145fec1d6a59712c6195fa561b4b349c93b7` with its original [MIT license](LICENSE).

The template uses the native .NET template engine. It creates the solution,
projects, and a solution-root `Directory.Packages.props` together. This replaces
the legacy Visual Studio project-group ZIP; open the generated `.slnx` in Visual
Studio after creating the solution.

| Path | Contents |
| --- | --- |
| `src/` | Buildable template solution and its central package versions |
| `src/.template.config/` | Native .NET template metadata and icon |
| `Paradigm.Enterprise.Templates.WebApi.csproj` | NuGet template packaging project |
| `build/` | Package build and local installation scripts |

## Build and install

Use the .NET 10 SDK and PowerShell 7 for maintainer commands. From the
Paradigm.Enterprise repository root:

```powershell
dotnet pack templates/web-api/Paradigm.Enterprise.Templates.WebApi.csproj --configuration Release --output templates/web-api/artifacts/packages
$version = ([xml](Get-Content build/Paradigm.Version.props)).Project.PropertyGroup.ParadigmEnterpriseVersion
dotnet new install "templates/web-api/artifacts/packages/Paradigm.Enterprise.Templates.WebApi.$version.nupkg" --force
```

The package version follows `build/Paradigm.Version.props`. Packaging checks that
the template's central versions are aligned before creating the package.

The Bash wrapper builds and installs the package for the current version:

```bash
bash templates/web-api/build/build-template.sh
```

Use `--no-install` to build only, or `--install-only` to install an existing package.
For template development, installation directly from `templates/web-api/src` is
also supported by `dotnet new install`.

## Create a solution

Review the file inventory before creating the solution:

```powershell
dotnet new paradigm-webapi --name Company.Product --output ../Company.Product --dry-run
dotnet new paradigm-webapi --name Company.Product --output ../Company.Product
dotnet restore ../Company.Product/Company.Product.slnx
dotnet build ../Company.Product/Company.Product.slnx
```

The generated solution has seven projects, the six canonical solution folders,
and `Directory.Packages.props` listed under `00.SolutionItems`. The source template
uses concrete names; the template engine replaces them in paths and file contents.

## Central Package Management

All package versions are owned by the generated solution's root
`Directory.Packages.props`. Project files contain versionless `PackageReference`
items and preserve their analyzer and asset metadata. Update the central file to
change package versions. Its `ParadigmEnterpriseVersion` property keeps all
Paradigm package versions aligned.

The application targets .NET 10; the interface generator targets .NET Standard
2.0. The template source can
also be restored and built directly through `src/Paradigm.Web.ApiTemplate.slnx`.

## Release maintenance

After adding the new release's changelog entry, run
`pwsh build/set-version.ps1 -Version <new-version>`. It updates framework and
plugin metadata and synchronizes the template's central versions. The existing
`bash build/increment.version.sh <old-version> <new-version>` entry point uses the
same command.

For dependency changes without a release-version change:

```powershell
pwsh build/templates/update-templates.ps1
pwsh build/templates/update-templates.ps1 -Check
```

Synchronization copies shared package versions from `src/Directory.Packages.props`,
aligns all Paradigm references through `ParadigmEnterpriseVersion`, and preserves
template-only package pins. To validate against packages built from this checkout:

```powershell
dotnet pack src/Paradigm.Enterprise.slnx --configuration Release --output artifacts
pwsh build/templates/verify-templates.ps1 -FrameworkPackages artifacts
```

Validation builds the source and a freshly generated solution, checks package
contents and CPM discovery, verifies exact local framework package hashes, and
checks the Development and Production documentation routes. It uses an isolated
template hive and package cache. Add `-KeepWorkDirectory` to retain a successful
run's generated solution; failed runs retain their work directory automatically.

## API documentation

In Development, Scalar serves the API reference at `/scalar/v1`, using the
OpenAPI document at `/openapi/v1.json`. Swashbuckle also exposes
`/swagger/v1/swagger.json` for client generation. These documentation endpoints
are enabled only in Development.
