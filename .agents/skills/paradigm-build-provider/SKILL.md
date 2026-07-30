---
name: paradigm-build-provider
description: Implement or review Paradigm.Enterprise providers for application use cases, CRUD hooks, mapping, validation, repository/service orchestration, transactions, commits, and external side effects. Use when adding business workflows or coordinating more than one collaborator.
---

# Build a Paradigm provider

## Choose the smallest base

- Use read/edit generic bases for conventional CRUD/search behavior.
- Use `ProviderBase` only when a custom use case needs its same-scope `GetProvider<TProvider>()` helper; otherwise implementing `IProvider` directly is sufficient.
- Keep the Provider independent of HTTP so workers and tests can reuse it.

Query installed signatures instead of guessing generic order:

```powershell
dotnet tool run paradigm api show ReadProviderBase --project <solution>
dotnet tool run paradigm api show EditProviderBase --project <solution>
dotnet tool run paradigm api show ProviderBase --project <solution>
```

## Implement for discovery

A provider must be public, concrete, implement `IProvider`, and implement the exact-name application interface: `CheckoutProvider` implements `ICheckoutProvider`. That interface should inherit the typed Enterprise provider contract where applicable.

Constructor-inject stable application collaborators. `ProviderBase` supplies only `ServiceProvider` and `GetProvider<TProvider>()`; `ReadProviderBase` resolves its view repository, and `EditProviderBase` resolves its repositories and `IUnitOfWork`. A custom provider that needs `IUnitOfWork` must constructor-inject it. Do not treat the service provider as a general locator.

## Own orchestration

- Coordinate repositories, domain behavior, identity/policy checks, mapping, and infrastructure adapters.
- Put intrinsic invariants in Domain; put EF/query mechanics in repositories.
- Use lifecycle hooks for checks that belong around generic edit behavior.
- Return application results/exceptions, not HTTP status codes or MVC results.
- Add and propagate `CancellationToken` on custom contracts that support it. The current generic CRUD contracts do not accept a token; do not claim controller cancellation reaches those calls.
- Define optimistic-concurrency ownership and translate update conflicts to an application exception; never silently overwrite a newer write.

Generic edit operations already commit. Do not add a second commit around them.

Read [transaction patterns](references/transactions.md) for custom multi-repository writes or external side effects.

## Test

Mock focused contracts for orchestration tests. Cover allowed/denied operations, hook ordering, mapping/validation failures, commit count, rollback, cancellation, and collaborator failures. Use a database integration test for transaction guarantees.
