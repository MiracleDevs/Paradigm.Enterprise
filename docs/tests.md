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
dotnet test src/Paradigm.Enterprise.slnx --filter "TestCategory!=Integration"
```

This is the fast developer loop. Run `bash build/quality.sh` for the complete repository suite, including integration tests that consume the prepared CLI package and installed executable.

MySQL tests have their own explicit database run. Set `PARADIGM_MYSQL_TEST_CONNECTION_STRING` to a disposable MySQL 8.4/InnoDB server with permission to create and drop fixture databases, then run:

```powershell
dotnet test src/Paradigm.Enterprise.Data.MySql.Tests/Paradigm.Enterprise.Data.MySql.Tests.csproj --configuration Release --filter "TestCategory=Integration"
```

The integration fixture creates an isolated database and removes it afterward. Use synthetic data and a dedicated test server. Without a connection string, manual runs report database tests as skipped (inconclusive) with a setup message; they never fall back to application configuration. Set `PARADIGM_REQUIRE_DATABASE_TESTS=true` to fail instead when database configuration is missing. A supplied connection string always opts in: invalid configuration, unreachable databases, missing privileges, and test failures remain failures. Database integration tests run manually; there is no dedicated MySQL CI job. The fast solution filter and packed CLI integration command do not execute these database tests; run the command above separately from `build/quality.sh` for full MySQL validation. See [Database providers](data-providers.md) for setup and mapping boundaries.

Oracle Database uses a separate explicit run. Set `PARADIGM_ORACLE_TEST_CONNECTION_STRING` to an administrator connection within a disposable PDB, for example SYSTEM on `host:1521/FREEPDB1`, with permission to create/drop fixture users and grant their session, table, procedure, and tablespace quota privileges. Never point it at a production database. The fixture creates a uniquely named schema/user and drops only that schema during cleanup.

```powershell
dotnet test src/Paradigm.Enterprise.Data.Oracle.Tests/Paradigm.Enterprise.Data.Oracle.Tests.csproj --configuration Release --filter "TestCategory=Integration"
dotnet test src/Paradigm.Enterprise.Cli.Tests/Paradigm.Enterprise.Cli.Tests.csproj --configuration Release --filter "TestCategory=OracleIntegration"
```

The second command compiles generated Oracle mappers and exercises their parameters and reader conversions against actual ODP.NET results. Both commands use the same connection variable and report skipped (inconclusive) when it is absent during manual runs. Neither command silently reads application settings or skips a configured database failure. There is no dedicated Oracle CI job; supply the connection string and set `PARADIGM_REQUIRE_DATABASE_TESTS=true` when deliberately qualifying these tests. This fixture targets Oracle AI Database Free 26ai; package compatibility alone does not certify older servers or production editions. Run both commands separately from the fast loop and `build/quality.sh` to qualify Oracle changes.

For example, after starting disposable local database servers, configure the test process before running either project:

```powershell
$env:PARADIGM_MYSQL_TEST_CONNECTION_STRING = "Server=127.0.0.1;Port=3306;User ID=root;Password=<test-password>;SslMode=Disabled;AllowPublicKeyRetrieval=True"
$env:PARADIGM_ORACLE_TEST_CONNECTION_STRING = "Data Source=127.0.0.1:1521/FREEPDB1;User ID=SYSTEM;Password=<test-password>;Pooling=false"
$env:PARADIGM_REQUIRE_DATABASE_TESTS = "true"
```

Use actual mapped ports and dedicated test credentials. For Visual Studio Test Explorer, start Visual Studio from the configured shell or provide the variables through your local test run settings so the test process inherits them. An unconfigured project run executes its offline tests and reports the database tests as skipped; that result does not certify database integration.

When running all tests directly with `dotnet test` or Visual Studio Test Explorer, the packed CLI tests are reported as skipped (inconclusive) if neither `PARADIGM_CLI_INTEGRATION_PACKAGE` nor `PARADIGM_CLI_INTEGRATION_EXECUTABLE` is configured. The quality script and PR workflow supply these paths and execute the tests. Once either variable is configured, missing required paths, relative paths, and nonexistent files still fail the tests.

The checked-in example solution references the current source projects. Build and test it separately to validate the documented application patterns as well as the framework solution:

```powershell
dotnet test example/ExampleApp.sln
dotnet run --project src/Paradigm.Enterprise.Cli -- checks run --project example/ExampleApp.sln
```
