# PostgreSQL DbPublisher projects

## Configure the project

Keep `project.jsonc` beside the database folders and specify `databaseType: "PostgreSql"`. Do not store `connectionString`; supply `Paradigm_ORM_ConnectionString` from Aspire, CI, or a secure shell environment.

Use explicit `files` ordering when objects depend on one another:

1. pre-deployment/database setup;
2. tables and deferred relationship scripts;
3. retained upgrade scripts for existing installations;
4. views;
5. functions and routines;
6. post-deployment reference data.

Use `paths` only for a group whose alphabetical order is sufficient and deterministic. Reject missing and duplicate files. Generate `publish.sql` for review even when direct execution is enabled.

Base `CREATE TABLE IF NOT EXISTS` files create an empty database but do not evolve an existing table. Put retained, versioned, rerunnable upgrade scripts in an explicit ordered group after base tables and before dependent views/routines. Guard every transition against its already-applied state, keep destructive or data-rewrite steps under human review, and do not remove an upgrade until every supported environment has advanced past it.

## Write rerunnable scripts

- Use quoted PascalCase identifiers when matching Paradigm-generated model names.
- Use singular object names and the shared `PK_`, `FK_`, `UQ_`, `IX_`, and `DF_` convention where PostgreSQL names the object.
- Use `CREATE TABLE IF NOT EXISTS`, `CREATE OR REPLACE VIEW`, and replaceable routine/function definitions as appropriate.
- Keep cross-referenced constraints in a later ordered file when both tables must exist first.
- Use `INSERT ... ON CONFLICT ... DO UPDATE` or a reviewed equivalent for reference data.
- In an upsert update, normally assign from `EXCLUDED`; review any clause that only reassigns existing target values because it may be an accidental no-op.
- Never delete rows missing from a seed source unless the table is an explicitly authoritative closed catalog.

Mirror the shared audit and lifecycle meanings with PostgreSQL-native types, normally `TIMESTAMPTZ` for audit instants and boolean `IsActive`. Preserve the domain's identifier decision; do not convert existing keys merely to match a default.

Use identity/serial IDs for transactional entities. Use assigned integer IDs for seeded system/status catalogs, publish them with rerunnable post-deployment scripts, and mirror their numeric values and stable machine codes in a service-side .NET enum whose owner is chosen with `$paradigm-model-domain`. Never delete, reuse, or renumber published status IDs; retire them with `IsActive`. A table with `StatusId` also needs an append-only `<Entity>StatusHistory` transition table with non-cascading foreign keys. Record the initial status and update current status plus insert history in one transaction-owning, concurrency-protected workflow. PostgreSQL has no DACPAC pre-pre-deployment phase, so place ordered compatibility upgrades explicitly before dependent objects.

## Orchestrate and verify

In Aspire, reference the PostgreSQL database resource from a finite DbPublisher/bootstrap resource and map the connection explicitly, for example `.WithReference(database).WithEnvironment("Paradigm_ORM_ConnectionString", database)`. `WithReference` alone creates `ConnectionStrings__<resource-name>`; it does not create DbPublisher's variable. Make APIs wait for successful completion.

Pin the verified DbPublisher executable or container version in repository-owned configuration and restore or pull it before AppHost starts; do not let a running bootstrap discover or upgrade a version. DbPublisher versions have differed in how reliably execution failures become nonzero process exits. Before automating a version, force a disposable failure and verify its exit behavior. Follow publication with a schema probe so a false success cannot release dependent services.

Run against a disposable empty database and then run again against the populated database. Both runs must succeed without duplicate data or object-order failures.
