# Beacon AR framework CRUD implementation decisions

## Address naming

The existing application names the capability `Address` while the persistence entity and generated view are `CustomerAddress` and `CustomerAddressView`. This task retained the established provider, repository, controller, and route names to avoid Task 4 API changes, while using `CustomerAddress`/`CustomerAddressView` in all official typed generic contracts.

## Stored-procedure search materialization

The existing SQL Server procedures continue to own validated filtering, sorting, and pagination, but now select from the matching `{Entity}View` and return its complete generated shape. The established atomic `BeaconAr.CodeGenerator` `mappers` target discovers those result types from the procedure wrappers and owns their data-reader mappers and central registrations under `BeaconAr.Data/Mappers`. This avoids generated-file edits and the inconsistent two-round-trip ID hydration that could observe two database states.

The exact master-data generated result boundary is `ProductView`, `CustomerView`, `CustomerAddressView`, and `CarrierView`. The former `ProductSearchRow`, `CustomerSearchRow`, `AddressSearchRow`, and `CarrierSearchRow` types are not compatibility contracts and were removed with their stale generated mappers and registrations. Repositories register only `StoreProcedureMappersRegisterer`; no bounded-context-specific parallel mapper registry is permitted. Procedure discovery and registrations are ordinally ordered while reflected properties retain metadata/declaration order, making consecutive generation runs repeatable without changing parameter-table column semantics.

## Temporary legacy controller adapters

Task 4 owns route and controller conversion. The current request/DTO methods remain on each master-data provider interface and map to and from generated views inside Providers. No generic official mutation action was added to an HTTP controller in this task. In particular, the existing ETag protocol remains the only exposed update/delete path, so no unsafe generic update or delete endpoint bypasses the concurrency policy.

Because `IEditProvider<TView, int>` advertises mutation methods even when controllers do not expose them, each master-data provider explicitly overrides and rejects every official single/bulk add, update, save, and delete overload. This is intentional fail-closed behavior, not a completed generic mutation implementation. Task 4 must define how row-version input, generated-ID audits, duplicate checks, referenced deletes, and bulk atomicity map to the official controller contract before enabling these methods.

## Generated identifiers and audit saves

Audit rows require the final database-generated integer identifier. For create operations, Beacon AR therefore keeps two participant saves inside one explicit outer SQL transaction: save the entity, stage its audit using the assigned identifier, save the audit, then commit the transaction. This is the documented exception to the normal one-save provider operation. Cancellation or any second-save failure rolls back the outer transaction and discards tracked state.

## Official provider search versus mutation

The official provider search is operational through `MasterDataViewSearchParameters`, provider validation, and each view repository's official search hook. The generic framework read contracts do not carry cancellation tokens, while the temporary legacy identifier operations do; application view repositories therefore retain an explicit cancellation-aware identifier overload. Mutation remains guarded as described above, and the temporary request operations preserve the stricter lifecycle until Task 4 decides the final transport contract.

## Sales abstraction choice

Quote, SalesOrder, and QuoteConversion do not use `EditProviderBase` because their operations span aggregate lines, histories, snapshots, status transitions, idempotent conversion, and explicit transactions. They implement their exact-name provider interfaces directly. A sealed collaborator holds only cross-workflow mechanics; it is neither discoverable as an `IProvider` nor an inheritance extension point. The class and constructor remain public solely because Web API registers it through DI and the public providers accept it; its properties and helper/operation methods are internal because they have no cross-assembly caller.

## SQL project identity and folders

Explicit nested `Folder` items are retained because they describe Visual Studio's logical project tree while the `Build`/`None` globs determine compilation and deployment content. The project does not declare a separate `ProjectGuid`; the solution's existing project identifier remains authoritative. `TargetDatabaseSet` was removed because the normal Microsoft.Build.Sql build and strict database validator do not require it.

## Guidance scope

The review exposed two reusable failure modes rather than Beacon AR-only preferences: a typed provider can
compile while inherited operations are unsafe or unimplemented, and a routine result change can accidentally
create a second mapper ownership path. These remain judgment-based workflow rules because their required
lifecycle and generator commands depend on the application. They were therefore promoted into the narrow
provider, repository, and review skills rather than a broad CLI diagnostic. The existing behavior and
architecture tests are the deterministic fixtures for this example; a future framework metadata signal that
can distinguish intentionally guarded operations from incomplete ones would justify a built-in semantic rule.
