---
name: paradigm-build-provider
description: Implement or review Paradigm.Enterprise providers for application use cases, CRUD hooks, mapping, validation, repository/service orchestration, transactions, commits, and external side effects. Use when adding business workflows or coordinating more than one collaborator.
---

# Build a Paradigm provider

Load `$paradigm-common-guidance` before creating or editing source.

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
- When a generated database view is the reviewed read/API contract, return that canonical view from the provider and remove transitional parallel DTOs, result rows, `ToDto` helpers, and compatibility methods. Keep a second transport model only for a named external compatibility contract with tests, a documented removal trigger, and mapping sourced from canonical views rather than persistence entities or a parallel query shape.
- Use lifecycle hooks for checks that belong around generic edit behavior.
- Do not inherit `IEditProvider` or `EditProviderBase` when the application write contract is intentionally incompatible with view-based single/bulk mutations and every inherited mutation would only throw. Expose the smallest honest command-oriented Provider contract, document the missing framework capability needed for a future migration, and preserve explicit concurrency, audit, overposting, cancellation, and transaction behavior.
- Treat a typed provider interface as an executable contract, not an inheritance marker. Before exposing
  `IEditProvider<TView, TId>`, exercise every single and bulk add, update, save, delete, get, and search
  operation through that interface. A migration may temporarily override unsupported mutations to fail
  closed, but it must cover every overload and document what transport or concurrency decision will enable
  them; never leave unsafe inherited behavior callable.
- Return application results/exceptions, not HTTP status codes or MVC results.
- Add and propagate `CancellationToken` on custom contracts that support it. The current generic CRUD contracts do not accept a token; do not claim controller cancellation reaches those calls.
- Define optimistic-concurrency ownership and translate update conflicts to an application exception; never silently overwrite a newer write.

Generic edit operations already commit. Do not add a second commit around them.

Read [transaction patterns](references/transactions.md) for custom multi-repository writes or external side effects.

## Test

Mock focused contracts for orchestration tests. Cover allowed/denied operations, hook ordering, mapping/validation failures, commit count, rollback, cancellation, and collaborator failures. When inheriting a typed provider contract, test its full single and bulk surface through the interface; reflection-only inheritance assertions do not prove lifecycle safety. Use a database integration test for transaction guarantees.
