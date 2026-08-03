# Beacon-ar solution layout decisions

These decisions resolve details that were not completely specified by the requirement or by the framework guidance. Reconsider them only when the stated trigger occurs.

## D1. Canonical solution format and location

**Context:** Beacon currently has `src/BeaconAr.sln`, while the requested structure is demonstrated by a concise `.slnx` and the task explicitly names a root target.

**Decision:** Make `examples/beacon-ar/BeaconAr.slnx` the single canonical solution and retire `examples/beacon-ar/src/BeaconAr.sln` after all consumers migrate.

**Consequences:** Paths in the solution are rooted at the example directory (`src/...` and `tests/...`). Automation and documentation must invoke the root `.slnx`. The repository avoids two representations that can drift.

**Rejected alternatives:** Keep both files; place the new `.slnx` under `src`; convert the old file in place.

**Reconsider when:** A supported downstream tool demonstrably cannot consume `.slnx` and has no project-level invocation or planned `.slnx` support.

## D2. Solution folders classify responsibility, not physical location

**Context:** The requested solution folders are conceptual. The physical project paths are already stable and moving them would expand risk across references, generation, scripts, and tooling.

**Decision:** Change only solution presentation. Do not move any project or test directory.

**Consequences:** A project displayed under `02.Modules` may remain physically below `src/`, and tests displayed under `05.Tests` remain below `tests/`. Folder membership does not alter dependency direction.

**Rejected alternative:** Reshape the disk tree to mirror the solution folders.

**Reconsider when:** A separate repository-wide physical-layout migration is approved with generation and automation impacts explicitly in scope.

## D3. `BeaconAr.ServiceDefaults` is the current shared project

**Context:** The standard permits HealthChecks, Telemetry, and other shared projects, but Beacon currently has one ServiceDefaults project and the task does not authorize inventing projects.

**Decision:** Put `BeaconAr.ServiceDefaults` under `01.Shared`. Do not create placeholder HealthChecks or Telemetry projects.

**Consequences:** The solution accurately represents current ownership without speculative abstractions.

**Rejected alternative:** Split ServiceDefaults during the layout task.

**Reconsider when:** Independent ownership, reuse, deployment, or test boundaries justify extracting a dedicated shared project.

## D4. The SQL Server schema is a module asset

**Context:** The legacy solution places `BeaconAr.Database` under a tools folder. A schema project owns application data and deployment artifacts; it is not a developer utility. The user prefers SQL Server.

**Decision:** Keep `src/database/BeaconAr.Database.sqlproj` unchanged and group it under `02.Modules` alongside Data, Domain, Interfaces, and Providers.

**Consequences:** The solution shows schema/data ownership together. SQL Server, Microsoft.Build.Sql, deployment ordering, and database validation remain unchanged.

**Rejected alternatives:** Keep the database under `04.Tools`; create a database-only solution; replace it with PostgreSQL.

**Reconsider when:** Database ownership moves to an independently delivered bounded context or a governed standard introduces a dedicated database solution folder.

## D5. Bootstrap and code generation are tools; AppHost and WebApi are hosts

**Context:** `BeaconAr.DatabaseBootstrap` is a finite executable used to publish/bootstrap the database, while `BeaconAr.AppHost` is the long-running local orchestration entry point. Their current shared legacy folder obscures those roles.

**Decision:** Group CodeGenerator and DatabaseBootstrap under `04.Tools`; group WebApi and AppHost under `03.Hosts`.

**Consequences:** Navigation reflects lifecycle and responsibility. This does not change AppHost resource ordering or executable behavior.

**Rejected alternative:** Treat every executable project as a host.

**Reconsider when:** DatabaseBootstrap becomes a deployed long-running service rather than a finite orchestration resource.

## D6. `00.SolutionItems` is curated configuration, not a miscellaneous folder

**Context:** The requirement calls for config files but does not enumerate them. Repository configuration can change over time.

**Decision:** During implementation, inventory the example root and include existing files that directly govern restore, build, formatting, tooling, test execution, or environment defaults. Do not create missing files solely to populate the folder, and do not add general source documentation as configuration.

**Consequences:** The solution makes operative settings discoverable while avoiding stale duplicates. Membership is based on file responsibility, not extension alone.

**Rejected alternatives:** Hard-code guessed filenames without checking existence; put every root file in SolutionItems; leave the folder empty.

**Reconsider when:** The repository adopts an explicit centrally governed SolutionItems manifest.

## D7. Historical validation records remain truthful

**Context:** Existing task change summaries record commands that actually ran against `src/BeaconAr.sln`. Blind replacement would make those audit records inaccurate, but unlabeled legacy paths confuse readers and AI tooling.

**Decision:** Update all active/copyable commands and current guidance to `BeaconAr.slnx`. Preserve old paths only in historical evidence, labeling them as pre-migration and pointing readers to the canonical root solution.

**Consequences:** A reference scan may retain explicit historical occurrences. Each exception must be intentional and self-explanatory; scripts and current instructions have no exceptions.

**Rejected alternatives:** Rewrite historical evidence as if it used `.slnx`; leave legacy references unexplained; delete historical task records.

**Reconsider when:** The project adopts versioned documentation where historical files are excluded from normal search/discovery.

## D8. `.slnx` tests validate semantics, not representation details

**Context:** The current direct test counts a project GUID occurrence in the text `.sln`. `.slnx` represents membership without those solution GUID mappings.

**Decision:** Parse XML and assert exact folder/project mappings, uniqueness, file existence, database membership, and legacy solution absence. Do not assert element order, whitespace, or incidental serialization.

**Consequences:** The test protects the requested convention and remains stable under harmless formatting changes.

**Rejected alternative:** Port the GUID-count assertion to another text-count heuristic.

**Reconsider when:** The .NET solution format provides a supported semantic API that is more reliable than direct XML parsing.

## D9. No dependencies or runtime changes belong to this task

**Context:** The layout repair can be completed with the SDK, existing test framework, and XML APIs.

**Decision:** Add no NuGet packages, project references, source projects, runtime configuration, or database objects.

**Consequences:** Review and rollback remain narrow. Any unrelated improvement becomes a separate task and branch.

**Rejected alternative:** Combine the layout migration with broader architecture or database refactoring.

**Reconsider when:** A verified `.slnx` consumer requires an approved dependency that cannot be replaced by the installed SDK.

