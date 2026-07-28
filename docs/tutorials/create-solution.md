# Create a solution from the Visual Studio template

The Visual Studio template creates the expected project boundaries, references the Enterprise packages, and includes the database-first and code-generation tools used by the team. Starting from the template is safer than assembling the layers from memory.

The template targets Visual Studio 2022 or later and .NET 10. Install the current .NET 10 SDK and make sure it is visible to Visual Studio before creating a solution.

## Install the template

Download or build `Paradigm.Web.ApiTemplate.zip` from the [template repository](https://github.com/MiracleDevs/Paradigm.Web.ApiTemplate). Close Visual Studio before installing or replacing a project template. Copy the archive to the user project-template directory for the installed Visual Studio version, then restart Visual Studio.

```text
%USERPROFILE%\Documents\Visual Studio 2022\Templates\ProjectTemplates
```

Template maintainers can use the repository's installation script instead. Application developers should normally install a reviewed archive rather than build a template from an arbitrary branch.

## Create the solution

Open the Create a new project dialog, search for `Paradigm WebApi`, and choose the project-group template. Select a neutral solution name that represents the product or bounded context rather than a technical layer. The template appends layer names to that root.

After creation, restore and build before adding features:

```powershell
dotnet restore
dotnet build
```

A new template is a scaffold, not a production-ready host. Add a connection string through user secrets or environment variables, review CORS and health output, select authentication and authorization, and verify middleware ordering before deployment.

Continue with [Understand the generated solution](generated-solution.md), then [Scaffold from a database](database-first.md).

## If the template is unavailable

Create separate Interfaces, Domain, Data, Providers, and WebApi projects with the dependency direction shown in [Architecture](../architecture.md). This manual path is useful for an existing solution, but it does not reproduce the analyzer, T4, and generator setup automatically.
