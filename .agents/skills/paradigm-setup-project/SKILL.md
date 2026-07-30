---
name: paradigm-setup-project
description: Set up or repair a Paradigm.Enterprise .NET solution, including layer projects, aligned NuGet packages, the Paradigm CLI, database-first generation, and prerequisite checks. Use for new applications, framework adoption, package upgrades, restore/build failures, or generated-code setup.
---

# Set up a Paradigm project

## Establish the baseline

1. Inspect the solution, target frameworks, project references, `Directory.Packages.props`, EF configuration, and generated-file headers. Do not assume the template layout.
2. Prefer .NET 10 and the reviewed Paradigm Web API template for a new solution. For an existing solution, preserve its naming and introduce only missing boundaries.
3. Keep one version across every `Paradigm.Enterprise.*` package. Reference a package from the project that uses its types; do not rely on accidental transitive access.
4. Preserve the dependency direction `Interfaces <- Domain <- Data <- Providers <- WebApi`. Database adapters depend on Data; infrastructure adapters expose focused contracts to Providers.
5. Restore and build before adding features.

## Install deterministic tooling

Use a repository-local manifest by default:

```powershell
dotnet new tool-manifest
dotnet tool install Paradigm.Enterprise.Cli --version <framework-version>
dotnet tool run paradigm doctor --project <solution>
```

Use `dotnet tool install --global Paradigm.Enterprise.Cli` only for evaluation. The CLI is not a runtime dependency and does not replace `Paradigm.Enterprise.CodeGenerator`.

Run `dotnet tool run paradigm api search <term> --project <solution>` only when an exact signature is needed. Prefer `--format json` for programmatic use and keep search limits small. Use bare `paradigm` only after a global install.

## Configure database-first projects

- Keep credentials in user secrets or environment configuration.
- Review EF Core Power Tools selection, context/namespace/output settings, key types, nullability, views, and routines.
- Treat EF/T4 and analyzer output as generated. Put behavior in partial entity/context files or change the owning template.
- Build immediately after regeneration and review the full generated diff.

## Finish

Run:

```powershell
dotnet restore <solution>
dotnet build <solution>
dotnet tool run paradigm validate --project <solution>
```

Then review authentication, authorization, CORS, serialization, middleware order, health, telemetry, secrets, and deployment policy; the framework does not choose them.
