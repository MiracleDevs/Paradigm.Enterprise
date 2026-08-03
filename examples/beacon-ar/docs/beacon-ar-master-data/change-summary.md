# Beacon AR master-data change summary

> Historical record: commands and solution references below describe validation before the migration to root `BeaconAr.slnx`. Use `BeaconAr.slnx` for current work.

## Implemented

- Added transport-neutral application contracts for Products, Customers, Customer Addresses, and Carriers: separate create/update requests, immutable response DTOs, resource-specific search inputs, allow-listed sorting, filters, and the required `items/pageNumber/pageSize/totalPages/itemsCount` page shape.
- Search text is trimmed, limited to 320 characters, and treated literally by SQL Server, including `%`, `_`, `[`, and `\`. Page-offset arithmetic is promoted to `BIGINT`, so the largest valid `int` page number returns an empty page instead of overflowing.
- Added field-addressable validation and normalization for all release-one master-data rules. Required text is trimmed, optional blank text becomes `null`, country codes normalize to uppercase, payment terms and numeric bounds are enforced, and failed entity replacement validates the entire proposed state before mutation.
- Aligned monetary validation exactly with SQL Server storage: Product `unitPrice` must fit `decimal(19,4)` and Customer `creditLimit` must fit `decimal(19,2)`. Overflow and value-changing excess scale now fail at the application boundary under the correct field path.
- Hardened external URL fields without dereferencing them. Product thumbnails accept safe absolute HTTP(S) URLs; carrier templates require HTTPS and allow only the optional literal `{trackingNumber}` placeholder. Credentials, control characters, braces in thumbnails, and unknown template placeholders are rejected.
- Added canonical SQL Server rowversion encoding/decoding. Public DTOs expose Base64 versions rather than raw bytes, and update/delete workflows reject malformed tokens before opening a transaction and stale tokens with `concurrency_conflict`.
- Added handwritten behavior partials outside generated persistence directories for all four master-data entities. They own create/replace, activation state, audit stamping, and address-default clearing while preserving the database-first generated boundary.
- Added exact-name edit and view repository pairs for every resource plus an append-only audit repository and reviewed SQL Server persistence-error classifier. Individual reads use `AsNoTracking`; the four paged searches now execute reviewed SQL Server routines that return count metadata before rows, apply allow-listed ordering in SQL, and use `Id` as the final tie-breaker.
- Added case-insensitive duplicate prechecks aligned with the database collation, with the existing unique constraints remaining authoritative for races. Reference checks cover every Product, Customer, Address, and Carrier quote/order foreign-key path; named SQL constraint failures are translated without exposing SQL text.
- Added public exact-name Providers for all four resources. Every mutation uses one explicit Unit of Work transaction, actor/correlation/time metadata, cancellation-aware repository calls, rollback on every failure, and safe stable application errors. Rollback now clears the scoped EF change tracker so a caller can safely attempt another use case in the same scope. Creates save twice inside one transaction so the generated resource ID and its audit fact remain atomic.
- Implemented serialized address-default changes. Customer rows are locked one at a time in ascending ID order with parameterized `UPDLOCK, HOLDLOCK`; previous defaults are cleared and separately audited, moves lock both owners, referenced addresses cannot be re-parented, and the filtered unique indexes remain the final invariant. The re-parent path uses a rowversion-checked database update because EF treats the composite address/customer principal key as immutable.
- Default-address updates persist destination clears inside the existing transaction before applying the requested final default state. Re-parenting uses one rowversion-checked full-row database update, avoiding both transient filtered-index conflicts and EF alternate-key mutation.
- Added direct Domain tests for normalization, boundary rules, payment terms, address/default compatibility, safe URLs, atomic failed replacement, version tokens, and search validation.
- Added a Provider test project with focused handwritten fakes covering create save/commit counts, audit metadata, stale versions, reference conflicts, unexpected persistence failures, rollback, invalid-query short-circuiting, and activation-specific audit actions.
- Extended architecture checks for exact-name repository/provider discovery and infrastructure-free public contracts.
- Added live SQL Server tests covering all four Providers together: stored-routine search/filter/page metadata and ordering, case-insensitive uniqueness, versioned update and stale-write/delete rejection, physical and reference-protected deletion, inactive reads, atomic default replacement, forward response mapping, safe audit facts, and correlation/actor consistency. Fresh-scope race tests prove unique-insert conflict translation, same-scope recovery after rollback, and repeated inverse address moves without deadlock.

HTTP controllers, authentication/authorization, ETag header parsing, Problem Details rendering, JSON enum configuration, and OpenAPI exposure remain deferred to `beacon-ar-api-security-contract` as planned.

## Verification evidence

Executed from `examples/beacon-ar` on 2026-08-01:

- `dotnet restore src/BeaconAr.sln --property:RestoreAdditionalProjectSources=../../artifacts` passed.
- `dotnet build src/BeaconAr.sln --configuration Release --no-restore` passed with zero warnings and zero errors.
- `dotnet test --solution src/BeaconAr.sln --configuration Release --no-build --no-restore --minimum-expected-tests 1` passed through Microsoft.Testing.Platform under the pinned .NET SDK 10.0.302. Without a connection string, 47 tests pass and the 13 SQL integration cases skip as designed.
- A fresh named LocalDB instance was explicitly created with installed SQL Server 2025 LocalDB version `17.0.4025.3`; the current SQL Server 2022-targeted DACPAC published without a platform override. With `ConnectionStrings__DatabaseConnection` pointed at that disposable database, the same solution-level command passed all 60 tests with no failures or skips.
- `dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution src/BeaconAr.sln --strict --format json` passed with `status: success` and zero diagnostics.
- `dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release` passed with zero warnings and zero errors.
- `build/regenerate-persistence.ps1` completed against LocalDB; the generated Domain entities and EF context remained byte-for-byte clean and the regenerated solution built with zero warnings and zero errors.
- Focused `paradigm checks run` passed for Domain, Data, Providers, and WebApi; replacing EF pagination removed all four `PE3103` findings.
- `paradigm validate` completed with only the existing database-first public-setter advisories for generated persistence shapes.

## Tooling note

The solution-wide `paradigm checks run` still cannot semantically compile Aspire's generated `Projects.*` types in `BeaconAr.AppHost/Program.cs` and reports `PE1002`. Focused production-layer checks pass, normal .NET compilation succeeds, and this is the same source-generator metadata limitation documented by the foundation task rather than a master-data diagnostic.
