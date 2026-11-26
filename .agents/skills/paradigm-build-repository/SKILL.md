---
name: paradigm-build-repository
description: Implement or review Paradigm.Enterprise repository contracts, read projections, editable aggregate persistence, searches, contexts, and stored-procedure boundaries. Use when adding data access, queries, CRUD persistence, aggregate deletion, or repository registration.
---

# Build a Paradigm repository

Load `$paradigm-common-guidance` before creating or editing source.

## Select the contract

- Use a read/view repository for projections and query-specific shapes.
- Use an edit repository for aggregate writes.
- Use a custom `RepositoryBase` implementation when generic CRUD/search does not express the query.
- Keep stored-procedure/database-specific mechanics in Data; expose application-meaningful contracts inward.

Confirm exact installed bases before coding:

```powershell
dotnet tool run paradigm api search RepositoryBase --project <solution> --limit 20
dotnet tool run paradigm api show EditRepositoryBase --project <solution>
```

## Implement for discovery

Keep contexts, repositories, typed routine callers, and parameter/result plumbing in the application's single Data project, under module/responsibility folders from the common solution layout. Use the framework's database adapter package in that project. Before writing a repository, verify the generated Domain entity/view and interface contract described in [database-first models](../paradigm-common-guidance/references/database-first-models.md).

Require actual EF Core Power Tools output for database-first Domain entities and views. Never create a missing entity/view scaffold manually, including temporary result records used as a substitute. Report missing generation prerequisites before dependent repository work.

A repository must be public, concrete, assignable to `IRepository`, and implement exactly one exact-name interface: `OrderRepository` implements `IOrderRepository`. Make that application interface inherit the appropriate typed Enterprise repository contract.

Keep one identifier type across entity, repository, provider, and controller contracts. Pass explicit assembly roots to registration when the host cannot reach the module through references.

Use a context owned by the repository's bounded domain. Introduce a small `SharedDbContext` only for data with genuine shared ownership; do not grow a general-purpose application context.

## Keep the boundary narrow

- Put EF queries, projections, includes, database calls, and persistence mechanics here.
- Never return `IQueryable`, `DbContext`, provider-specific connections, or HTTP types from a repository contract or public repository member.
- Do not decide whether the caller may act or whether a business operation is allowed.
- Avoid unbounded reads; make ordering and pagination deterministic.
- Override the protected search function before exposing generic search.
- Map database concurrency tokens without exposing provider-specific state through repository contracts.

Repository writes stage changes. The Provider/Unit of Work owns commit timing.

## Select EF or a stored procedure

- Production repositories must not contain handwritten SQL query or command text. Do not use raw `FromSql*`, `ExecuteSql*`, `SqlQuery*`, `DbCommand.CommandText`, equivalent `Query*`/`Execute*` connection extensions with string/`FormattableString` command parameters, or SQL-bearing repository field/property assignments there. A runtime command argument does not make the connection extension safe. This includes partial and source-generated production repository declarations.
- Prefer a typed stored-procedure boundary for pagination, locking and range locking, complex or dynamic filtering, multi-join/reporting queries, performance-sensitive paths, and multi-step database operations. Keep the SQL object in the database project and its typed parameter/result plumbing in Data.
- For an existing or requested stored procedure, use the matching Paradigm `StoredProcedureBase` or `ResultStoredProcedureBase` caller and registered mappers. SQL Server table-valued inputs are supported; inspect the installed mapper APIs and [table-parameter guidance](references/repository-patterns.md#sql-server-table-valued-parameters) before claiming a limitation or replacing the caller with ad hoc SQL.
- Use EF/LINQ only for simple, bounded queries whose translation, cardinality, and ordering are predictable.
- Keep in-memory LINQ outside this selection rule; the concern is database query translation and execution.
- Run `dotnet tool run paradigm checks run --project <solution>` and resolve every `PE3107`; an exception requires an exact, justified, expiring suppression and does not move SQL ownership into the repository.

For aggregate child removal, query selection, and provider-specific stored-procedure mechanics, read [repository patterns](references/repository-patterns.md).

## Test

Cover filtering, ordering, pagination, not-found behavior, projections, key types, database constraints, and provider-specific SQL/routines. Use integration tests when provider translation or transaction behavior matters.
