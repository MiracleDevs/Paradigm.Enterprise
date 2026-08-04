# Beacon AR framework CRUD migration plan

> **Historical persistence note (superseded 2026-08-03):** This document records an earlier implementation stage. Current persistence ownership is defined by docs/beacon-ar-context-boundaries: four disjoint Access, MasterData, Operations, and Sales contexts/configurations; there are no Receivables or Reporting application roots.

## Purpose

Migrate the straightforward Beacon AR master-data CRUD paths to the official Paradigm.Enterprise provider and repository bases. Keep the quote and sales-order workflows custom where their transaction, audit, status, snapshot, idempotency, and concurrency behavior makes a generic CRUD lifecycle unsafe.

This is an implementation plan only. It does not authorize code changes, route changes, or commits.

## Scope and ownership

The migration applies to Product, Customer, CustomerAddress, and Carrier. Their application repositories and providers become thin adapters over the official typed contracts and bases:

| Layer | Read contract/base | Edit contract/base |
| --- | --- | --- |
| Provider | `ReadProviderBase<TInterface, TView, TViewRepository, int>` | `EditProviderBase<TInterface, TEntity, TView, TRepository, TViewRepository, int>` |
| Repository | read repository exposing generated `*View` shapes | `EditRepositoryBase<TEntity, ReceivablesDbContext, int>` |

Use `ReadRepositoryBase<TView, ReceivablesDbContext, int>` for the standard generated view-query implementation. An existing stored-procedure search may remain a focused custom override/implementation when required, but it must still return the generated `*View` shape.

The providers own entity/view mapping and validation through the official base hooks. Repositories own only persistence-facing responsibilities: query, materialization, attach/load where the official edit base requires it, and staging entity changes for the provider-controlled commit lifecycle. Repositories must not absorb validation, mapping, or provider workflow decisions.

## Files to change

Adapt the existing files in the Beacon AR application rather than creating parallel abstractions:

- Product application repository interface and implementation.
- Customer application repository interface and implementation.
- CustomerAddress application repository interface and implementation.
- Carrier application repository interface and implementation.
- Product provider interface and implementation.
- Customer provider interface and implementation.
- CustomerAddress provider interface and implementation.
- Carrier provider interface and implementation.
- Dependency-registration files that currently bind the above interfaces to concrete repositories/providers.
- `MasterDataProviderBase` and all references to it; remove the base after the four master-data providers no longer depend on it.
- Quote and SalesOrder provider/repository interfaces and implementations only as needed to remove `SalesProviderBase` inheritance or duplicated base behavior, while preserving their named workflow APIs.
- Related provider/repository tests, compile-time contract tests, and generated-code integration tests.

Before modifying each file, use its concrete type names and namespaces from the existing Beacon AR project. Do not alter generated entity or generated `*View` source files.

## Master-data repository migration

For each of Product, Customer, CustomerAddress, and Carrier:

1. Change the read repository contract to inherit the official typed read repository contract for its generated `*View` and `int` identifier.
2. Change the editable repository contract to inherit the official typed edit repository contract for its generated entity and `int` identifier. If the existing app convention combines read and edit members in one interface, preserve that public shape while inheriting both official contracts.
3. Rebase the concrete read/query repository on `ReadRepositoryBase<TView, ReceivablesDbContext, int>`.
4. Rebase the concrete edit repository on `EditRepositoryBase<TEntity, ReceivablesDbContext, int>`, or adapt the combined repository to provide both official read and edit capabilities without duplicating their standard behavior.
5. Replace hand-authored read projection models with the corresponding generated `ProductView`, `CustomerView`, `CustomerAddressView`, or `CarrierView` type at repository boundaries.
6. Retain custom search methods only where the standard read base cannot express the query. Implement these as narrow query overrides/methods; stored-procedure results must be materialized as the generated `*View` type, never a legacy duplicate DTO.
7. Remove repository-side validation, DTO-to-entity mapping, direct provider-style commits, and unrelated workflow logic. Standard add/update/delete staging stays in the official edit repository base; transaction completion remains under the provider base lifecycle.

## Master-data provider migration

For each of Product, Customer, CustomerAddress, and Carrier:

1. Change the provider interface to inherit the official edit provider contract, `IEditProvider<TView, int>`, and retain any truly domain-specific members explicitly.
2. Rebase the provider on `EditProviderBase<TInterface, TEntity, TView, TRepository, TViewRepository, int>`; use the matching concrete generated entity, generated `*View`, and repository interfaces.
3. Where a read-only provider surface is required independently, use `ReadProviderBase<TInterface, TView, TViewRepository, int>` with the matching typed contract, instead of replicating read behavior.
4. Implement the official hooks for:
   - mapping the editable/request shape to the generated entity on create and update;
   - mapping generated entities to generated `*View` responses where the base requires it;
   - validation, including entity-specific business rules that are appropriate for master data;
   - any narrowly scoped lifecycle behavior that belongs to the provider.
5. Keep authorization and exception behavior consistent with current Beacon AR endpoint conventions, but do not move them into repositories.
6. Delete provider code made redundant by the official bases only after behavior is covered by tests.

`MasterDataProviderBase` is removed once all four providers are migrated. Do not replace it with another local generic CRUD base: the official framework bases are the shared abstraction.

## Quote and SalesOrder workflow boundary

Quote, SalesOrder, and QuoteConversion remain custom named workflow providers. Their public operations should continue to express their workflow intent rather than being reshaped into generic CRUD methods.

- Preserve explicit transaction control where a workflow spans multiple aggregates or writes.
- Preserve idempotency, audit creation, state/status transitions, snapshot persistence, and optimistic-concurrency handling as named workflow behavior.
- Replace `SalesProviderBase` inheritance and duplicated generic-base behavior with `ProviderBase` or a direct `IProvider` implementation, selecting the smallest framework abstraction that preserves the existing workflow semantics.
- Use generated entity and generated view repositories at their boundaries. This is a repository/type modernization, not authorization to force Quote or SalesOrder through `EditProviderBase`.
- QuoteConversion remains custom and is not converted to generic CRUD.

## Commit and transaction ownership

For the four master-data providers, rely on the official provider base lifecycle for validation, staging, and commit/transaction ownership. The provider invokes repository operations; repositories stage/query only and must not independently commit an operation.

For Quote, SalesOrder, and QuoteConversion, retain explicit provider-owned transaction boundaries. This includes the complete atomic unit for all related entity writes, audit records, snapshots, status changes, idempotency records, and concurrency checks. Repositories do not start or commit those workflows independently.

## Tests and verification

Add or update tests to establish the intended boundary, then run the relevant solution/project test suites and build:

- Provider tests for Product, Customer, CustomerAddress, and Carrier covering create, read, update, delete, validation failures, mapping, and provider-owned commit behavior.
- Repository tests verifying standard official-base behavior and any retained custom stored-procedure searches return the applicable generated `*View` type.
- Contract/registration tests or compilation coverage proving app repository interfaces inherit the official typed contracts and provider interfaces inherit `IEditProvider<TView, int>`.
- Regression tests that repositories do not commit changes independently.
- Quote, SalesOrder, and QuoteConversion workflow regression tests for idempotency, audit/status/snapshot behavior, transaction atomicity, and concurrency conflict handling after `SalesProviderBase` is removed.
- A clean build to identify stale legacy DTO references, obsolete `MasterDataProviderBase` references, and dependency-injection mismatches.

## Route and API impact (deferred to Task 4)

Do not change routes, controller actions, request/response JSON contracts, authorization policies, or OpenAPI shape in this task. Task 4 will evaluate endpoint exposure and route impacts once the provider and repository contracts are stable. The migration should preserve existing endpoint-facing method behavior through adapters or interface-compatible signatures where necessary.

## Decisions

- Generated `*View` types are the canonical read shapes at application repository and provider boundaries.
- Official framework bases replace local generic master-data abstractions.
- Provider hooks, not repositories, contain master-data mapping and validation.
- Repository customizations are permitted for queries such as stored-procedure searches, provided their output remains the generated `*View` shape.
- Workflow safety overrides generic uniformity: Quote, SalesOrder, and QuoteConversion retain explicit named workflows.
- `SalesProviderBase` is removed/replaced rather than extended as a second generic CRUD framework.

## Non-goals

- No migration of Quote, SalesOrder, or QuoteConversion into generic CRUD bases.
- No changes to generated entity or view source.
- No route/controller/API-contract work before Task 4.
- No new local generic provider base to replace `MasterDataProviderBase`.
- No change to persistence schema, stored procedures, or business workflow semantics solely to fit framework CRUD conventions.
- No implementation or commit as part of this plan.
