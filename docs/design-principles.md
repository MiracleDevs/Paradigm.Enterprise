# Design principles

Paradigm.Enterprise provides a layered path for building an API, but a layer diagram is not the goal. The goal is to keep business decisions, transport policy, persistence mechanics, and infrastructure integration understandable as each one changes for a different reason.

The framework is most useful when its conventions reduce routine code without hiding ownership. Applications still decide their domain boundaries, security policy, data design, deployment model, and operational requirements.

## Separate by reason to change

A controller changes when the HTTP contract changes. A provider changes when a use case or orchestration rule changes. A domain entity changes when business behavior changes. A repository changes when persistence or query behavior changes. An infrastructure service changes when an external system or protocol changes.

Keeping those reasons separate is more important than placing every class in a prescribed folder. A provider that returns HTTP status codes has absorbed a transport concern. A repository that decides whether an operation is allowed has absorbed an application or domain concern. A domain entity that resolves a repository has absorbed infrastructure.

The boundaries are not barriers to collaboration. A request crosses several layers by design. Each boundary should make the contract clearer and prevent implementation details from becoming assumptions everywhere else.

## Keep dependencies pointed inward

The common dependency direction is:

```text
Web API
    Providers
        Data contracts and Domain
            Interfaces
```

Concrete data projects implement repository contracts and depend on Entity Framework. Providers depend on contracts and coordinate use cases. Controllers depend on provider contracts. Domain behavior should not depend on ASP.NET Core, a database provider, telemetry vendor, or deployment platform.

Infrastructure services such as cache, email, blob storage, and file parsing are adapters around systems outside the domain. Providers consume them through focused contracts. A service should not become a general container for rules that happen to be inconvenient elsewhere.

Dependency injection connects these parts at the host boundary. Convention registration reduces repeated registrations, but it does not remove the need to choose correct lifetimes or make assemblies reachable. Review [Dependency registration](guides/dependency-registration.md) before relying on discovery.

## Model use cases independently from HTTP

A provider represents an application use case. It should accept and return application contracts, coordinate domain objects and repositories, and remain usable from an HTTP controller, a worker, a scheduled process, or another adapter.

This independence keeps controllers focused on routing, binding, authorization metadata, and response translation. It also makes use cases easier to test without a web server. Protocol-specific details such as headers, route values, and status codes should be translated at the controller boundary rather than carried through the provider.

Providers may coordinate policy that depends on collaborators. Intrinsic rules about an entity's valid state belong in the domain model. The distinction is practical: if the rule can be evaluated from the entity's own state, keep it with that state; if it needs a repository, logged-user context, or remote service, coordinate it in the provider.

## Separate reads from writes when their needs differ

Write models protect behavior and consistency. Read models answer questions efficiently. The framework supports editable entity repositories and view repositories so a query can return a projection suited to its caller without forcing the write aggregate to serve every read shape.

This is a pragmatic read/write separation. It does not require full CQRS, separate stores, event-driven projections, or eventual consistency. A database view, an Entity Framework projection, or a dedicated read table can each be appropriate. Choose the simplest option that satisfies correctness, performance, and ownership needs.

When a projection is asynchronous or stored separately, its delay becomes part of the user-visible contract. Define how stale it may be, how failures are repaired, and how operators can observe lag. Paradigm.Enterprise does not create or synchronize asynchronous projections.

## Design modules around coherent capabilities

Layers organize technical responsibility. Modules organize related language, behavior, and ownership. A large application may repeat the Enterprise layer structure within several modules rather than sharing one global collection of controllers, providers, and repositories.

A useful module has a coherent vocabulary and changes for a recognizable set of reasons. Its public contracts are smaller than its implementation, and other modules do not reach into its context or repositories. Translation at a boundary is preferable to coupling two modules through persistence types that happen to look similar.

Begin with a modular application unless independent deployment solves a concrete problem. Separate services introduce network failure, versioned contracts, independent data ownership, observability, deployment coordination, and operational cost. Scaling one workload, isolating a critical failure domain, or enabling genuinely independent ownership can justify that cost. A desire for cleaner folders cannot.

Paradigm.Enterprise can be used inside a modular monolith or an independently deployed API. It does not select the deployment boundary or provide service-to-service consistency.

## Make consistency and side effects explicit

An aggregate defines which changes must remain immediately consistent in the domain. A database transaction defines which persistence operations succeed or fail together. These boundaries are related, but they are not interchangeable.

The scoped Unit of Work awaits registered commiteable objects sequentially in registration order. Without an active compatible transaction, an earlier context may already be persisted when a later context fails. Merely registering several contexts does not make their saves atomic.

An explicit compatible transaction can coordinate supported contexts that share the underlying transaction. It cannot roll back an email, cache write, blob operation, queue publication, or remote API call.

When a workflow crosses those boundaries, define its failure behavior. Idempotency can make a repeated request safe. An outbox can couple a database change with later message publication. A saga can coordinate compensating work across independent participants. These patterns add state and operational obligations, and the framework does not implement them. Introduce them only when the use case needs their guarantees.

## Prefer reversible decisions

Not every architectural choice deserves the same commitment. Keep decisions reversible while requirements are uncertain. A direct provider call is easier to change than a distributed workflow. A focused repository is easier to replace than persistence logic spread across controllers and services.

Record decisions that constrain future work, especially module boundaries, consistency models, security assumptions, public contracts, and deployment topology. A short architecture decision record should describe the context, selected option, consequences, and conditions that would trigger reconsideration.

The repository's [RFC process](https://github.com/MiracleDevs/Paradigm.Enterprise/tree/main/docs/rfc) is the canonical path for substantial framework changes. Application teams can use the same reasoning style for application-specific decisions without treating every local choice as a framework rule.

## Treat operations as part of design

A design is incomplete if the deployed system cannot explain its behavior or recover from expected failures. Decide where requests and use cases are measured, how work is correlated, which dependencies affect readiness, and what operators should do when a dependency degrades.

Keep telemetry out of domain entities. HTTP middleware can measure transport behavior, providers can measure use cases, repositories can measure database work, and infrastructure adapters can measure external calls. Logs, metrics, and traces should share correlation identifiers without recording secrets or unnecessary personal data.

Timeouts, bounded retries, circuit breakers, rate limits, and bulkheads are host or adapter policies. Apply them according to the semantics of the operation. Retrying a non-idempotent write can duplicate work, while a circuit breaker without useful health signals can hide the actual failure mode.

Read [Operations and health](guides/operations.md) for runtime guidance and [Secure delivery](guides/secure-delivery.md) for the path from reviewed source to an operable release.

## Use generation as a maintained boundary

Database scaffolding, interface generation, serializer contexts, and stored-procedure mappers reduce repetitive code. Generated output is still part of the system and must be reviewed, built, and regenerated through a reproducible process.

Do not place hand-written behavior in files that regeneration replaces. Partial classes and explicit configuration hooks are the supported customization boundary. A generation result that compiles can still expose an unintended property, change a contract, omit serializer metadata, or alter a database mapping.

Code generation reinforces conventions. It does not replace modeling, security review, schema review, or tests.
