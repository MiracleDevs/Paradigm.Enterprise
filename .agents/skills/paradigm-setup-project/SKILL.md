---
name: paradigm-setup-project
description: Set up or repair a Paradigm.Enterprise .NET solution, including the reviewed Web API template, layer projects, aligned NuGet packages, the Paradigm CLI, Aspire orchestration, database projects, database-first generation, and prerequisite checks. Use for new applications, framework adoption, package upgrades, restore/build failures, or generated-code setup.
---

# Set up a Paradigm project

Load `$paradigm-common-guidance` before changing source, solution folders, dependencies, contexts, or host policy.

## Establish the baseline

1. Inspect the solution, target frameworks, project references, `Directory.Packages.props`, EF configuration, and generated-file headers. Do not assume the template layout.
2. Use the .NET 10 SDK and the native `paradigm-webapi` solution template for a new Web API, generating into repository-root `src/api`. Confirm the active SDK and any ancestor `global.json` before generation. Fix missing template prerequisites through the published/source installation flow below; do not silently fall back to manually assembling layer projects. For an existing solution, preserve its naming and introduce only missing boundaries.
3. Keep one version across the application's `Paradigm.Enterprise.*` library packages through the root `Directory.Packages.props`. Reference a package from the project that uses its types; do not rely on accidental transitive access.
4. Preserve the dependency direction `Interfaces <- Domain <- Data <- Providers <- WebApi`. Database adapters depend on Data; infrastructure adapters expose focused contracts to Providers. Organize growing applications by bounded context and prefer domain-specific `DbContext` types over a general-purpose context.
5. Restore and build before adding features.

## Create a solution from the template

Use `Paradigm.Enterprise.Templates.WebApi`, short name `paradigm-webapi`. It is maintained under `templates/web-api` in [Paradigm.Enterprise](https://github.com/MiracleDevs/Paradigm.Enterprise/tree/main/templates/web-api). Solution creation uses `dotnet new`; it does not require the Paradigm CLI.

Select an explicit published template version matching the requested framework release. When the user has not chosen one, resolve a suitable stable published version from the configured feed. Repository or plugin version metadata alone does not establish that a package is published. Check `dotnet new uninstall` for an existing registration and reuse it when it is already the selected package version. Otherwise install that version:

```powershell
$templateVersion = '<published-template-version>'
dotnet new install "Paradigm.Enterprise.Templates.WebApi@$templateVersion"
```

For an unreleased version or work against a reviewed source revision, use an existing Paradigm.Enterprise checkout at that revision, or obtain it from the official repository. Read its `templates/web-api/README.md`. From that checkout, the maintained build/install entry point is:

```bash
bash templates/web-api/build/build-template.sh
```

Source packaging requires PowerShell 7 as well as the .NET SDK; the wrapper requires Bash. Without Bash, use the README's `dotnet pack` command and install the exact resulting `.nupkg` with `dotnet new install`. The local installer explicitly removes an existing package registration before installing, including same-version rebuilds. Use that installer for repeated local installation, or explicitly uninstall the package before replacing it manually. Remove any folder-based registration using its source path before switching to a package. Do not use `--force` for installation or generation, clone the former template repository, or modify template sources to customize an application.

Run generation from the consuming repository root into `src/api`. Substitute the product name, inspect the destination and dry-run inventory, then generate. Preserve existing repository configuration; resolve file conflicts before writing instead of silently overwriting it. A different physical layout requires an explicit user choice; solution folders are not physical project directories.

```powershell
dotnet new paradigm-webapi --name Company.Product --output ./src/api --dry-run
dotnet new paradigm-webapi --name Company.Product --output ./src/api
dotnet restore ./src/api/Company.Product.slnx
dotnet build ./src/api/Company.Product.slnx
```

Keep the generated `src/api/Directory.Packages.props`, its `ParadigmEnterpriseVersion` property, and versionless project `PackageReference` items. Verify that the generated library versions match the selected release. Keep the application's single Data project and reference its database adapter package there; do not add a separate application `Data.SqlServer` project. For a source build against unpublished framework packages, configure the supplied local package feed for restore and report that prerequisite explicitly. Do not upgrade a scaffold by merely replacing version strings without restoring and building it.

The template creates seven application projects, the six canonical solution folders, CPM, and Development-only Scalar documentation. Application projects disable `IsPackable` and `GeneratePackageOnBuild`; preserve those settings. It does not create AppHost, `start.sh`, a local tool manifest, or CI workflows. Add orchestration through `$paradigm-setup-aspire` when it is part of the task, and add repository automation according to the user's scope.

To run the generated API from the repository root, use `dotnet run --project ./src/api/Company.Product.WebApi --launch-profile https`; use its launch settings for the URL and open `/scalar/v1`. Database connection configuration is still required by the host. The sample context, provider, and commented controller are placeholders. Before implementing a data endpoint, follow [database-first models](../paradigm-common-guidance/references/database-first-models.md), verify EFPT-generated Domain entities/views and analyzer-generated interfaces, then continue with `$paradigm-build-feature` in the shared layout's containing folders.

Preserve the template's `.sln` or `.slnx` format and organize the canonical solution by the responsibilities in the solution layout from `$paradigm-common-guidance`. Solution-folder classification does not by itself require physical project moves. When adding a database project, use `$paradigm-build-database` to create `src/database` and add its project to that solution; do not create a second database-only solution.

## Install deterministic tooling

Install the Paradigm CLI separately when diagnostics or application code generation are needed. Use an existing repository-local manifest, or create one when absent. Choose and pin an available CLI version compatible with the application's packages; do not assume every framework release also publishes the CLI:

```powershell
dotnet new tool-manifest
dotnet tool install Paradigm.Enterprise.Cli --version <published-cli-version>
dotnet tool run paradigm doctor --project <solution>
```

Use `dotnet tool install --global Paradigm.Enterprise.Cli` only for evaluation. The CLI is a development tool for applications consuming Paradigm NuGet packages, not a runtime dependency. It supplies diagnostics, built-in C# checks, and explicit generation commands through one `paradigm` entry point.

Do not add a NuGet dependency without explicit user permission. Before requesting it, report the package's purpose, alternatives, open-source license and official repository, maintenance/security posture, and important transitive dependencies.

Run `dotnet tool run paradigm api search <term> --project <solution>` only when an exact signature is needed. Prefer `--format json` for programmatic use and keep search limits small. Use bare `paradigm` only after a global install.

## Configure database-first projects

- When a database project is part of the task, use `$paradigm-build-database` to create or validate the SQL Server or PostgreSQL project and include it in the application solution under `src/database`.
- When orchestration is part of the task, use `$paradigm-setup-aspire` to add AppHost, the explicit `Paradigm.Enterprise.Aspire.Hosting` and `.ServiceDefaults` project references, root `.env`, `start.sh`, database bootstrap ordering, and optional deployment publishing. Prefer the Paradigm Aspire libraries' versioned recipes over copying their hosting or telemetry code into the application.
- For Aspire SQL Server projects, use its governed container-bootstrap assets so SQLCMD, SqlPackage, and DACPAC publication stay inside Docker rather than becoming workstation prerequisites.
- Keep credentials in user secrets or environment configuration.
- Review EF Core Power Tools selection, context/namespace/output settings, key types, nullability, views, and routines.
- Domain database-first entity and view scaffold files MUST be generated by EF Core Power Tools with the reviewed T4 templates. Never create them manually, through scripts, or as temporary placeholders. Require actual generation evidence before dependent feature work; missing database metadata or tooling is a prerequisite to report, not permission to substitute handwritten models.
- For multiple bounded contexts, keep one explicit EFPT config per context. Every selected database object and generated CLR type belongs to exactly one config/context; validate that selections are disjoint and complete. Keep cross-context foreign keys in the database and expose scalar IDs unless an explicit read contract owns the relationship.
- Treat EF/T4 and analyzer output as generated. Require both a source ownership marker within the first 2,048 characters and `System.CodeDom.Compiler.GeneratedCodeAttribute` on generated types when assembly validation is part of acceptance. Put behavior in partial entity/context files or change the owning template.
- When generated and handwritten partial files share a directory, cleanup, backup, recovery, and determinism checks must operate on an exact generated-file manifest. Never clear the whole directory.
- Build immediately after regeneration and review the full generated diff.

## Finish

Run:

```powershell
dotnet restore <solution>
dotnet build <solution>
```

When the Paradigm CLI is installed, also run `dotnet tool run paradigm validate --project <solution>`. Report the template version/source, generated solution path, build result, run command, and any remaining prerequisites. Distinguish a buildable scaffold from a verified database query or production-ready host.

Then review authentication, authorization, browser cookie/token storage and CSRF, CORS, serialization, middleware order, liveness/readiness, logs/traces/metrics, secrets, and deployment policy; the framework does not choose them.
