# Paradigm Aspire libraries

Use these packages only in the projects that compose or consume the relevant capability. They are reusable application libraries, not a substitute for deciding an application's topology, production deployment model, security policy, or schema publication authorization.

## AppHost composition

`Paradigm.Enterprise.Aspire.Hosting` owns common local-resource recipes. Start an AppHost with `ParadigmDistributedApplication.CreateBuilder(args)` so the repository-root `.env` loader runs before Aspire builds configuration. Do not separately load or overwrite `.env` values.

Use the following extension families with their option objects:

| Need | API | Consumer behavior |
| --- | --- | --- |
| SQL Server | `AddParadigmSqlServerDatabase(ParadigmSqlServerDatabaseOptions)` | Its `ParadigmSqlServerDatabaseReference` exposes managed `Server`, `Database`, and `Password` only in managed mode; attach it with `WithParadigmInfrastructure`. |
| PostgreSQL | `AddParadigmPostgreSqlDatabase(ParadigmPostgreSqlDatabaseOptions)` | Attach its reference with `WithParadigmInfrastructure`. |
| Redis | `AddParadigmRedis(ParadigmRedisOptions)` | Attach with `WithParadigmInfrastructure`. |
| Azurite blobs | `AddParadigmBlobStorage(ParadigmBlobStorageOptions)` | Attach with `WithParadigmInfrastructure`. |
| Angular dev client | `AddParadigmAngularClient(ParadigmAngularClientOptions, apis)` | References supplied API projects and can wait for them; expose its endpoint only when browser access is intended. |

For database, Redis, and blob options, choose `Managed` only for locally owned infrastructure. Use `External` for a supplied connection string; it creates a connection-string resource and must not create a container, persistent volume, or automatic database import. Keep the connection name deliberate: it becomes the consumer's `ConnectionStrings__<name>` key. The database option defaults are `DatabaseConnection`, while Redis and blob defaults are `RedisCacheConnection` and `BlobStorageConnection`.

`WithParadigmInfrastructure` configures references and their local waits on a project. Use `WithOptionalSettings` only for non-secret values that are already configured, and `WithOptionalSecrets` for optional values that must become secret Aspire parameters. Never pass a raw secret through `WithOptionalSettings` or hand-copy a connection string to a managed consumer.

## Database bootstrap composition

For managed SQL Server, the SQL Server reference exposes the exact managed resources required by `AddSqlServerDatabaseBootstrap`. The helper creates a Dockerfile resource, references and waits for the database, passes the secret password as `SA_PASSWORD`, and preserves the server relationship. The API must wait for that finite resource with `WaitForCompletion`.

For managed PostgreSQL, call `AddPostgreSqlDatabaseBootstrap` with the managed database and server resources. The helper maps the database to `Paradigm_ORM_ConnectionString` by default. The API must wait for its finite resource with `WaitForCompletion`.

Neither helper supports external mode because an external database has no managed server/database resource. In that mode, require an explicit publication opt-in and compose only the reviewed external bootstrap path described in the AppHost patterns.

## Hosted-service defaults

`Paradigm.Enterprise.Aspire.ServiceDefaults` provides `AddParadigmServiceDefaults()` and `MapParadigmServiceDefaultsEndpoints()`.

`AddParadigmServiceDefaults()` registers OpenTelemetry logs, metrics, and tracing with OTLP exporters; runtime, ASP.NET Core, and HTTP instrumentation; service discovery; standard HTTP resilience; and a `live` self health check. `MapParadigmServiceDefaultsEndpoints()` maps that self-only liveness check to `/alive`.

Call each method once. Do not add duplicate standard OpenTelemetry exporters, `AddServiceDiscovery`, or `ConfigureHttpClientDefaults(...AddStandardResilienceHandler())`. Add readiness (`/health` by default) in the service host and ensure it reflects dependencies that the service actually needs; never turn liveness into dependency health.
