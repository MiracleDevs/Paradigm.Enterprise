---
name: paradigm-setup-project
description: Set up or repair a Paradigm.Enterprise .NET solution, including the reviewed Web API template, layer projects, aligned NuGet packages, the Paradigm CLI, Aspire orchestration, database projects, database-first generation, and prerequisite checks. Use for new applications, framework adoption, package upgrades, restore/build failures, or generated-code setup.
---

# Set up a Paradigm project

Read and apply [Paradigm Good Coding Practices](../../references/good-coding-practices.md) and [Paradigm Solution Layout](../../references/solution-layout.md) before changing source, solution folders, dependencies, contexts, or host policy.

## Establish the baseline

1. Inspect the solution, target frameworks, project references, `Directory.Packages.props`, EF configuration, and generated-file headers. Do not assume the template layout.
2. Prefer .NET 10 and the reviewed Paradigm Web API template for a new solution. Use the sibling `C:\Repositories\github\Paradigm.Web.ApiTemplate` clone when available; otherwise clone `https://github.com/MiracleDevs/Paradigm.Web.ApiTemplate` into a temporary working directory and pass that local path to the scaffolder. Do not modify either template source. For an existing solution, preserve its naming and introduce only missing boundaries.
3. Keep one version across every `Paradigm.Enterprise.*` package. Reference a package from the project that uses its types; do not rely on accidental transitive access.
4. Preserve the dependency direction `Interfaces <- Domain <- Data <- Providers <- WebApi`. Database adapters depend on Data; infrastructure adapters expose focused contracts to Providers. Organize growing applications by bounded context and prefer domain-specific `DbContext` types over a general-purpose context.
5. Restore and build before adding features.

For a new solution, run the reviewed scaffolder from the Paradigm CLI:

```powershell
dotnet tool run paradigm scaffold solution `
  --template-root <Paradigm.Web.ApiTemplate> `
  --name <Company.Product> `
  --output <empty-repository-directory> `
  --paradigm-version <approved-version> `
  --dry-run
```

Review the dry-run inventory, then rerun without `--dry-run`. The CLI copies only template `src`, replaces template tokens and GUIDs, aligns Paradigm package references, preserves binary assets, never edits the source template, refuses a non-empty output directory, and creates the root `start.sh` Aspire wrapper. It also creates a baseline GitHub quality workflow and Paradigm problem matcher. Review those generated files and add any other repository policy deliberately after scaffolding.

Preserve the template's `.sln` or `.slnx` format and organize the canonical solution by the responsibilities in Paradigm Solution Layout. Solution-folder classification does not by itself require physical project moves. After selecting the database engine, use `$paradigm-build-database` to create `src/database` and add its project to that solution; do not create a second database-only solution.

## Install deterministic tooling

Use a repository-local manifest by default:

```powershell
dotnet new tool-manifest
dotnet tool install Paradigm.Enterprise.Cli
dotnet tool run paradigm doctor --project <solution>
```

Use `dotnet tool install --global Paradigm.Enterprise.Cli` only for evaluation. The CLI is a development tool for applications consuming Paradigm NuGet packages, not a runtime dependency. It supplies diagnostics, built-in C# checks, and explicit generation commands through one `paradigm` entry point.

Do not add a NuGet dependency without explicit user permission. Before requesting it, report the package's purpose, alternatives, open-source license and official repository, maintenance/security posture, and important transitive dependencies.

Run `dotnet tool run paradigm api search <term> --project <solution>` only when an exact signature is needed. Prefer `--format json` for programmatic use and keep search limits small. Use bare `paradigm` only after a global install.

## Configure database-first projects

- Use `$paradigm-build-database` to create or validate the SQL Server or PostgreSQL project and include it in the application solution under `src/database`.
- Use `$paradigm-setup-aspire` to add AppHost, ServiceDefaults, root `.env`, database bootstrap ordering, and optional deployment publishing.
- For Aspire SQL Server projects, use its governed container-bootstrap assets so SQLCMD, SqlPackage, and DACPAC publication stay inside Docker rather than becoming workstation prerequisites.
- Keep credentials in user secrets or environment configuration.
- Review EF Core Power Tools selection, context/namespace/output settings, key types, nullability, views, and routines.
- For multiple bounded contexts, keep one explicit EFPT config per context. Every selected database object and generated CLR type belongs to exactly one config/context; validate that selections are disjoint and complete. Keep cross-context foreign keys in the database and expose scalar IDs unless an explicit read contract owns the relationship.
- Treat EF/T4 and analyzer output as generated. Require both a source ownership marker within the first 2,048 characters and `System.CodeDom.Compiler.GeneratedCodeAttribute` on generated types when assembly validation is part of acceptance. Put behavior in partial entity/context files or change the owning template.
- When generated and handwritten partial files share a directory, cleanup, backup, recovery, and determinism checks must operate on an exact generated-file manifest. Never clear the whole directory.
- Build immediately after regeneration and review the full generated diff.

## Finish

Run:

```powershell
dotnet restore <solution>
dotnet build <solution>
dotnet tool run paradigm validate --project <solution>
```

Then review authentication, authorization, browser cookie/token storage and CSRF, CORS, serialization, middleware order, liveness/readiness, logs/traces/metrics, secrets, and deployment policy; the framework does not choose them.
