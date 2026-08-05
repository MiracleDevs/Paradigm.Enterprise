---
name: paradigm-review-change
description: Review a Paradigm.Enterprise change for framework API correctness, layer ownership, discovery conventions, identifiers, security, transactions, generated boundaries, tests, and token-efficient fixes. Use for pull requests, implementation audits, build failures, or pre-release validation.
---

# Review a Paradigm change

Read and enforce [Paradigm Good Coding Practices](../../references/good-coding-practices.md) for handwritten code and documented generated-code exceptions.

## Gather evidence

Inspect the diff, project graph, generated headers, tests, and host configuration. Run:

```powershell
dotnet restore <solution>
dotnet build <solution>
dotnet test <solution>
dotnet tool run paradigm doctor --project <solution>
dotnet tool run paradigm packages check --project <solution>
dotnet tool run paradigm validate --project <solution>
dotnet tool run paradigm checks run --project <solution>
```

Run the built-in C# checks against consuming application projects. Framework source and test internals may intentionally exercise patterns those checks reject. Apply the repository's review policy to warnings even when the CLI exits `0`.

Use `dotnet tool run paradigm api show/search` only to resolve an exact signature; do not load broad API documentation.

## Review in risk order

1. Security: inherited anonymous metadata, authorization per action, browser cookie/token storage and CSRF, exposure versus authorization, secret/error disclosure, request limits.
2. Correctness: invariants, identifier consistency, nullability, query bounds/order, stored-procedure result ordering, transaction and external-side-effect claims.
3. Architecture: HTTP only in controllers, orchestration in Providers, invariants in Domain, persistence only in repositories, inward references.
4. Conventions: one semantic type per file, required member regions/order, least visibility, immutability, meaningful folders, public discoverable types only where required, exact `I{ConcreteName}` interfaces, marker inheritance, reachable assemblies, correct lifetimes.
5. Generation: no edits to replaceable output; JSON/request/response metadata complete.
6. Operations: cancellation, standard trace correlation, structured logs, metrics, liveness/readiness, dependency health, retry/idempotency.
7. Tests: behavior and failure paths at the cheapest effective boundary.

Reject repository contracts/public members exposing `IQueryable`, EF pagination where a stored-procedure boundary is required, public setters on handwritten entity state, and state mutation performed outside entity behavior. Verify positive behavior tests, not only property mapping.

For dependency/security review also run `paradigm packages audit --project <solution>`. Treat vulnerabilities, mixed Paradigm package versions, expired suppressions, and incomplete audits as errors. Report stable direct updates and deprecations as warnings unless policy promotes them. For a proposed new NuGet package, verify its open-source license and repository, prefer MIT when comparable, explain alternatives and risk, and confirm that the user approved it before the project changed.

Framework discovery and metadata-only tooling may use reviewed reflection. Flag reflection in application/runtime business code when it replaces explicit contracts.

## Report

Lead with actionable findings ordered by severity. Include file/line, observed behavior, consequence, and smallest safe correction. Separate verified defects from questions and optional improvements. If no defect is found, state remaining test or environment gaps.
