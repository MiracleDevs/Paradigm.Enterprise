# Beacon AR foundation final re-review feedback

## Outcome

Approved. No actionable foundation findings remain. The generated persistence boundary now represents customer/address ownership as one-to-many, and the live ownership test reaches and independently proves both composite foreign keys.

## Final corrections

### Customer/address EF cardinality

- SQL Server reverse engineering interpreted the filtered unique default-address indexes as global uniqueness and scaffolded `FK_CustomerAddress_Customer` as one-to-one.
- The owning EF Core T4 templates now contain a constraint-specific normalization for that known provider ambiguity. Regeneration emits `Customer.CustomerAddresses` and `HasOne(...).WithMany(...)` without editing generated output or changing either filtered index.
- `CustomerAddressOwnershipMetadataIsOneToMany` inspects the compiled EF model and asserts that the foreign key is not unique, its principal navigation is a collection named `CustomerAddresses`, and relationship fixup tracks two addresses for one customer.
- Independent re-review regenerated all 20 persistence files with zero SHA-256 differences; its pre- and post-generation solution builds passed with zero warnings and zero errors.

### Live ownership fixtures

- Quote and sales-order test identifiers now use the schema's exact 20-character maximum rather than prefixing a full GUID.
- The live test also asserts the violated constraint name, proving the quote insert fails on `FK_Quote_CustomerAddress` and the sales-order insert independently fails on `FK_SalesOrder_CustomerAddress`.
- The focused ownership test passed against the disposable `BeaconArFoundationGeneration` LocalDB database; both failures were SQL error 547 rather than truncation error 2628.

## Verification evidence

Executed from `examples/beacon-ar` on 2026-08-01:

- `build/regenerate-persistence.ps1` completed against the published disposable LocalDB schema; all 20 generated files were byte-for-byte stable and both builds passed cleanly.
- The focused `CustomerAddressOwnershipMetadataIsOneToMany` test passed against the compiled EF metadata model.
- The focused `TransactionRootsRejectAnotherCustomersShippingAddress` test passed live against LocalDB and asserted both named composite ownership constraints.
- The complete solution test run passed all 20 tests with the LocalDB connection supplied; no test was skipped.
- The prior SQLCMD 18 execution, secret/TLS handling, structured bootstrap logging, strict database validation, and CI SQL Server 2022 regeneration gates remain in place.

## Environmental limitation

The workstation has SQLCMD 15 rather than the required SQLCMD 18, and Docker's Linux daemon remains unavailable. The exact SQLCMD 18 bootstrap and SQL Server 2022 publication path remain CI-owned; no unsupported local runtime claim is made.
