# Beacon AR Database Views — Decisions

> **Historical Provider/API note (superseded 2026-08-04):** This report preserves evidence and decisions from an earlier implementation stage. Current ownership is: repositories materialize canonical views; entities own intrinsic invariants; request objects own query and proposed-input rules; Providers invoke those rules and own only collaborator-dependent orchestration and transactions; MasterData exposes explicit request-based `IProvider` contracts rather than `IEditProvider`, `EditProviderBase`, or a fail-closed mutation guard; and controllers bind one `[FromQuery]` request and own HTTP concerns. See [the current Provider/API decisions](../beacon-ar-provider-api-conventions/decisions.md).

## Scope of API-facing views

We treated master-data entities and the quote/sales-order transactional aggregate and line tables as consumer-facing: `ApplicationUser`, `Product`, `Customer`, `CustomerAddress`, `Carrier`, `Quote`, `QuoteLine`, `SalesOrder`, and `SalesOrderLine`. This is the smallest set that supports the example's API workflows while giving every major entity or transaction table a one-row-per-entity `{Entity}View` contract.

Reconsider this scope when the API adds a concrete read use case for another table, or when an existing table becomes externally consumable rather than implementation storage.

## Excluded internal tables

Status catalogs, status history, audit/idempotency storage, and similar internal tables have no views because they are not API DTOs in this example. They remain available to domain workflows and diagnostics through purpose-built paths. Adding broad views for them would make implementation details public without a consumer need.

Reconsider this decision when an API, report, export, or support workflow needs a stable read contract for one of these tables.

## Pricing helpers

`QuotePricingView` and `SalesOrderPricingView` remain schema-bound helper views. The convention task supersedes their original names. They centralize aggregate calculation and feed `QuoteView` and `SalesOrderView`, while the entity-named views remain the DTO boundary. Helper or reporting views need not use an entity name, but every view still ends in `View`.

Reconsider this decision if a pricing shape becomes an independently supported API/read-model contract or no longer represents a reusable aggregate helper.

## Joined-field balance

Each entity view preserves the base-table mapping surface, retains foreign-key IDs, and adds commonly used descriptive fields such as names, codes, status display values, audit-user names, and parent/product fields for lines. Joins are bounded to retain one row per base entity; uncommon or high-cardinality related data stays out of the default view.

Reconsider the projection when repeated consumers need a missing field, a field is consistently unused, or a proposed join risks duplicate entity rows, significant cost, or disclosure beyond the DTO's purpose.

## Deferred repository adoption

This task establishes the database DTO contract and updates the quote and sales-order search procedures. EF Core Power Tools generation of entity/view classes, shared domain interfaces, and context code is deferred to Task 2. Detail repositories retrieving generated view shapes and Provider-owned mapping/validation orchestration are deferred to Task 3. The target rule remains that repositories retrieve entities or views and Providers map them; it is not a claim about the current detail-repository implementation.

Reconsider the sequencing only if the database contract changes before generation, or a correctness issue requires an earlier data-layer change.

## Guidance control choice

No deterministic validator was added. Whether a table is major, consumer-facing, or has a useful descriptive join requires product and security judgment that source metadata cannot reliably infer. The durable skill/reference guidance records the default and its exceptions. Promote a narrow check only if future examples establish an unambiguous metadata signal and a fixture set that distinguishes internal storage from consumer-facing entities.
