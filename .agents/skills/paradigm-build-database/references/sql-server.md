# SQL Server database projects

Use the current [SQL database projects documentation](https://learn.microsoft.com/en-us/sql/tools/sql-database-projects/sql-database-projects?view=sql-server-ver17) to verify stable SDK and SqlPackage behavior.

## Create the project

Prefer an SDK-style `Microsoft.Build.Sql` project and the current stable template/toolchain approved by the user:

```powershell
dotnet new install Microsoft.Build.Sql.Templates
dotnet new sqlproj -n Product.Database -o src/database
dotnet sln <solution> add src/database/Product.Database.sqlproj
dotnet build src/database/Product.Database.sqlproj
```

Choose the target platform from the deployed SQL Server or Azure SQL target. Do not copy preview SDK versions from older applications. SDK-style globbing includes ordinary `.sql` object files; keep the project file minimal.

Use:

```text
tables/<Capability>/
views/<Capability>/
functions/<Capability>/
routines/<Capability>/
types/
scripts/prepredeployment/PrePreDeployment.sql
scripts/predeployment/PreDeployment.sql
scripts/postdeployment/PostDeployment.sql
scripts/postdeployment/<Capability>/*Data.sql
scripts/maintenance/
bootstrap/<Database>.bacpac   # optional, at most one
```

Exclude pre-pre-deployment, included post-deployment, and maintenance scripts from model build. Register pre-pre-deployment as a non-model bootstrap artifact, not `PreDeploy`. Register exactly one DACPAC `PreDeploy` and one `PostDeploy` root when present. Have `PostDeployment.sql` include subordinate seed files with `:r` in dependency order.

## Define schema objects

- Use explicit `[dbo]` unless another owned schema is intentional.
- Use singular PascalCase object names and one semantic object per file.
- Define views with `SCHEMABINDING` unless a reviewed cross-database or dynamic dependency prevents it.
- Use `PK_Table`, `FK_Table_ReferencedTable`, `UQ_Table_Columns`, `IX_Table_Columns`, and `DF_Table_Column`. Append the local column when several relationships target the same table.
- Give transactional/entity tables `INT IDENTITY(1,1)` identifiers by default. Give closed system/status catalogs ordinary non-identity `INT` identifiers whose values are assigned in source-controlled seed data; preserve a reviewed distributed/assigned identifier boundary.
- Do not add cascade deletion by habit. Use it only for an owned child/junction whose aggregate deletion semantics require it.

For auditable entities, use `CreatedByUserId`, `CreationDate`, `ModifiedByUserId`, and `ModificationDate` consistently. Let the Paradigm application audit owner set the values unless a database-owned default is an explicit boundary decision. Use named foreign keys to the user table.

Use `IsActive BIT NOT NULL` with a named default for disable/logical lifecycle behavior. Use a seeded status table and `StatusId` foreign key for workflow state; do not overload `IsActive` as a state machine. Pair each status catalog with a rerunnable `<StatusTable>Data.sql` and a service-side .NET enum in the boundary selected with `$paradigm-model-domain`, using identical numeric values and stable machine codes. Keep localized/display labels separate. Never delete, reuse, or renumber a published status identifier; retire it with `IsActive` so current and historical foreign keys remain valid.

For a stateful entity such as `SalesOrder`, add append-only `SalesOrderStatusHistory` with identity `Id`, `SalesOrderId`, `StatusId`, `CreatedByUserId`, and `CreationDate`. Use `NO ACTION` foreign keys from history, record the initial status when the entity is created, and update current status plus insert history through one transaction-owning workflow with optimistic concurrency. Do not infer history from the current row later or expose generic update/delete behavior for history.

## Publish safely

Commit SqlPackage to the repository-local tool manifest and restore it before AppHost starts; a running bootstrap must never install it. Build first, then use the complete target connection string:

```powershell
dotnet tool run sqlpackage /Action:Publish /SourceFile:<database.dacpac> /TargetConnectionString:<connection-string>
```

Keep destructive publish properties disabled by default. Generate and review a deployment report/script before approving possible data loss.

Execute `scripts/prepredeployment/PrePreDeployment.sql` through an explicit, idempotent pre-publish bootstrap step after optional BACPAC import and a successful DACPAC build, but before SqlPackage creates its deployment plan. Use a SQLCMD-compatible batch executor that supports `GO`, reviewed includes/variables, bounded timeouts, cancellation, nonzero failure propagation, and secret-free logs; do not feed the whole file to a naive `SqlCommand`. Merely registering it as DACPAC `PreDeploy` does not move it before plan generation. Apply the same managed/external publication gate, require human review for destructive statements, make the step visible in Aspire ordering, and fail the bootstrap when it fails.

An optional BACPAC is a developer baseline, not schema source. Store at most one under `bootstrap`, import it only when an Aspire-managed local database is empty, and always publish the current DACPAC afterward. Never regenerate it automatically or import it into an external database.
