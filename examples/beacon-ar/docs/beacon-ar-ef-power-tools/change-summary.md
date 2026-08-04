# Beacon AR EF Core Power Tools — Change Summary

> **Historical persistence note (superseded 2026-08-03):** This document records an earlier implementation stage. Current persistence ownership is defined by docs/beacon-ar-context-boundaries: four disjoint Access, MasterData, Operations, and Sales contexts/configurations; there are no Receivables or Reporting application roots.

> Historical record: commands and solution references below describe validation before the migration to root `BeaconAr.slnx`. Use `BeaconAr.slnx` for current work.

> Superseding ownership note: EFPT/T4 now emits metadata-derived generic entity/context identifiers and ordinary persistence navigations only. Domain partials own aggregate trackers and behavior; `ReceivablesDbContext.Relationships.cs` owns the `Customer`/`CustomerAddress` correction through the context partial hook. References below to generated aggregate trackers or a T4-owned relationship correction describe the earlier implementation and are not current guidance.

## Implemented

- Pinned the repository-local official `ErikEJ.EFCorePowerTools.Cli` tool at 10.1.1386 and verified its installed help/current config format.
- Added credential-free `src/BeaconAr.Data/efcpt-config.json` with explicit selection of 17 tables, nine public `{Entity}View` projections, and two internal pricing helper views. Stored procedures and functions are excluded from this context generation path.
- Built `BeaconAr.Database.sqlproj`, published its DACPAC to the disposable local SQL Server database `BeaconArEfcpt`, and generated from that live database with `efcpt` rather than `dotnet ef`.
- Replaced the legacy `database-first.json` and destructive `dotnet-ef` script. The new script verifies the local pin/config/T4 inputs and absence of credentials, restores tools, builds before and after generation, catches EFPT's error-text/zero-exit behavior, and validates all expected output markers without deleting output.
- Moved the generated boundary to 28 Domain entity/view files under `Receivables/Entities` and one context under `Receivables/Context`. All handwritten partials, repository contracts/implementations, providers, tests, generator consumers, and host registration now use the new namespaces.
- Adapted the EF Core 10 T4 templates from the DMS reference to the framework's current generic-ID API. The nine mapped entities use `EntityBase<int, I{Entity}, {Entity}, {Entity}View>`, generated scalar mappers, mapping/validation partial hooks, and collection trackers for the owned quote/order line aggregates. Their public views use `EntityBase<int>` and the same generated interface. Pricing helpers remain plain keyless shapes.
- Preserved integer IDs, DateOnly mappings, nullable reference types, audit and row-version fields, fluent mappings, keyless `ToView` mappings, `DbContextBase<int>`, the service-provider constructor, `OnModelCreatingPartial`, and the `FK_CustomerAddress_Customer` one-to-many correction.
- Reworked the incremental interface generator to include EFPT-generated four-argument entity classes safely, derive getter-only scalar contracts from Roslyn symbols, preserve exact type/nullability display, and exclude views/navigations as interface sources.
- Updated the Beacon README and framework database-first tutorial with SQL Server view conventions, CLI/config ownership, live-database generation, generated boundaries, and repository/provider responsibilities.

## Generated diff evidence

- EFPT discovery from the disposable live database: 17 tables, 11 views, and seven stored procedures discovered; explicit config produced 29 files (28 Domain shapes plus one context) and no routine output.
- Generated context contains 28 `DbSet<>` properties, 11 keyless mappings, 11 `ToView` mappings, and 12 row-version mappings across six table/view pairs.
- A second complete invocation of `build/regenerate-persistence.ps1` produced identical SHA-256 hashes for all 29 owned files.
- No connection string, password, user ID, or server value is present in `efcpt-config.json` or generated C#.
- Accidental investigation outputs under the example root and `Data/CodeTemplates` were removed; only the two reviewed templates remain under `CodeTemplates/EFCore`.

## Tests and validation

- `build/regenerate-persistence.ps1` — passed; both Release builds completed with zero warnings and zero errors.
- `dotnet build src/BeaconAr.sln --configuration Release` — passed with zero warnings and zero errors.
- `dotnet test --solution src/BeaconAr.sln --configuration Release --no-build --no-restore --minimum-expected-tests 1` — passed: 97 succeeded, 41 environment-gated tests skipped, zero failed.
- `BeaconAr.Architecture.Tests` — passed 14/14. New checks cover all nine compile-time entity/view/interface assignments, scalar type/nullability parity, aggregate-only collection trackers, EFPT file counts/markers, keyless `ToView` metadata, context base/constructor, and CustomerAddress cardinality.
- `GeneratedViewLiveTests.AllPublicEntityViewsAreQueryableAndExpandCommonReferences` — passed 1/1 against `BeaconArEfcpt`; it queried all nine generated view sets and verified joined user, customer, address type, status, product, and carrier fields in a rollback-only transaction.
- `dotnet tool run paradigm doctor --project src/BeaconAr.sln` — success.
- `dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution src/BeaconAr.sln --strict` — success.
- `dotnet tool run paradigm validate --project src/BeaconAr.sln` — passed cleanly after compiled generated-code metadata was added; no PE3101 findings remain.

## Final review resolution

- Removed the unused `dotnet-ef` manifest entry and added explicit Domain-assembly mapper/entity registration to the Web API host.
- The owning T4 now protects identity, creation/modification audit, deletion, and snapshot values during interface-to-entity mapping while retaining the complete getter-only shared scalar contract. `RowVersion` and normal editable fields continue to map; entity-to-view mapping includes server-owned fields.
- Entity, view, mapper, and context types now carry compiled `GeneratedCodeAttribute` metadata as well as the exact first-line EFPT marker.
- Regeneration now enforces the exact 28-plus-one output set before and after generation, rejects unexpected entries, verifies and removes only the known EFPT readme, and restores a validated backup after any failure. The recovery fixture passed, and a real wrong-database attempt was safely rejected and restored.
- Scalar tests now require the exact declared scalar set, getter-only interfaces, and type/nullability parity across all nine entity/interface/view surfaces.
- Final focused validation: Architecture tests passed 19/19; Domain and Data source checks passed; `paradigm validate` passed without PE3101; repeated EFPT generation was byte-for-byte stable for all 29 owned files; the script's Release builds completed with zero warnings and zero errors.

## Second-pass resolution

- Refactored the recovery fixture to copy the exact generated boundary into a uniquely named guarded temporary tree. Intentional `Product.cs` mutation, `Unexpected.cs` creation, readme deletion, restoration, and SHA-256 comparison occur only in that fixture; checked-in output is read-only during routine tests.
- Centralized connection-string redaction for verbose EFPT output, selected error excerpts, and caught exceptions. Full connection strings, extracted credential values, and standalone credential assignments are removed before output or rethrow. The sentinel redaction fixture passed.
- Moved generated mapper behavior tests to `BeaconAr.Domain.Tests`. Moved DI discovery coverage to `BeaconAr.WebApi.Tests`, where `WebApplicationFactory<Program>` resolves `Product`, `ProductView`, and `ProductMapper` from the actual host and executes `MapFrom`/`MapTo`.
- Removed the Architecture-to-WebApi project reference, force-refreshed `packages.lock.json`, passed locked restore, and verified the Architecture lock no longer contains `Paradigm.Enterprise.WebApi`.
- Affected Release builds passed with zero warnings/errors. Domain tests passed 37/37, Architecture tests passed 16/16, and Web API tests passed with 28 successes and 19 environment-gated live-database skips. `paradigm validate` remained clean.

## Remaining gaps

- Repository-to-generated-view adoption and Provider-owned mapping orchestration are intentionally deferred to Task 3.
- The tool-emitted `efcpt-readme.md` is generic and contradictory to Beacon's host registration, so it is ignored; Beacon-specific instructions live in the example README and regeneration script.
- EFPT's obsolete-file soft deletion remains disabled by design. Removing a selected object requires an explicit reviewed deletion after a successful replacement generation.
