---
name: paradigm-build-repository
description: Implement or review Paradigm.Enterprise repository contracts, read projections, editable aggregate persistence, searches, contexts, and stored-procedure boundaries. Use when adding data access, queries, CRUD persistence, aggregate deletion, or repository registration.
---

# Build a Paradigm repository

## Select the contract

- Use a read/view repository for projections and query-specific shapes.
- Use an edit repository for aggregate writes.
- Use a custom `RepositoryBase` implementation when generic CRUD/search does not express the query.
- Keep stored-procedure/database-specific mechanics in Data; expose application-meaningful contracts inward.

Confirm exact versioned bases before coding:

```powershell
dotnet tool run paradigm api search RepositoryBase --project <solution> --limit 20
dotnet tool run paradigm api show EditRepositoryBase --project <solution>
```

## Implement for discovery

A repository must be public, concrete, assignable to `IRepository`, and implement exactly one exact-name interface: `OrderRepository` implements `IOrderRepository`. Make that application interface inherit the appropriate typed Enterprise repository contract.

Keep one identifier type across entity, repository, provider, and controller contracts. Pass explicit assembly roots to registration when the host cannot reach the module through references.

## Keep the boundary narrow

- Put EF queries, projections, includes, database calls, and persistence mechanics here.
- Never return `IQueryable`, `DbContext`, provider-specific connections, or HTTP types from a repository contract or public repository member.
- Do not decide whether the caller may act or whether a business operation is allowed.
- Avoid unbounded reads; make ordering and pagination deterministic.
- Override the protected search function before exposing generic search.
- Map database concurrency tokens without exposing provider-specific state through repository contracts.

Repository writes stage changes. The Provider/Unit of Work owns commit timing.

## Select EF or a stored procedure

- Prefer a stored procedure for pagination, complex or dynamic filtering, multi-join/reporting queries, and multi-step database operations.
- Use EF/LINQ only for simple, bounded queries whose translation, cardinality, and ordering are predictable.
- Keep in-memory LINQ outside this selection rule; the concern is database query translation and execution.

For aggregate child removal, query selection, and provider-specific stored-procedure mechanics, read [repository patterns](references/repository-patterns.md).

## Test

Cover filtering, ordering, pagination, not-found behavior, projections, key types, database constraints, and provider-specific SQL/routines. Use integration tests when provider translation or transaction behavior matters.
