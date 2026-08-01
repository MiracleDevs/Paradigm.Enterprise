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
scripts/predeployment/PreDeployment.sql
scripts/postdeployment/PostDeployment.sql
scripts/postdeployment/<Capability>/*Data.sql
scripts/maintenance/
bootstrap/<Database>.bacpac   # optional, at most one
```

Exclude included post-deployment and maintenance scripts from model build. Register exactly one `PreDeploy` and one `PostDeploy` root when present. Have `PostDeployment.sql` include subordinate seed files with `:r` in dependency order.

## Define schema objects

- Use explicit `[dbo]` unless another owned schema is intentional.
- Use singular PascalCase object names and one semantic object per file.
- Define views with `SCHEMABINDING` unless a reviewed cross-database or dynamic dependency prevents it.
- Use `PK_Table`, `FK_Table_ReferencedTable`, `UQ_Table_Columns`, `IX_Table_Columns`, and `DF_Table_Column`. Append the local column when several relationships target the same table.
- Default entity identifiers to `INT IDENTITY(1,1)` when the domain does not require assigned or distributed identifiers. Use stable assigned integer identifiers for closed seeded catalogs; preserve a domain decision to use `UNIQUEIDENTIFIER`.
- Do not add cascade deletion by habit. Use it only for an owned child/junction whose aggregate deletion semantics require it.

For auditable entities, use `CreatedByUserId`, `CreationDate`, `ModifiedByUserId`, and `ModificationDate` consistently. Let the Paradigm application audit owner set the values unless a database-owned default is an explicit boundary decision. Use named foreign keys to the user table.

Use `IsActive BIT NOT NULL` with a named default for disable/logical lifecycle behavior. Use a seeded status table and `StatusId` foreign key for workflow state; do not overload `IsActive` as a state machine.

## Publish safely

Install SqlPackage in the repository-local tool manifest. Build first, then use the complete target connection string:

```powershell
dotnet tool run sqlpackage /Action:Publish /SourceFile:<database.dacpac> /TargetConnectionString:<connection-string>
```

Keep destructive publish properties disabled by default. Generate and review a deployment report/script before approving possible data loss.

If a script must run before SqlPackage creates its deployment plan, execute it through an explicit, idempotent pre-publish bootstrap step. Merely registering it as DACPAC `PreDeploy` does not move it before plan generation. Make that step visible in Aspire ordering and fail the bootstrap when it fails.

An optional BACPAC is a developer baseline, not schema source. Store at most one under `bootstrap`, import it only when an Aspire-managed local database is empty, and always publish the current DACPAC afterward. Never regenerate it automatically or import it into an external database.
