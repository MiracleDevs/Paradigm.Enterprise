# Create a solution from the Web API template

The Paradigm Web API template creates the project boundaries, Enterprise package
references, database-first scaffolding, interface generator, and host composition
root. It uses NuGet Central Package Management from the first restore.

Install the .NET 10 SDK before creating a solution.

## Install the template

The [Web API template](https://github.com/MiracleDevs/Paradigm.Enterprise/tree/main/templates/web-api)
is packaged as a native .NET template. Build and install it from a reviewed
Paradigm.Enterprise checkout:

```powershell
dotnet pack templates/web-api/Paradigm.Enterprise.Templates.WebApi.csproj --configuration Release --output templates/web-api/artifacts/packages
$version = ([xml](Get-Content build/Paradigm.Version.props)).Project.PropertyGroup.ParadigmEnterpriseVersion
dotnet new install "templates/web-api/artifacts/packages/Paradigm.Enterprise.Templates.WebApi.$version.nupkg" --force
```

Maintainer packaging commands require PowerShell 7. Generated applications require
the .NET SDK without a PowerShell dependency.
Maintainers can also run `bash templates/web-api/build/build-template.sh` to build
and install the package for the current framework version.

## Create the solution

Choose a solution name that represents the product or bounded context. The
template appends layer names to that root.

```powershell
dotnet new paradigm-webapi --name Company.Product --output ../Company.Product --dry-run
dotnet new paradigm-webapi --name Company.Product --output ../Company.Product
dotnet restore ../Company.Product/Company.Product.slnx
dotnet build ../Company.Product/Company.Product.slnx
```

Open the generated `Company.Product.slnx` in Visual Studio. The solution-root
`Directory.Packages.props` contains all package versions and appears under
`00.SolutionItems`; individual projects use versionless package references.
Change versions in the central file, including its `ParadigmEnterpriseVersion`
property when upgrading the Paradigm package set.

In Development, open `/scalar/v1` for the interactive API reference. Scalar uses
the OpenAPI document at `/openapi/v1.json`; `/swagger/v1/swagger.json` remains
available for client generation. These documentation routes are disabled outside
Development.

A new template is a scaffold, not a production-ready host. Add a connection string
through user secrets or environment variables, review CORS and health output,
select authentication and authorization, and verify middleware ordering before
deployment.

Continue with [Understand the generated solution](generated-solution.md), then
[Scaffold from a database](database-first.md).
