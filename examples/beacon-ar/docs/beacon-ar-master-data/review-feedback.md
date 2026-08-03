# Beacon AR master-data final re-review feedback

> Historical record: commands and solution references below describe validation before the migration to root `BeaconAr.slnx`. Use `BeaconAr.slnx` for current work.

## Outcome

Approved. The second fix round resolves every release-facing finding from the prior review. Independent static review, framework validation, a clean Release build, the offline test matrix, and a fresh SQL Server 2025 LocalDB publication with all live tests found no remaining correctness, transaction, persistence, or database-contract defect. No source fix was made during this review.

## Final finding status

No blocking or non-blocking implementation finding remains.

### Resolved - Default-address re-parenting no longer creates a transient uniqueness violation

- `src/BeaconAr.Providers/MasterData/AddressProvider.cs` now locks both owners, clears destination defaults, and persists those clears inside the existing transaction before invoking the re-parent operation.
- `src/BeaconAr.Data/MasterData/AddressRepository.cs` applies the requested final row in one rowversion-checked update, avoiding EF alternate-key mutation and the prior intermediate state in which the moving row retained its old default flags under the destination customer.
- `DefaultAddressReparentClearsDestinationDefaults` proves billing-only, shipping-only, and combined moves live against the filtered unique indexes.

### Resolved - Search inputs are literal, bounded, and safe at the largest page number

- `MasterDataRequestValidator.ValidateSearch` trims search text and rejects values longer than 320 characters with the field path `search`.
- All four search routines escape `\`, `[`, `%`, and `_`, use an explicit `ESCAPE` clause, and promote offset arithmetic to `BIGINT` before multiplication.
- The live search test exercises every Provider with SQL metacharacters, the 320/321-character boundary, and `int.MaxValue` page input. The largest accepted page now returns an empty page instead of SQL error 8115.

### Resolved - Risk-based live coverage now exercises the previously missing failure paths

- Live tests cover destination-default re-parenting, concurrent billing and shipping replacement, cancellation after the resource save with resource-and-audit rollback, and reference-protected deletion for Product, Customer, Address, and Carrier.
- The focused Provider project now includes validation coverage for Customer, Address, and Carrier in addition to the deeper Product workflow fakes.
- The exact SQL foreign-key names remain explicitly classified in the persistence translator. The combined live reference test proves each public resource outcome, although it intentionally does not force every same-resource FK branch in isolation.

### Resolved - Verification evidence is accurate and reproducible

- The documented offline count is correct: 47 passed and 13 SQL integration cases skipped.
- SQL Server 2025 LocalDB version `17.0.4025.3` is installed. A fresh named instance accepted the current SQL Server 2022-targeted DACPAC without an incompatible-platform override, and the live matrix passed all 60 tests.

## Independent verification

Executed from `examples/beacon-ar` on 2026-08-01:

- `dotnet build src/BeaconAr.sln --configuration Release --no-restore`: passed with zero warnings and zero errors.
- `dotnet test --solution src/BeaconAr.sln --configuration Release --no-build --no-restore --minimum-expected-tests 1`, without a connection string: 60 discovered, 47 passed, 13 skipped, zero failed.
- Published `src/database/bin/Release/BeaconAr.Database.dacpac` to disposable SQL Server 2025 LocalDB `17.0.4025.3` without `/p:AllowIncompatiblePlatform`; publication succeeded.
- The same solution-level test command against the disposable database: 60 passed, zero skipped, zero failed.
- Focused `paradigm checks run` for Domain, Data, and Providers: success with no diagnostics.
- `paradigm database validate --strict`: success with no diagnostics.
- The disposable database and LocalDB instance were dropped after verification.

## Residual test scope

The suite does not independently trigger every Product, Customer, and Address quote-versus-order FK constraint branch, and the non-Product Provider unit tests remain deliberately lightweight. This is a future hardening opportunity, not a release blocker: exact-name classifier coverage was reviewed statically, every resource-level conflict is exercised live, and the complete 60-test SQL matrix passes.
