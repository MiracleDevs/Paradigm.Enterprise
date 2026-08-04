# Beacon AR domain ownership — decisions

> **Historical Provider/API note (superseded 2026-08-04):** This report preserves evidence and decisions from an earlier implementation stage. Current ownership is: repositories materialize canonical views; entities own intrinsic invariants; request objects own query and proposed-input rules; Providers invoke those rules and own only collaborator-dependent orchestration and transactions; MasterData exposes explicit request-based `IProvider` contracts rather than `IEditProvider`, `EditProviderBase`, or a fail-closed mutation guard; and controllers bind one `[FromQuery]` request and own HTTP concerns. See [the current Provider/API decisions](../beacon-ar-provider-api-conventions/decisions.md).

## Generated files stay generated; behavior stays beside them

The generated EF/T4 entity and view files remain replaceable output. Behavior is implemented only in a handwritten partial in the same capability folder and namespace as the generated type. The `.Behavior.cs` suffix is retained because the defect was separation/namespace drift, not the suffix. Regeneration must replace an exact generated manifest and preserve every co-located partial.

Generated views remain mutable only as a documented persistence/materialization exception. They do not receive write behavior, and public API mutations continue to use purpose-built request models.

## Entity validation and transport validation are different

Rules that can be decided from persisted entity state belong in `ValidateEntity` and intention-revealing behavior. Factories, replacements, and generated mapping hooks validate a complete proposed state before mutation so a rejected operation does not dirty an EF-tracked object.

Search paging, filter, sort allow-lists, and rowversion decoding are transport/query protocol concerns. They live on the request/parameter object or the single narrow Operations codec, not in a broad entity validator. Checks that need uniqueness queries, reference loading, identity, authorization, or remote services remain in Providers. Database constraints remain the final persistence guard.

`DomainValidator` is used for entity-state validation. Stable field-key validation exceptions remain appropriate for query/transport input and reference errors where the client contract requires field paths; those types must not become a second home for entity invariants.

Quote and SalesOrder replacement each expose one public `Replace` operation. It checks draft ownership, loaded reference facts, the complete normalized header, replacement lines, and audit inputs before assigning any field. The repository stages the returned already-validated lines only after that operation succeeds. Separate public prepare/apply phases are prohibited because they make lifecycle checks bypassable and can leave tracked headers partially changed after validation failure.

## Broad static validators are removed, narrow value/protocol policies remain

`MasterDataRequestValidator`, `SalesDomainValidation`, `SalesRequestValidator`, `SafeUrlValidator`, and the MasterData `ValidationErrorBuilder` are removed. Private entity methods may be static when they are an implementation detail of that entity; the prohibited pattern is a general class that owns several unrelated entities' rules.

Two shared helpers remain deliberately:

- Operations `VersionTokenCodec` owns the canonical SQL Server rowversion wire protocol. It has no business rules and replaces the duplicate MasterData wrapper.
- `MonetaryRounding` owns one shared value policy that must match SQL Server computed-column rounding for both Quote and Sales Order lines.

These are narrow protocol/value concepts, not general validation services.

## System catalog enums belong to Interfaces

`AddressType`, `IdempotencyState`, `QuoteStatus`, and `SalesOrderStatus` are cross-application catalog contracts and move to capability `Enums` namespaces in `BeaconAr.Interfaces`. Their assigned numeric IDs and machine codes are permanent public meanings and are tested against the complete idempotent SQL seed sets.

The generated Domain entity classes named `AddressType`, `IdempotencyState`, `QuoteStatus`, and `SalesOrderStatus` remain database models. Namespace aliases are used where necessary; generated types are not renamed to work around the collision. `AddressUsage` and `SortDirection` remain Domain transport enums because neither mirrors a seeded system table.

Stable code metadata is owned with each Interface enum rather than in a Domain-wide `AddressTypeCodes` class. Seed display labels are not enum identity and may change independently; IDs and machine codes may not be renumbered, reused, or silently changed.

The active idempotency states have explicit terminal shapes. `InProgress` has no resource, response, completion, or modification data. `Completed` is reachable only from `InProgress`, carries a 2xx response plus resource type/ID, and records matching modification/completion timestamps. `Failed` is also reachable only from `InProgress`, carries a 4xx/5xx response and matching terminal audit timestamps, and never identifies a resource. A terminal request cannot transition again. This defines the previously unspecified seeded `failed` member without changing current creation replay behavior; orchestration may opt into `Fail` when a durable failure policy is designed.

## Generated database views are the canonical read shapes

MasterData already returns `ProductView`, `CustomerView`, `CustomerAddressView`, and `CarrierView`; that remains the model for ordinary reads. Sales search is aligned with it: `SearchQuote` and `SearchSalesOrder` return the exact `QuoteView` and `SalesOrderView` shapes, so `QuoteSummaryDto`, `SalesOrderSummaryDto`, `QuoteSearchRow`, and `SalesOrderSearchRow` are deleted.

The Sales detail path has a deliberate compatibility exception. `QuoteDto`, `SalesOrderDto`, and `SalesLineDto` remain a named external client contract because the existing response is a flattened historical snapshot with nested lines and stable enum/version members; there is no single one-row database view that can express that hierarchy without repeating the header. Repositories must materialize the canonical header and line views first and map only at this boundary. The exception ends when a versioned client contract accepts canonical `{ header, lines }` composition; at that point the three DTOs and mapping are removed.

`CurrentUserDto` remains because evaluated policies are not database state. `DashboardSummaryDto` remains because it is one consistent aggregate routine result rather than an entity projection. Operation and paging envelopes remain because they add workflow/pagination meaning rather than copy a view.

## Write requests remain separate

Create, update, line, and transition requests remain handwritten. They intentionally omit server-owned identity, audit, snapshots, computed totals, deletion, status initialization, source-quote ownership, and unrestricted concurrency fields. Returning generated views does not authorize accepting those views for mutation.

The existing inherited generic MasterData edit surface remains fail-closed during this task. Task 5 owns the decision to simplify Provider CRUD and must exercise every inherited single/bulk operation before enabling it. It may not remove the safe write-command boundary merely to reduce method count.

## SQL Server search routines remain appropriate

The Sales searches remain typed stored procedures because paging, filtering, stable dynamic sort allow-lists, and performance are database concerns. They return canonical generated view shapes instead of handwritten parallel rows. This is not handwritten SQL in a repository: SQL stays in the SQL Server database project and Data uses typed routine wrappers/mappers.

## Guidance changes are evidence-based

This task updates durable guidance for three evidenced mistakes: multi-entity static validators, seeded catalog enums owned in Domain, and parallel read DTOs beside generated views. Beacon-specific type names and allow-lists remain in Beacon tests/docs. A universal CLI diagnostic is added only when metadata/source evidence can enforce the policy with documented exceptions and no application-name whitelist; judgment-heavy ownership decisions remain in skills/review guidance.
