---
name: paradigm-model-domain
description: Model Paradigm.Enterprise domain contracts, entities, views, value objects, mapping, validation, aggregates, states, and identifiers. Use when creating or changing persistence-generated entities, write behavior, read shapes, invariants, or entity/view mappings.
---

# Model the Paradigm domain

Load `$paradigm-common-guidance` before creating or editing source.

## Choose the model

- Use a behavior-rich entity/aggregate for writes that protect invariants.
- Use a view/projection for caller-specific reads.
- Use a small immutable record/class for a value with validated meaning. Configure EF mapping explicitly.
- Use a database-generated mutable shape only as a persistence exception; add behavior and invariants in a non-generated partial or handwritten model.

Keep Domain independent of ASP.NET Core, concrete databases, and infrastructure.

## Carry identity consistently

Choose a value-type `TId` implementing `IEquatable<TId>` and use it through `IEntity<TId>`, entity/view bases, repository contracts/bases, provider contracts/bases, and controller bases. Default numeric/Guid values are treated as new identifiers.

Before choosing a base or mapping signature, query the installed packages:

```powershell
dotnet tool run paradigm api search IEntity --project <solution> --limit 10
dotnet tool run paradigm api show EntityBase --project <solution>
```

## Put rules in the right place

- Give handwritten entity state private or protected setters by default.
- Expose intention-revealing behavior such as `Activate`, `Deactivate`, `Rename`, or `ChangeAddress`; do not let callers assemble transitions through property assignment.
- In entity-owned `MapFrom`, call that behavior for transition-sensitive state instead of assigning fields directly.
- Make `MapFrom` atomic for tracked entities: prevalidate failure-prone input before mutation, and reload/detach a tracked entity after a failed partial map rather than continuing with dirty state.
- Keep shared entity interfaces getter-only.
- Keep public setters on request/view models or a documented generated persistence exception only.
- Put every rule decidable from an entity or view's complete proposed state in that type's co-located partial behavior and `Validate`; normalize and prevalidate before mutation. Do not create broad static validation owners for rules that belong to individual objects.
- Keep query-shape and transport-only rules, such as paging and sort allow-lists, on the request type.
- Put checks needing repositories, identity, or remote services in a Provider.
- Keep aggregate children reachable and mutated through the aggregate root.
- Do not resolve repositories or services inside an entity, mapping method, or validation method.
- Keep write commands separate from read views when callers must not set every persisted field. Treat row-version/concurrency values as explicit protocol state and reject stale writes.
- When database catalogs are shared across applications and an Interfaces project exists, place their explicitly numbered enums there with stable serialized codes. Test the complete enum member set against the complete seed ID/code set in both directions.

Use `DomainValidator` to collect meaningful failures. Validate after mapping and before staging persistence.

## Respect generation boundaries

Inspect headers before editing. EF/T4 entities and contexts, analyzer interfaces, mapper output, and serializer contexts may be replaced. Use partial classes/configuration hooks or update the source/template. Do not hand-edit generated interfaces.

Read [model patterns](references/model-patterns.md) only when choosing between generated shapes, partial behavior, and separate read/write models.

## Test

Construct domain objects directly. Cover valid behavior, rejected transitions, boundary values, mapping, and validation without EF, ASP.NET, or the full container.
