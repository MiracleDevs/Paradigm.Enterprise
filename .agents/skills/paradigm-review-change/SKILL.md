---
name: paradigm-review-change
description: Review a Paradigm.Enterprise change for framework API correctness, layer ownership, discovery conventions, identifiers, security, transactions, generated boundaries, tests, and token-efficient fixes. Use for pull requests, implementation audits, build failures, or pre-release validation.
---

# Review a Paradigm change

## Gather evidence

Inspect the diff, project graph, generated headers, tests, and host configuration. Run:

```powershell
dotnet restore <solution>
dotnet build <solution>
dotnet test <solution>
dotnet tool run paradigm doctor --project <solution>
dotnet tool run paradigm validate --project <solution>
```

Use `dotnet tool run paradigm api show/search` only to resolve an exact signature; do not load broad API documentation.

## Review in risk order

1. Security: inherited anonymous metadata, authorization per action, exposure versus authorization, secret/error disclosure, request limits.
2. Correctness: invariants, identifier consistency, nullability, query bounds/order, transaction and external-side-effect claims.
3. Architecture: HTTP only in controllers, orchestration in Providers, invariants in Domain, persistence only in repositories, inward references.
4. Conventions: public concrete types, exact `I{ConcreteName}` interfaces, marker inheritance, reachable assemblies, correct lifetimes.
5. Generation: no edits to replaceable output; JSON/request/response metadata complete.
6. Operations: cancellation, correlation, useful telemetry, dependency health, retry/idempotency.
7. Tests: behavior and failure paths at the cheapest effective boundary.

Framework discovery and metadata-only tooling may use reviewed reflection. Flag reflection in application/runtime business code when it replaces explicit contracts.

## Report

Lead with actionable findings ordered by severity. Include file/line, observed behavior, consequence, and smallest safe correction. Separate verified defects from questions and optional improvements. If no defect is found, state remaining test or environment gaps.
