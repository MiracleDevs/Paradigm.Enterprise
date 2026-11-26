# Create a solution from the Web API template

The Paradigm Web API template creates the project boundaries, Enterprise package
references, database-first scaffolding, interface generator, and host composition
root. It uses NuGet Central Package Management from the first restore.

Install the .NET 10 SDK before creating a solution.

## Install the template

The [Web API template](https://github.com/MiracleDevs/Paradigm.Enterprise/tree/main/templates/web-api)
is distributed as the native .NET template package
`Paradigm.Enterprise.Templates.WebApi`. Choose a published package version and
replace `<released-version>` below with that version:

```powershell
$templateVersion = '<released-version>'
dotnet new install "Paradigm.Enterprise.Templates.WebApi@$templateVersion"
```

Installing the published package requires the .NET SDK and access to the package
feed. No framework checkout or local packaging is needed. Pinning the version
makes the selected template reproducible. See
[dotnet new install](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-install)
for package-source options.

For an unreleased version, or before the first template package is published,
follow the [maintainer build and installation instructions](https://github.com/MiracleDevs/Paradigm.Enterprise/blob/main/templates/web-api/README.md#maintainers-build-and-install-from-source)
in a reviewed Paradigm.Enterprise checkout. Those commands require PowerShell 7;
the build/install wrappers also require Bash.

## Create the solution

Choose a solution name that represents the product or bounded context. The
template appends layer names to that root. Run these commands from the application repository root; the solution and layer projects are created together under `src/api`.

```powershell
dotnet new paradigm-webapi --name Company.Product --output ./src/api --dry-run
dotnet new paradigm-webapi --name Company.Product --output ./src/api
dotnet restore ./src/api/Company.Product.slnx
dotnet build ./src/api/Company.Product.slnx
```

Open the generated `src/api/Company.Product.slnx` in Visual Studio. The API solution-root
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
