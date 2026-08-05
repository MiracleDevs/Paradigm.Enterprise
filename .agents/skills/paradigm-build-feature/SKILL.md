---
name: paradigm-build-feature
description: Implement an end-to-end Paradigm.Enterprise feature or CRUD vertical slice across domain, repositories, providers, and Web API. Use when adding a capability, command, query, endpoint, or coordinated workflow that spans more than one framework layer.
---

# Build a Paradigm feature

Read and apply [Paradigm Good Coding Practices](../../references/good-coding-practices.md) before creating or editing source.

## Plan the slice

Before editing, inspect adjacent working code and run:

```powershell
dotnet tool run paradigm inspect --project <solution>
dotnet tool run paradigm api search <relevant-base-type> --project <solution> --limit 10
```

Define the use case, identifier type, write aggregate, read projection, authorization, transaction, external side effects, errors, and tests. Reuse the existing capability/module structure.

## Implement inward to outward

1. Model contracts and domain behavior with `paradigm-model-domain`.
2. Add repository contracts and implementations with `paradigm-build-repository`.
3. Add the application workflow with `paradigm-build-provider`.
4. Expose only required transport actions with `paradigm-build-web-api`.
5. Update the host composition root, generated JSON metadata, and explicit module assembly roots.

Carry one `TId` through entity/view interfaces, repository/provider contracts and bases, and any anonymous generic controller base.

Avoid pass-through layers: every class must own a decision or isolate a changing mechanism. Do not move HTTP choices into Providers or authorization/business policy into repositories.

Require intention-revealing entity behavior and getter-only entity contracts. Reject external entity mutation and public setters on handwritten entity state. Select stored procedures for pagination, complex filtering, multi-join/reporting queries, and multi-step database work; keep EF/LINQ for simple bounded queries. Never expose `IQueryable`.

## Handle generated code

Never edit replaceable EF/T4, analyzer, mapper, serializer, client, or stored-procedure output. Change its source/template or add a partial file. Review regeneration diffs for key, nullability, navigation, serialization, and contract changes.

## Verify

- Unit-test domain behavior, repository queries, provider orchestration, and failure paths.
- Integration-test protected endpoints with anonymous, underprivileged, valid, malformed, missing, and conflicting requests.
- Run restore, build, tests, then `dotnet tool run paradigm validate --project <solution>`.
- Run configured semantic checks against the consuming application with `dotnet tool run paradigm checks run --project <application-solution>`; framework source/test internals are not a clean consumer target.
- Apply the repository's review policy to warnings; CLI exit `0` for warnings does not make them automatically PR-acceptable.
- Cover entity behavior transitions and the selected stored-procedure result ordering with focused tests.
- Review transaction atomicity, idempotency, error disclosure, structured logs, trace propagation, metrics, and liveness/readiness health impact.
