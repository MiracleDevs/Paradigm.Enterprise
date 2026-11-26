# Paradigm Solution Layout

For a new consuming Web API repository, put the canonical application solution at `src/api/<Product>.slnx`. Generate the native template directly into `src/api`; its solution root and `Directory.Packages.props` belong there. Preserve the template's solution format. Keep the database project under repository-root `src/database` and reference it from this same application solution; do not add a root-level compatibility solution or a separate database-only solution.

This is the default for new consuming applications, not a migration instruction for the Paradigm.Enterprise framework repository or an established application. Preserve an existing layout unless the user requests its reorganization, and validate references and root discovery when moving it.

## Physical project and source layout

All API-layer projects and their Aspire companions are siblings under `src/api`:

```text
<repository>/
  aspire.config.json          # when Aspire is requested
  start.sh                   # when Aspire is requested
  .env.example               # when Aspire is requested; .env stays untracked
  src/
    api/
      Product.slnx
      Directory.Packages.props
      Product.Interfaces/
      Product.Domain/
      Product.Data/
      Product.Providers/
      Product.WebApi/
      Product.HealthChecks/
      Product.CodeGenerator/
      Product.AppHost/        # when Aspire is requested
      Product.ServiceDefaults/
      Product.DatabaseBootstrap/  # when database bootstrap is requested
    database/
```

Repository orchestration configuration, environment files, and `start.sh` stay at the repository root. Use explicit `src/api/...` project paths in those entry points. Solution-folder classification below does not change these physical paths.

Keep the consuming application's persistence implementation in its single `<Product>.Data` project. Reference the selected framework adapter package there, for example `Paradigm.Enterprise.Data.SqlServer`. The framework's separate adapter packages are not a reason to split an application into `<Product>.Data` and `<Product>.Data.SqlServer` projects.

Put business code in containing folders from the first feature. Follow the template's module grouping and add responsibility folders as needed; for a `Catalog` module:

| Project | Example containing folders |
| --- | --- |
| `WebApi` | `Controllers/Catalog`, `Models/Catalog`, `JsonSerializerContexts`, `Exceptions/Handlers`, `Middlewares` |
| `Data` | `Catalog/Context`, `Catalog/Repositories`, `Catalog/StoredProcedures`, `Catalog/StoredProcedures/Parameters`, `Mappers` |
| `Domain` | `Catalog/Entities` for generated entities, database views, and their partials; `Catalog/Validation` for focused validation helpers |
| `Interfaces` | Module folders for genuine handwritten shared contracts/enums; retain the template's interface generator and its generated namespace mapping |
| `Providers` | `Catalog/Providers`, `Catalog/Models` for use-case-specific inputs when needed |

Match namespaces to ownership and folders. Populate only folders needed by the feature. Keep `Program.cs`, configuration, project files, and generator entry points at their natural project roots; do not flatten controllers, contexts, entities, repositories, providers, and their contracts into project roots. Database views remain Domain classes, not records placed in Interfaces. See [database-first models](database-first-models.md) for the generation contract.

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

For a new or reorganized consuming solution, verify `src/api` placement, sibling Aspire projects, a single application Data project, containing folders/namespaces, the exact six solution-folder names, responsibility mapping, unique project membership, project-file existence, curated solution items, database-project ownership, and absence of a competing legacy solution. Check physical paths separately from logical solution folders. Prefer semantic `.slnx` XML assertions over line counts, element ordering, or project-GUID text checks.
