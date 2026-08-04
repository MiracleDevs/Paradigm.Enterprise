# Beacon AR database view conventions decisions

> **Historical persistence note (superseded 2026-08-03):** This document records an earlier implementation stage. Current persistence ownership is defined by docs/beacon-ar-context-boundaries: four disjoint Access, MasterData, Operations, and Sales contexts/configurations; there are no Receivables or Reporting application roots.

## SQL Server remains the database engine

Beacon AR retains its SDK-style `Microsoft.Build.Sql` SQL Server project and `Sql160` target. The requested preference already matches the implementation, so no PostgreSQL conversion or second database project is introduced.

## Every view ends in `View`, including helpers

The canonical suffix applies to every view object, file, generated CLR type, `DbSet`, and `ToView` mapping. Entity projections use `{Entity}View`; helpers use a descriptive name that still ends in `View`. The pricing helpers are therefore canonicalized as `QuotePricingView` and `SalesOrderPricingView`.

No old-name compatibility views are retained because they would immediately violate the same rule. The stable public `QuoteView` and `SalesOrderView` contracts absorb the internal rename.

## All three Operations tables receive views

General Paradigm guidance does not automatically create read projections for internal, status, history, audit, or idempotency storage. Here, the explicit instruction that Operations tables lack views is a concrete application decision and takes precedence. `AuditLog`, `IdempotencyRequest`, and `IdempotencyState` therefore receive paired schema-bound views.

This is not generalized into a CLI rule requiring a view for every table in every `Operations` folder. Such a rule would conflict with legitimate internal storage and would need a stronger, configurable consumer signal. Beacon's coverage is enforced by its database smoke and architecture tests.

## Operations views preserve mapping data but remain internal

The three views preserve the base row's identifiers/scalars and add bounded user/state display fields. This supports deterministic EF generation and makes each projection useful without a second lookup. Joins are limited to unique keys and preserve one row per base record.

`KeyHash`, `RequestHash`, and `MetadataJson` remain present because the views are internal persistence projections and the base mapping surface must remain inspectable. Hashes are not plaintext idempotency keys, and the schema stores no response body. Nevertheless, none of these fields or Operations view types may be reachable from an HTTP action, serializer registration, OpenAPI component, or generated client. A future API use requires a purpose-specific contract and a separate security review; it must not return the raw operational view by convenience.

## Status/history tables outside Operations do not gain artificial views

`AddressType`, quote/order status catalogs, and status-history tables are outside the reported Operations gap and currently have no concrete new read consumer. They remain tables only. Their absence does not weaken the universal rule: every object that is a view must end in `View`; the separate decision is whether a table needs a view at all.

## The official template is the owner, with a documented compatibility delta

The read-only official source is `https://github.com/MiracleDevs/Paradigm.Web.ApiTemplate` at revision `522906b9151d566a5dddd2609b66da36726368a7`. Its `src` and distributable `template` T4 files are byte-identical. Beacon's current templates are not: they contain application-specific type lists and relationship names.

The official revision also targets an older Paradigm API. It emits non-generic `DbContextBase`, `EntityBase<IEntity,Entity,EntityView>`, `EntityMapperBase<IEntity,...>`, and `IAuditableEntity<DateTimeOffset>`, which cannot compile against the installed 1.1 generic identifier contracts. Copying it byte-for-byte or adding framework compatibility bases would either fail the build or pollute the framework with obsolete API shapes.

The selected resolution is an in-repository, provenance-documented copy of the official templates with only reusable compatibility adaptations: metadata-derived entity and actor identifier types, current generic bases/mappers/audit contracts, and required generated-code ownership metadata. Application type-name whitelists and special cases are prohibited. The sibling repository is not modified. When the official template gains equivalent current-generic behavior, its next reviewed revision is the trigger to resynchronize and reduce/remove the local compatibility delta.

## Generation is atomic and owned

`efcpt-config.json`, the T4 source, the pinned EFPT tool, and `build/regenerate-persistence.ps1` own the generated entity/view/context output. Renamed and added generated files are produced from a freshly published disposable SQL Server database, not edited or moved manually. Two consecutive generations must be byte-stable. A failed run restores the exact prior owned output.

The task keeps the current single `ReceivablesDbContext` only as a sequencing boundary. Task 3 owns the four-context/folder migration and must regenerate again from the same canonical view names.

## The CLI enforces only evidence that is deterministic

The SQL parser can reliably identify a created view and its name, so `PEDB112` enforces the `View` suffix for SQL Server and PostgreSQL. Existing `PEDB100` enforces object/file-name parity. Whether a specific internal table deserves a view, which descriptive fields are safe, and whether a projection should become an API contract remain review decisions because source metadata alone cannot determine them without false positives.

## Renames require no deployment compatibility script

The pricing helpers hold no data and have only schema-bound parent-view consumers in the same DACPAC. Updating those dependencies atomically lets SqlPackage plan the safe drop/create sequence. A legacy alias is forbidden, and pre-pre-deployment is not used for routine renames. The deployment report must still be inspected for external dependencies and unexpected table operations before publication.

## Mixed entity and audit-actor identifiers cannot use the current audit interface

`AuditLog` and `IdempotencyRequest` use `bigint` row identifiers but their audit-actor foreign keys use `int`. The current `IAuditableEntity<TDate,TId>` contract inherits `IEntity<TId>`, so it cannot truthfully express a `long` entity identifier and a separate `int` actor identifier. The generic template therefore emits the audit scalar properties and mappings but implements the audit interface only when the entity and actor identifier types are the same. This avoids a false contract while preserving every database column. A future framework contract with independent entity and actor identifier parameters can remove this compatibility decision.

## Aggregate ownership remains handwritten behavior

Database metadata cannot determine whether a collection navigation is an aggregate-owned, change-tracked domain collection. The reusable T4 template therefore emits persistence navigations only. `Customer`, `Quote`, and `SalesOrder` keep their application-specific collection ownership and add/remove or validation behavior in colocated partial classes. This prevents Beacon-specific aggregate names from leaking into an official-derived template.

## The CustomerAddress relationship is corrected in a context partial

EF Power Tools interprets the filtered unique index on `CustomerAddress` as a singular relationship during reverse engineering. The database intentionally allows a customer to have multiple addresses while constraining selected default roles. The correction belongs in `ReceivablesDbContext.Relationships.cs` through the generated context's partial hook, rather than in generated output or a type-name special case in T4.

## Generic marker interfaces must not create a second identifier candidate

An inspected type can expose both the non-generic `IEntity` marker and `IEntity<long>` through inheritance. Treating the marker as an independent legacy `int` contract produced a false `int|long` identifier diagnostic for the Operations pairs. CLI analysis now ignores the marker candidate when a closed generic entity contract is present, with a regression test for the mixed inherited shape.

## Repeat DACPAC publication has a known constraint-normalization gap

The first disposable SQL Server publication introduced the renamed and new views without table data loss or unexpected table operations. A second deployment report was not empty: DACFx proposed drop/recreate operations for seven pre-existing check constraints even though the view output was stable. Because those constraints are outside this task and the report contained no table rebuild or data-loss operation, the view change is accepted with the churn recorded as an environment/tooling gap. It must not be described as a fully no-op second DACPAC publication.
