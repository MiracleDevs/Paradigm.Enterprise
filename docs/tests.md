# Testing applications

The framework's base classes remove repetitive code but do not remove the need to test application behavior. Tests should focus on domain invariants, provider workflows, persistence contracts, HTTP policy, and configuration conventions.

## Domain tests

Domain tests should construct entities directly, invoke behavior, and verify both valid state and rejected transitions. These tests do not need Entity Framework, HTTP, or dependency injection.

Test `Validate` with meaningful combinations rather than one assertion per property. For aggregate roots, verify that callers cannot leave child collections in an invalid state.

## Provider tests

Provider tests replace repository and infrastructure contracts with test doubles, register them in a small `ServiceCollection`, and exercise the use case through its provider interface. Verify mapping, validation, lifecycle hooks, commit count, and rollback behavior.

When testing a generic edit provider, remember that a successful single save reads the resulting view through the view repository. Configure that result instead of asserting only that the edit repository was called.

## Repository tests

Use a relational database compatible with production semantics when testing provider-specific queries, transactions, constraints, or stored procedures. Entity Framework's in-memory provider is useful for simple collaboration tests, but it does not reproduce relational behavior.

Test custom search functions with empty results, paging boundaries, invalid filter values, and realistic data volume. Test generated stored-procedure mappers against each result-set shape they consume.

## API tests

Use an ASP.NET Core test host to verify route shape, model binding, serialization contexts, exception translation, middleware order, authentication, authorization, and endpoint exposure.

Include negative cases. For protected custom controllers, confirm that anonymous and underprivileged callers are rejected. Test library-based controllers as intentionally anonymous because their inherited `AllowAnonymous` metadata bypasses `Authorize` and fallback policies. Also confirm that hidden endpoints return not found only when exposure control is enabled, invalid domain state produces the intended safe response, and missing resources map consistently.

## Running this repository

Run the library test project through the solution:

```powershell
dotnet test src/Paradigm.Enterprise.slnx
```

The checked-in example solution is pinned to an older package line and serves as a compatibility sample. Its successful build does not validate current source APIs.
