---
name: paradigm-model-domain
description: Model Paradigm.Enterprise domain contracts, entities, views, value objects, mapping, validation, aggregates, states, and identifiers. Use when creating or changing persistence-generated entities, write behavior, read shapes, invariants, or entity/view mappings.
---

# Model the Paradigm domain

## Choose the model

- Use a behavior-rich entity/aggregate for writes that protect invariants.
- Use a view/projection for caller-specific reads.
- Use a small immutable record/class for a value with validated meaning. Configure EF mapping explicitly.
- Use a database-generated mutable shape only as a persistence exception; add behavior and invariants in a non-generated partial or handwritten model.

Keep Domain independent of ASP.NET Core, concrete databases, and infrastructure.

## Carry identity consistently

Choose a value-type `TId` implementing `IEquatable<TId>` and use it through `IEntity<TId>`, entity/view bases, repository contracts/bases, provider contracts/bases, and controller bases. Default numeric/Guid values are treated as new identifiers.

Before choosing a base or mapping signature, query the installed version:

```powershell
dotnet tool run paradigm api search IEntity --project <solution> --limit 10
dotnet tool run paradigm api show EntityBase --project <solution>
```

Older Paradigm versions may use fixed `int` identifiers and non-generic contracts. Preserve that installed-version model during maintenance; do not force a generic migration unless it is explicitly in scope.

## Put rules in the right place

- Put a rule decidable from the entity's state in entity behavior and `Validate`.
- Put checks needing repositories, identity, or remote services in a Provider.
- Keep aggregate children reachable and mutated through the aggregate root.
- Do not resolve repositories or services inside an entity, mapping method, or validation method.
- Keep write commands separate from read views when callers must not set every persisted field. Treat row-version/concurrency values as explicit protocol state and reject stale writes.

Use `DomainValidator` to collect meaningful failures. Validate after mapping and before staging persistence.

## Respect generation boundaries

Inspect headers before editing. EF/T4 entities and contexts, analyzer interfaces, mapper output, and serializer contexts may be replaced. Use partial classes/configuration hooks or update the source/template. Do not hand-edit generated interfaces.

Read [model patterns](references/model-patterns.md) only when choosing between generated shapes, partial behavior, and separate read/write models.

## Test

Construct domain objects directly. Cover valid behavior, rejected transitions, boundary values, mapping, and validation without EF, ASP.NET, or the full container.
