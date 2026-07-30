---
name: paradigm-design-application
description: Design a Paradigm.Enterprise application, module, or major workflow with explicit layer ownership, capability boundaries, deployment choices, consistency, side effects, security, and operations. Use before scaffolding a solution, splitting modules or services, or implementing a cross-cutting feature.
---

# Design a Paradigm application

## Start from decisions

Capture:

- capabilities, vocabulary, owners, users, and sensitive assets;
- use cases and HTTP/non-HTTP entry points;
- aggregate invariants and transaction boundaries;
- read shapes, freshness, scale, and query ownership;
- external side effects and failure/retry behavior;
- authentication, per-action authorization, audit, and data exposure;
- deployment, observability, health, recovery, and compatibility needs.

Do not turn uncertain requirements into framework conventions.

## Assign responsibility

Use this dependency flow:

```text
WebApi -> Providers -> Data -> Domain -> Interfaces
```

- Controllers: transport, binding, authorization metadata, response translation.
- Providers: application use cases, collaborator coordination, transaction/commit decisions.
- Domain: behavior and invariants decidable from domain state; aggregate boundaries.
- Repositories/Data: persistence and query mechanics, never permission or business decisions.
- Services: focused external-system adapters, consumed by Providers.

Organize related layers by coherent capability when the application grows. Do not share a module's repositories or context with another module; cross through a focused contract.

## Choose the smallest sufficient topology

Begin with a modular monolith unless independent deployment solves ownership, isolation, scaling, or lifecycle needs. A service boundary adds network failure, contract versioning, data ownership, delivery coordination, and operational cost.

Use separate read shapes when query needs differ from write behavior. Do not introduce asynchronous projections unless acceptable staleness, repair, and lag monitoring are defined.

## Define consistency and delivery

The Unit of Work stages and sequentially commits registered contexts; several contexts are not automatically atomic. Use a compatible explicit database transaction when required. A transaction cannot roll back email, cache, blob, queue, or remote calls.

For reliable external work, decide explicitly between immediate best-effort delivery, idempotent retry, an application-owned outbox, or persisted orchestration/compensation. Paradigm supplies none of the latter patterns.

Read [decision checklist](references/decision-checklist.md) when producing an architecture proposal or ADR.

## Validate the design

Reject designs where HTTP types enter Providers, repositories make business decisions, entities resolve services, lower layers reference the host, protected routes inherit anonymous controller metadata, or external delivery is claimed atomic with a database commit.
