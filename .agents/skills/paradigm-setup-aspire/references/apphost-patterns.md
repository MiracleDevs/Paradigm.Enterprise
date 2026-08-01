# Paradigm AppHost patterns

## Root configuration

Resolve the repository root from rooted `aspire.config.json`, not only the current directory. Load `<root>/.env` into the current process before creating the AppHost builder and never replace variables already supplied by the shell or CI.

Parse UTF-8 lines conservatively: ignore blank lines and full-line comments, allow an optional `export ` prefix, split on the first `=`, remove matching single or double quotes, and reject malformed or duplicate keys. Do not expand variables or execute the file as a shell script.

Use these keys:

| Key | Meaning |
| --- | --- |
| `Database__Provider` | `SqlServer` or `PostgreSql` |
| `Database__Mode` | `Managed` or `External` |
| `Database__Name` | Logical and physical database name |
| `Database__Password` | Secret password for a managed local server |
| `Database__PublishOnStart` | Explicit schema-publish switch |
| `ConnectionStrings__DatabaseConnection` | Required only for external mode |

Treat process variables as highest precedence, `.env` as a local fallback, and checked-in appsettings as non-secret defaults. Represent passwords and external connection strings as secret Aspire parameters.

Parse `Database__Provider` and `Database__Mode` as closed, case-insensitive enums and `Database__PublishOnStart` only as `true` or `false`; fail before creating resources on missing or invalid required values. Require a nonblank database name. In managed mode require the password through a secret parameter. In external mode reject a missing connection string and keep publication off unless its opt-in is explicitly `true`.

Use the current Aspire CLI to generate and validate `aspire.config.json`; treat its rooted location as the repository marker rather than hand-authoring a stale schema. Before accepting the configuration, verify `.env` is ignored and is not tracked by Git. `.env.example` must contain placeholders only.

## Project structure

Keep:

```text
aspire.config.json
.env.example
start.sh
src/
  Product.AppHost/
  Product.ServiceDefaults/
  Product.DatabaseBootstrap/
  database/
```

Reference ServiceDefaults from each hosted .NET service. Add `AddServiceDefaults()` before building the app and map the standard endpoints after middleware configuration. Keep readiness dependency-aware and liveness process-only.

Keep `start.sh` at this root and make it a thin, noninteractive Aspire wrapper after prerequisite checks. Its `start` command runs Aspire in the foreground; `stop` uses the explicit AppHost project path so it cannot select another checkout's process. The script may install missing repository-local development tools before AppHost starts; AppHost resources and database bootstrap executables must never install tools.

Use `/health` for readiness and `/alive` for liveness unless the existing solution has a documented compatible contract. Do not expose dependency details or configuration values in either response.

## Managed databases

Use `AddSqlServer(...).WithLifetime(ContainerLifetime.Persistent).WithDataVolume()` or the PostgreSQL equivalent, then `AddDatabase`. Pass the database resource to the API and bootstrap with `WithReference`; do not override the generated connection string from `.env`.

Keep Aspire resource names lowercase. Name or explicitly map the database connection resource so consumers bind the canonical `ConnectionStrings:DatabaseConnection` key; do not assume the physical database name produces that alias. Verify the generated `ConnectionStrings__...` environment variable in `aspire describe`.

For SQL Server, model this graph:

```text
sql server -> database -> database bootstrap -> API
```

The bootstrap must:

1. wait until the database accepts connections;
2. conservatively prove the database is empty: require no non-system objects in `sys.objects`, no user-defined entries in `sys.types`, and no user-owned schemas beyond the expected empty-database defaults; abort the import when the result is uncertain;
3. import `src/database/bootstrap/<Database>.bacpac` only when the database is empty and the file exists;
4. build the SDK-style SQL project into a known repository artifact directory and require exactly the expected `<Project>.dacpac`;
5. execute `scripts/prepredeployment/PrePreDeployment.sql` idempotently with a SQLCMD-compatible batch runner and fail on error;
6. generate the SqlPackage plan and publish the resulting DACPAC with the complete target connection string;
7. probe an expected schema object and exit nonzero on failure.

SqlPackage [imports a BACPAC into a new or empty database](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-import?view=sql-server-ver17); the bootstrap must enforce that precondition rather than using import as a reset mechanism.

Keep SqlPackage pinned in the repository-local tool manifest and restore it before AppHost starts. Never install it at runtime. Avoid disabling data-loss blocking by default; require explicit approval for a reviewed exceptional publish profile.

Give the finite bootstrap a clear contract: connection string from the Aspire reference, repository-rooted project/baseline paths, bounded connection retries and command timeouts, host cancellation, structured secret-free logs, and nonzero exit for build/import/publish/probe failure. Default local readiness to a two-minute overall deadline and each import/publish process to a fifteen-minute deadline; make non-secret timeout overrides explicit and never retry forever. Select a stable required table or schema object from the database project as the post-publish probe; do not use "connection succeeded" as proof that publication succeeded.

Pre-pre-deployment is not DACPAC `PreDeploy`: compile and verify the DACPAC first, then finish pre-pre before SqlPackage generates its plan. Use a SQLCMD-compatible executor with `GO`, reviewed include/variable behavior, bounded timeouts, cancellation, nonzero failure propagation, and secret-free logs; a naive `SqlCommand` over the complete file is invalid. Run it only when schema publication is enabled, including the explicit external-mode opt-in, and never use it to bypass unreviewed data-loss protection.

For PostgreSQL, map the Aspire database resource explicitly with `.WithReference(database).WithEnvironment("Paradigm_ORM_ConnectionString", database)`; `WithReference` alone only creates the conventional `ConnectionStrings__<resource-name>` variable. Pin the verified DbPublisher executable or container in repository-owned configuration. Because DbPublisher behavior varies by installed version, verify nonzero failure propagation and follow execution with a schema probe before marking the bootstrap complete.

## External databases

Use an Aspire connection-string resource instead of adding a local container. Default `Database__PublishOnStart` to `false`; require an explicit opt-in before the bootstrap can change the external database. Disable BACPAC import completely in this mode.

## Resource discipline

- Use stable lowercase resource names.
- Use `WaitFor` for long-lived dependencies and `WaitForCompletion` for finite bootstrap tasks.
- Expose only developer-facing endpoints that need browser access.
- Keep generated service references as the sole connection-string source for managed resources.
- Do not model email, cache, queue, or remote calls as transactionally atomic with database publication.
