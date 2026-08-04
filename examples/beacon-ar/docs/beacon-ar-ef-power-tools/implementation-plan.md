# Beacon AR EF Core Power Tools — Implementation Plan

> **Historical persistence note (superseded 2026-08-03):** This document records an earlier implementation stage. Current persistence ownership is defined by docs/beacon-ar-context-boundaries: four disjoint Access, MasterData, Operations, and Sales contexts/configurations; there are no Receivables or Reporting application roots.

## Goal

Replace Beacon AR's hand-invoked `dotnet ef dbcontext scaffold` workflow with a repository-local, version-pinned EF Core Power Tools CLI (`efcpt`) reverse-engineering configuration and the existing customized T4 templates. Generate the SQL Server `Receivables` persistence model deterministically: entities and API-read views live in `BeaconAr.Domain/Receivables/Entities`, the context lives in `BeaconAr.Data/Receivables/Context`, and matching entity/view pairs share a generated `I{Entity}` interface.

`Receivables` is the current persistence bounded context: it owns the transactional Sales, MasterData, and Access tables that participate in the one EF model. This task does not split that model into independently deployable bounded-context DbContexts. A future split must first define cross-context query and FK/navigation ownership.

## Superseding ownership note

The database-view convention review supersedes the earlier template-ownership statements below where they describe application behavior. EFPT/T4 owns generated scalar properties, persistence navigations, mappings, and the context constructor. Domain partials own aggregate `DomainTracker<TEntity>` members, add/remove methods, and child validation. `ReceivablesDbContext.Relationships.cs` owns the application-specific `Customer`/`CustomerAddress` cardinality correction through `OnModelCreatingPartial`. The reusable templates contain no aggregate or relationship-name branches.

## Evidence and constraints

- The source of truth is the SQL Server project `src/database/BeaconAr.Database.sqlproj`, included in root `BeaconAr.slnx`; it explicitly compiles tables, views, routines, functions, types, and sequences. No database-project relocation is required.
- Task 1 created the consumer DTO views: `ApplicationUserView`, `ProductView`, `CustomerView`, `CustomerAddressView`, `CarrierView`, `QuoteView`, `QuoteLineView`, `SalesOrderView`, and `SalesOrderLineView`. Every one retains the base entity's mapping columns, including `Id`, so it can implement the corresponding entity interface. `QuotePricingView` and `SalesOrderPricingView` remain internal helper views, not DTO interfaces. These names supersede the original pre-convention helper names.
- The current `database-first.json` and `build/regenerate-persistence.ps1` invoke `dotnet-ef`, write `*/Generated`, and do not select the new DTO views. They are legacy scaffolding artifacts, not EFPT configuration.
- `CodeTemplates/EFCore/EntityType.t4` and `DbContext.t4` are valuable owning templates: they infer Paradigm entity and audit-actor identifier types from EF metadata, generate the required service-provider context constructor, and expose `OnModelCreatingPartial`. Preserve and evolve them; never replace them with stock EF output or add application type/relationship names.
- The checked-in Roslyn generator currently ignores generated classes and all views. It therefore cannot create the interfaces required here. The reference DMS project demonstrates the intended analyzer ownership, but Beacon AR needs a deliberate table/view-pair extension rather than copying its older implementation unchanged.

## Deterministic CLI workflow

`ErikEJ.EFCorePowerTools.Cli` stable `10.1.1386` is the approved generation tool: it targets .NET 10, has no package dependencies, is MIT licensed, and is maintained in the official EFCorePowerTools GitHub repository. `dotnet tool search ErikEJ.EFCorePowerTools.Cli --detail` confirms the stable version.

Install it locally, from `examples/beacon-ar/`, with `dotnet new tool-manifest` (only if `.config/dotnet-tools.json` does not already exist) and `dotnet tool install ErikEJ.EFCorePowerTools.Cli --version 10.1.1386`. Commit the manifest lockstep with the generated configuration. Run it only as `dotnet tool run efcpt`; no global installation is part of the workflow.

The implementer must run `dotnet tool run efcpt --help` and the command-specific help **after the pinned installation**, then create/configure the CLI's supported `efcpt-config.json` input format—not the Visual Studio extension's legacy `efpt.config.json` format. Check the CLI-produced config into `src/BeaconAr.Data/efcpt-config.json`; retain explicit object selection, paths, namespaces, templates, nullable settings, and no connection string. Its connection belongs in the caller's development environment, never the config or repository. Do not infer option names from the legacy extension config.

The Visual Studio extension is optional developer convenience only. It may be documented only if it can use an independently maintained extension configuration without being represented as the deterministic/repository generation path. It must not overwrite, replace, or become a prerequisite for `efcpt-config.json`.

## Target ownership and layout

| Artifact | Owner and target | Notes |
| --- | --- | --- |
| EF Core Power Tools CLI selection/settings | `src/BeaconAr.Data/efcpt-config.json` | CLI-owned, checked-in config produced/validated by `efcpt`; selected SQL objects are explicit and it contains no connection string. |
| Local tool pin | `.config/dotnet-tools.json` | Pins `ErikEJ.EFCorePowerTools.Cli` exactly to `10.1.1386`; invoke only through `dotnet tool run efcpt`. |
| Optional extension configuration | no committed file by default | Add only after verifying it cannot undermine CLI determinism; it is not the source of truth. |
| Entity/view classes | `src/BeaconAr.Domain/Receivables/Entities/*.cs` | EFPT/T4-owned, auto-generated only. Tables use `EntityBase<int>`; matching `{Entity}View` types implement `I{Entity}` without becoming tracked aggregates. |
| Context | `src/BeaconAr.Data/Receivables/Context/ReceivablesDbContext.cs` | EFPT/T4-owned. It exposes table and keyless view sets, maps views with `ToView`, and keeps the Paradigm base/context constructor. |
| Entity interfaces | compiler output of `BeaconAr.Interfaces` analyzer into the Domain compilation | The analyzer owns `I{Entity}`. It derives getter-only scalar members from generated table entities and includes `IEntity<int>`. No generated interface is hand edited. |
| Invariants/aggregate behavior | existing and new Domain partials outside `Entities` | Domain partials own trackers, add/remove methods, child validation, and other application behavior; never place them in EFPT output. |
| Application-specific EF relationship correction | `src/BeaconAr.Data/Receivables/ReceivablesDbContext.Relationships.cs` | The context partial owns the `Customer`/`CustomerAddress` one-to-many correction through `OnModelCreatingPartial`; the generic T4 does not name this relationship. |

Use the current `efcpt --help` output to produce a supported `efcpt-config.json` with the following required *semantics*: `ReceivablesDbContext`; `BeaconAr.Data.Receivables.Context`; `BeaconAr.Domain.Receivables.Entities`; context output `Receivables/Context`; external Domain entity output `../BeaconAr.Domain/Receivables/Entities`; T4 enabled with `Data/CodeTemplates`; nullable reference types; database names; fluent configuration; no connection string; and an explicit table/view object list. Record the exact generated option/property names in the configuration rather than translating legacy `efpt` keys. Generate from a disposable published local SQL Server database rather than a DACPAC because EF Core Power Tools documents view/computed-column metadata limitations for DACPAC input.

## File-level implementation sequence

1. From `examples/beacon-ar/`, install/restore `ErikEJ.EFCorePowerTools.Cli` `10.1.1386` through the repository-local tool manifest. Inspect `dotnet tool run efcpt --help` and command-specific help; document the exact invocation and the CLI-generated `efcpt-config.json` schema before committing either config or output.
2. Build and deploy the SQL project using the existing DatabaseBootstrap/local SQL Server path to a disposable local SQL Server database. Run the pinned CLI against that database to create and validate `src/BeaconAr.Data/efcpt-config.json`; commit it only after verifying explicit object selection, output paths, namespaces, T4 setting, and absence of a connection string.
3. Replace `src/BeaconAr.Data/database-first.json` with the CLI config (or remove it if no other consumer uses it). Replace `build/regenerate-persistence.ps1` with a guarded CLI script that validates the named connection is supplied, restores the local tool, builds before and after generation, verifies the expected `efcpt-config.json`/T4 files, and invokes the exact pinned `dotnet tool run efcpt` command. It must not delete outputs itself or invoke `dotnet ef`.
4. Move the generated-output boundary from `Domain/Receivables/Generated` to `Domain/Receivables/Entities` and from `Data/Receivables/Generated` to `Data/Receivables/Context`. Update all partial namespaces/usings and direct consumers atomically. Delete only the former EFPT-owned generated files after a successful regeneration has produced their replacements; preserve handwritten `*.Behavior.cs`, repository contracts, and tests.
5. Amend `EntityType.t4` while retaining reusable base-type/auditing, nullable, navigation, and mapping behavior. Detect EF view mappings and identifier types from EF metadata. Internal helper views remain keyless types without artificial paired interfaces. Validate both audit-actor columns and fail template generation clearly when their underlying identifier types disagree. Persistence navigations remain generated collections; domain partials own aggregate behavior.
6. Amend `DbContext.t4` to infer the single audit-actor identifier type from audited model metadata, use it for `DbContextBase<TActorId>`, and correctly generate all table/view `DbSet`s and mapping. Preserve `OnModelCreatingPartial`; apply the `Customer`/`CustomerAddress` correction in the context partial rather than T4. Do not move mapping into repositories.
7. Update `EntityInterfaceGenerator.cs` to inspect EFPT-generated `EntityBase` table classes (remove the generated-folder exclusion), derive an exact getter-only `I{Entity} : IEntity<int>` interface from their scalar mapping surface, and continue excluding view classes as interface *sources*. The T4 template—not a second analyzer rule—makes views implement their corresponding interfaces. Ensure the generator ignores navigations and uses symbol data rather than source text so nullability/type names match compilation.
8. Regenerate through the pinned CLI config. Review the complete diff for every listed table, nine DTO views, the two helper views, primary keys, keyless `ToView` mappings, nullability, `DbSet` exposure, foreign keys, and namespace/output paths. Correct source SQL or templates/config and regenerate; do not patch generated C#.
9. Update `README.md`, `docs/tutorials/database-first.md`, and the relevant Paradigm generation guidance only after the workflow is proven. State the SQL Server/EF Core Power Tools CLI/T4 ownership, the `efcpt-config.json` command path, clean-output rules, DTO-view interface contract, and the provider/repository boundary. Record the tool-version decision and any unresolvable tool constraint in this task's `decisions.md`.

## Generated-output safeguards

- Treat only `Domain/Receivables/Entities` and `Data/Receivables/Context` as replaceable. Each generated file must retain EFPT's exact auto-generated first-line marker so the tool can safely remove obsolete selected objects.
- Before re-engineering, require a clean/stashed unrelated worktree and a green build. EFPT can remove any selected object's generated file when it is deselected; the explicit config object list is the protection against accidental scope loss.
- Generate into a disposable published-database/copy-worktree fixture first to verify external `OutputPath` behavior. If the pinned CLI cannot safely emit to the Domain project, do not introduce copy/move postprocessing: resolve the supported CLI layout first.
- Keep custom templates under `Data/CodeTemplates/EFCore`; they are source and reviewed in diffs. Generated classes, generated interfaces, and mapped stored-procedure output are never manual customization points.

## Tests and validation

- Add a compile-time contract fixture (one assertion per DTO pair) proving `Product` and `ProductView` (and each of the other eight pairs) are assignable to the same `I{Entity}` interface. Add reflection/property assertions that the base entity's scalar interface members exist on the matching view with the same type/nullability contract.
- Extend the live database integration tests to query all nine `DbSet<*View>` objects through the generated context, asserting they are keyless/read-only mappings and retain expected joined data. Keep existing view SQL tests.
- Preserve/add metadata assertions for `FK_CustomerAddress_Customer` as one-to-many, final domain-partial aggregate ownership, and the generated context's metadata-derived `DbContextBase<TActorId>`/service-provider constructor availability.
- Run `dotnet restore BeaconAr.slnx`, `dotnet build BeaconAr.slnx --configuration Release`, affected Domain/Data/Architecture/Database integration tests, `dotnet tool run paradigm doctor --project BeaconAr.slnx`, `dotnet tool run paradigm validate --project BeaconAr.slnx`, and `dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution BeaconAr.slnx --strict`. Inspect the EFPT-generated diff before accepting it.

## Non-goals

- Do not rewrite repositories to consume generated views or implement Provider-owned DTO mapping; that is Task 3.
- Do not replace framework providers/controllers, change API authentication/OpenAPI, alter the SQL DTO view definitions, or split the single persistence model into separate database contexts.
- Do not add an application/runtime NuGet package or persist credentials. The explicitly approved, repository-local EF Core Power Tools CLI is a development tool only.

## Risks and decisions requiring review

1. **CLI/config semantics:** `efcpt` `10.1.1386` is pinned, but the implementer must derive command/config fields from its installed help and generated `efcpt-config.json`, not legacy extension documentation.
2. **External Domain output:** use the pinned CLI's supported external output only after a disposable published-database/copy-worktree test. A post-generation mover would destroy the stable ownership boundary.
3. **Interface surface:** base table columns are the shared contract; view-only descriptive joins deliberately remain on the view and are not forced onto entities. Audit fields are handled consistently with the Paradigm auditable base, not duplicated ad hoc in interfaces.
4. **Pricing helpers:** retain `QuotePricingView` and `SalesOrderPricingView` as internal keyless types until Task 3 removes their repository consumers; do not make artificial helper contracts.
