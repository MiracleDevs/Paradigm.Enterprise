# Beacon AR EF Core Power Tools — Decisions

> **Historical persistence note (superseded 2026-08-03):** This document records an earlier implementation stage. Current persistence ownership is defined by docs/beacon-ar-context-boundaries: four disjoint Access, MasterData, Operations, and Sales contexts/configurations; there are no Receivables or Reporting application roots.

## EFPT CLI contract

- The deterministic tool is the official MIT-licensed `ErikEJ.EFCorePowerTools.Cli` 10.1.1386 package from ErikEJ/EFCorePowerTools. It is pinned in the example-local tool manifest and has no NuGet package dependencies.
- With `dotnet tool run`, CLI help must be requested as `dotnet tool run efcpt -- --help`; without the separator, `dotnet` displays its own `tool run` help.
- The committed file uses the CLI's current kebab-case `efcpt-config.json` schema. The legacy Visual Studio extension configuration and `database-first.json` are not interchangeable with it.
- `t4-template-path` is `.` because EFPT expects the path containing `CodeTemplates/EFCore`, not the `CodeTemplates` directory itself. The root namespace is empty because the model and context namespaces are already fully qualified; setting both produced a duplicated `BeaconAr.BeaconAr` prefix.
- EFPT 10.1.1386 can print an `error:` while returning process exit code zero for a T4 generation failure. The regeneration script therefore checks both the exit code and emitted error records, then validates the exact expected file count and ownership markers.

## Model boundary

- `ReceivablesDbContext` remains one bounded persistence context for this example. Splitting Access, MasterData, and Sales requires a separate decision about cross-context foreign keys, navigation ownership, and query composition.
- Nine public view/entity pairs receive generated `I{Entity}` contracts and mapped Paradigm bases: `ApplicationUser`, `Carrier`, `Customer`, `CustomerAddress`, `Product`, `Quote`, `QuoteLine`, `SalesOrder`, and `SalesOrderLine`.
- Public DTO views derive from `EntityBase<int>` as well as implementing the shared interface. The current framework's `EntityBase<int, TInterface, TEntity, TView>` and `EntityMapperBase<int, ...>` require `TView : EntityBase<int>`; implementing the interface alone is insufficient because `IEntity<int>` also requires `IsNew`.
- `QuotePricingView` and `SalesOrderPricingView` remain plain keyless read shapes. The convention task supersedes the original helper names. Status catalogs and history types do not receive artificial public view contracts; Operations projections remain internal.
- Superseding decision: T4 emits only ordinary EF persistence collections. Database metadata cannot safely identify aggregate ownership, so the `Quote.QuoteLines` and `SalesOrder.SalesOrderLines` `DomainTracker<TEntity>` members, add/remove behavior, and child validation live in the corresponding Domain partials. The application-specific `Customer`/`CustomerAddress` correction lives in `ReceivablesDbContext.Relationships.cs` through the generated context's partial hook.
- Interfaces are generated from declared scalar table properties using Roslyn symbols. They inherit `IEntity<int>`, use getter-only properties, include audit/concurrency scalar fields, and exclude navigations. View-only descriptive join fields deliberately stay off the shared interface.

## Generation and cleanup

- The DACPAC is built and published to a disposable live SQL Server database before EFPT runs. DACPAC input was used only for discovery while investigating the CLI; committed generation uses the live database so view and computed-column metadata are complete.
- EFPT owns exactly `src/BeaconAr.Domain/Receivables/Entities` and `src/BeaconAr.Data/Receivables/Context`. The script does not recursively delete or move output. Obsolete cleanup remains disabled, and explicit selection plus the exact EFPT marker define the replacement boundary.
- EFPT emits `src/BeaconAr.Data/efcpt-readme.md` on every run. It is generic tool output with configuration suggestions that do not match Beacon, so the example ignores it; `README.md`, `docs/tutorials/database-first.md`, and `build/regenerate-persistence.ps1` are authoritative.

## Final review decisions

- The complete getter-only scalar interfaces remain the entity/view compile-time contract, but not every member is client-writable. Generated interface-to-entity mappings ignore `Id`, creation/modification audit fields, deletion fields, and historical `*Snapshot` fields. Entity-to-view mapping still exposes them. `RowVersion` remains mapped as explicit caller-supplied concurrency protocol state, and ordinary editable fields continue to map.
- The Web API explicitly passes the generated Domain assembly to `RegisterMappers` and `RegisterEntities`; runtime mapping does not rely on entry-assembly traversal.
- Both T4 templates preserve the first-line EFPT marker and emit `System.CodeDom.Compiler.GeneratedCodeAttribute("EFCorePowerTools", "10.1.1386")` on generated entity, view, mapper, and context types. Comments support source/replacement checks; the compiled attribute supports assembly validation.
- Regeneration validates the exact 28 Domain filenames and one context filename plus their markers before and after EFPT. Production generation verifies and removes only EFPT's known generic readme byproduct and uses a validated temporary backup to restore owned output after generation, boundary-validation, or build failure. `-TestFailureRecovery` copies all 29 files into a uniquely named, guarded temporary fixture and performs every intentional mutation, unexpected-file creation, readme deletion, restore, and hash comparison there; routine tests never write to checked-in generated files or delete the real readme.
- Every EFPT log or caught generation exception is passed through one redaction function before verbose output or rethrow. It removes the exact connection string, extracted password/user/access-token values, and standalone credential assignments. `-TestRedaction` uses sentinel credentials and fails generically if any sentinel survives, without echoing the secret values.
- The exact-set guard rejected an actual invocation against the wrong database because only two configured views existed, then restored the owned output. This confirms stale files cannot mask an incomplete source schema.
- Final assembly validation is clean after adding compiled generated-code metadata; PE3101 no longer reports EFPT-generated setters.

## Deferred work

- Repositories still expose the pre-existing detail shapes and some repository-side projections. Moving repositories to generated views and making Providers own all entity/view mapping is Task 3.
- Provider/controller base-class adoption, OpenAPI/authentication changes, and API-root behavior are outside this task.
- `paradigm validate` now recognizes every EFPT-generated persistence type through compiled generated-code metadata and completes without PE3101 findings.
