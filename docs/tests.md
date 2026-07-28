# Testing applications

The framework's base classes remove repetitive code but do not remove the need to test application behavior. Tests should focus on domain invariants, provider workflows, persistence contracts, HTTP policy, and configuration conventions.

Use the smallest test boundary that can prove the behavior. Fast domain tests give precise feedback about rules. Provider tests verify orchestration. Relational integration tests verify mappings and transaction semantics. Host tests verify the assembled HTTP and security pipeline. A smaller test is not automatically better when it replaces the infrastructure behavior that the test is meant to validate.

## Domain tests

Domain tests should construct entities directly, invoke behavior, and verify both valid state and rejected transitions. These tests do not need Entity Framework, HTTP, or dependency injection.

Test `Validate` with meaningful combinations rather than one assertion per property. For aggregate roots, verify that callers cannot leave child collections in an invalid state.

Test intention-revealing methods and value-object construction directly. Verify allowed and rejected state transitions, and confirm that a failed transition does not leave partial state changes. When an aggregate uses `DomainTracker`, verify the manual added, edited, and removed entries as well as the visible collection.

## Provider tests

Provider tests replace repository and infrastructure contracts with test doubles, register them in a small `ServiceCollection`, and exercise the use case through its provider interface. Verify mapping, validation, lifecycle hooks, commit count, and rollback behavior.

When testing a generic edit provider, remember that a successful single save reads the resulting view through the view repository. Configure that result instead of asserting only that the edit repository was called.

## Repository tests

Use a relational database compatible with production semantics when testing provider-specific queries, transactions, constraints, or stored procedures. Entity Framework's in-memory provider is useful for simple collaboration tests, but it does not reproduce relational behavior.

Test custom search functions with empty results, paging boundaries, invalid filter values, and realistic data volume. Test generated stored-procedure mappers against each result-set shape they consume.

## API tests

Use an ASP.NET Core test host to verify route shape, model binding, serialization contexts, exception translation, middleware order, authentication, authorization, and endpoint exposure.

Include negative cases. For protected custom controllers, confirm that anonymous and underprivileged callers are rejected. Test library-based controllers as intentionally anonymous because their inherited `AllowAnonymous` metadata bypasses `Authorize` and fallback policies. Also confirm that hidden endpoints return not found only when exposure control is enabled, invalid domain state produces the intended safe response, and missing resources map consistently.

Threat-driven tests should also cover malformed and oversized input, unexpected content types, replay or duplicate requests where relevant, overexposed response fields, and safe error output. A successful authorized request does not prove that another caller, tenant, or role is correctly denied.

## Delivery and operational tests

Build the deployable artifact from a clean checkout and test the same configuration shape used by the deployment pipeline. Validate source-generated serialization metadata, startup configuration, dependency registration, database migrations, and health behavior before promotion.

Contract tests are useful for independently deployed callers or dependencies. Resilience tests can verify timeout, cancellation, retry, and degraded behavior when those policies are part of the host. Performance tests should use realistic query shapes and data volume when latency or throughput is an architectural requirement.

Backup restoration, deployment rollback, and disaster recovery are platform exercises rather than library unit tests. They still form part of the evidence required for a service whose recovery objectives depend on them. See [Secure delivery](guides/secure-delivery.md).

## Running this repository

Run the library test project through the solution:

```powershell
dotnet test src/Paradigm.Enterprise.slnx
```

The checked-in example solution is pinned to an older package line and serves as a compatibility sample. Its successful build does not validate current source APIs.
