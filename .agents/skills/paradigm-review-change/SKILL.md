---
name: paradigm-review-change
description: Review a Paradigm.Enterprise change for framework API correctness, layer ownership, discovery conventions, identifiers, security, transactions, generated boundaries, tests, and token-efficient fixes. Use for pull requests, implementation audits, build failures, or pre-release validation.
---

# Review a Paradigm change

Read and enforce [Paradigm Good Coding Practices](../../references/good-coding-practices.md) for handwritten code and documented generated-code exceptions. For new or reorganized solutions, also enforce [Paradigm Solution Layout](../../references/solution-layout.md).

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

When the change contains an AppHost, also run `./start.sh doctor`, `dotnet tool run aspire restore`, and inspect the starter's local-tool/Docker checks, resource graph, waits, root `.env` handling, secret parameters, health endpoints, and finite database bootstrap. For SQL Server, reject host SQLCMD/SqlPackage prerequisites: require a Dockerfile-backed finite bootstrap whose image builds the DACPAC and owns both tools. Use `$paradigm-setup-aspire` for the Paradigm-specific review.

When the change contains a `.sqlproj` or DbPublisher config, run:

```powershell
dotnet tool run paradigm database validate --project <database-project> --solution <solution>
```

Use `--strict` for a new project. Read [Paradigm Database Practices](../../references/database-practices.md) and use `$paradigm-build-database` for engine-specific review.

When solution metadata changes, verify the canonical responsibility folders, unique membership, curated solution items, one solution source of truth, and the separate managed-application/database validation boundaries from Paradigm Solution Layout. Do not demand physical project moves solely to mirror solution folders.

Run the built-in C# checks against consuming application projects. Framework source and test internals may intentionally exercise patterns those checks reject. Apply the repository's review policy to warnings even when the CLI exits `0`.

Use `dotnet tool run paradigm api show/search` only to resolve an exact signature; do not load broad API documentation.

## Review in risk order

1. Security: inherited anonymous metadata, authorization per action, browser cookie/token storage and CSRF, exposure versus authorization, secret/error disclosure, request limits. For Entra bearer APIs, verify the delegated-user/service-principal actor decision before user provisioning; reject `oid`/`sub` comparison as a token-type discriminator and require `idtyp` or the documented `scp`/`roles` fallback.
2. Correctness: invariants, identifier consistency, nullability, query bounds/order, stored-procedure result ordering, assigned system IDs and complete bidirectional enum/seed ID-and-code parity, status-transition history, pre-pre/DACPAC ordering, database bootstrap idempotency, baseline/import rules, transaction and external-side-effect claims.
3. Architecture: HTTP only in controllers, orchestration in Providers, invariants in Domain, persistence only in repositories, inward references.
4. Conventions: one semantic type per file, required member regions/order, least visibility, immutability, meaningful folders, public discoverable types only where required, exact `I{ConcreteName}` interfaces, marker inheritance, reachable assemblies, correct lifetimes.
5. Generation: no edits to replaceable output; generated source has an early ownership marker and generated types have compiled `GeneratedCodeAttribute` metadata when assembly checks are used; JSON/request/response metadata complete. Require official template provenance, a documented reusable compatibility delta, and no application type-name whitelist in T4. For multiple EFPT configs, verify selections and generated CLR ownership are disjoint and complete. When generated and partial files share a directory, require exact-file cleanup/recovery rather than recursive directory replacement. When a generated routine result changes, reject parallel handwritten mappers and stale result models/registrations; verify the atomic generator is byte-stable across a second run.
6. Operations: cancellation, standard trace correlation, structured logs, metrics, liveness/readiness, dependency health, Aspire wait ordering, retry/idempotency, secret-free generated deployment artifacts.
7. Tests: behavior and failure paths at the cheapest effective boundary. Exercise every required production identity/CORS setting by starting the real host with that setting absent, and prove the fully configured startup path without relying on live identity metadata.

For database source, reject any view object/file whose name does not end in `View`, including PostgreSQL materialized views. Helper/reporting views need not be `{Entity}View`, and whether an internal table needs a view remains a concrete-consumer decision.

Reject repository contracts/public members exposing `IQueryable`, handwritten SQL query/command text or raw-SQL APIs in production repositories, SQL assigned to repository fields/properties after declaration, EF pagination where a typed stored-procedure boundary is required, public setters on handwritten entity state, and state mutation performed outside entity behavior. Require a clean `PE3107` result or review every exact, justified, unexpired suppression. When reviewing the check itself, require post-generator compilation-tree coverage with a real source-generator fixture, conditional-access fixtures, runtime command arguments, type-based parameter classification independent of foldable contents, negative controls, and an exact ordered diagnostic manifest; a checked-in `.g.cs` fixture alone is insufficient. Allow SQL in database projects, deployment/bootstrap code, and explicit database tests; typed routine names and wrappers are not handwritten SQL. Exercise inherited typed-provider operations through their interfaces; class ancestry alone does not prove search is implemented or mutations preserve validation, concurrency, audit, and transaction policy. Verify positive behavior tests, not only property mapping.

Reject broad static validation classes when their rules are decidable from individual entity/view state; require co-located partial behavior, complete proposed-state prevalidation, and direct Domain tests. Keep query/transport rules on request types and repository/identity/remote checks in Providers. When a generated view is canonical, reject parallel public DTOs and private routine result rows; an exception must name the external compatibility contract, map from canonical views, include tests, and document its removal trigger.

Flag search actions that construct one request from a long primitive `[FromQuery]` list when the related values can bind directly to that request. Verify the refactor preserves flat query names/defaults and OpenAPI/client compatibility. Flag Providers that permanently inherit a generic edit contract while overriding every inherited mutation only to fail closed; require the smallest honest contract unless the generic base can actually preserve the reviewed command, concurrency, audit, overposting, bulk, cancellation, and transaction protocol.

For dependency/security review also run `paradigm packages audit --project <solution>`. Treat vulnerabilities, mixed Paradigm package versions, expired suppressions, and incomplete audits as errors. Report stable direct updates and deprecations as warnings unless policy promotes them. For a proposed new NuGet package, verify its open-source license and repository, prefer MIT when comparable, explain alternatives and risk, and confirm that the user approved it before the project changed.

Framework discovery and metadata-only tooling may use reviewed reflection. Flag reflection in application/runtime business code when it replaces explicit contracts.

## Report

Lead with actionable findings ordered by severity. Include file/line, observed behavior, consequence, and smallest safe correction. Separate verified defects from questions and optional improvements. If no defect is found, state remaining test or environment gaps.
