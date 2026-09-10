# Paradigm Solution Layout

Use one canonical application solution, preferably at the application or repository root. Preserve the template's `.sln` or `.slnx` format; do not add a compatibility solution or a separate database-only solution that can drift.

## Canonical solution folders

Classify projects and curated solution items by responsibility:

| Folder | Responsibility |
| --- | --- |
| `00.SolutionItems` | Existing build, restore, formatting, tooling, source-control, orchestration, and safe environment-default configuration that maintainers need to discover with the solution. Curate this list; do not include secrets, nonexistent placeholders, or unrelated documentation. |
| `01.Shared` | Genuine reusable cross-cutting projects such as service defaults, telemetry, or health-check libraries. Do not create speculative placeholder projects. |
| `02.Modules` | Application contracts, domain, data, providers, and other business modules. The database schema project is a module/data asset, including a SQL Server `.sqlproj`; it is not a developer tool. |
| `03.Hosts` | Deployable or executable hosts such as Web API, AppHost, worker, and other service entry points. |
| `04.Tools` | Finite development or deployment utilities such as code generators and database bootstrap/publish executables. A bootstrap that consumes a schema project does not own or replace that schema project. |
| `05.Tests` | Unit, architecture, integration, bootstrap, and host test projects. |

Solution folders are navigation and ownership metadata, not dependency boundaries. Classify an existing project by what it does. Do not move its physical directory, rename it, or change references merely to make its path mirror the solution folder unless the task separately requires and validates that migration.

Every project belongs exactly once in the canonical solution. Keep the database schema project with the modules, hosts with hosts, finite utilities with tools, and tests with tests. Preserve the framework dependency direction independently of this presentation.

## Mixed application and database solutions

A canonical `.slnx` may contain both managed C# projects and a database project. Paradigm CLI application commands inspect listed `.csproj` projects as managed assemblies; they must not treat a `.sqlproj` output or DACPAC as managed metadata. Validate the database boundary independently:

```powershell
dotnet tool run paradigm doctor --project <solution.slnx>
dotnet tool run paradigm validate --project <solution.slnx>
dotnet tool run paradigm database validate --project <database.sqlproj> --solution <solution.slnx> --strict
```

Restore, build, and test the canonical mixed solution so all members remain operational. Database validation supplements solution-wide build and application checks; it does not justify removing the database project from the solution.

## Review invariants

For a new or reorganized solution, verify the exact six folder names, responsibility mapping, unique project membership, project-file existence, curated solution items, database-project ownership, and absence of a competing legacy solution. Prefer semantic `.slnx` XML assertions over line counts, element ordering, or project-GUID text checks.
