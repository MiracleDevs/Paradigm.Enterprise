# Paradigm Database Practices

Apply these rules to handwritten SQL Server and PostgreSQL database source. Preserve established legacy names when renaming would create compatibility risk; use the canonical form for new projects and objects.

## Layout and ownership

- Keep one semantic database object per file and name the file after the object.
- Organize tables, views, functions, routines, types, and deployment data by business capability. Keep maintenance scripts separate from deployable model source.
- Keep the database project under `src/database` and include it in the application solution while retaining direct CLI build/publish support.
- Treat the database project as schema source. Treat generated EF/entities as replaceable output and put behavior in partial handwritten files.

## Names and relationships

- Use singular PascalCase object and column names and an explicit owned schema (`dbo` by default on SQL Server).
- Name primary keys `PK_Table`, foreign keys `FK_Table_ReferencedTable`, unique constraints `UQ_Table_Columns`, indexes `IX_Table_Columns`, and defaults `DF_Table_Column`. Preserve existing `UX_` names during maintenance but do not introduce them in a new project.
- Append the local column to a foreign-key name when a table has several relationships to the same target.
- Require an explicit aggregate/data-lifecycle reason for cascade deletion. Do not cascade by convention.
- Use schema-bound SQL Server views unless a documented cross-database or dynamic dependency prevents it.

## Audit, lifecycle, and state

- For auditable entities, use `CreatedByUserId`, `CreationDate`, `ModifiedByUserId`, and `ModificationDate` as one coherent set. Use database-native instant types and explicit user foreign keys.
- Let the Paradigm application audit owner set audit values unless the database is deliberately the owner. Do not add competing implicit defaults.
- Use `IsActive` for enabled/disabled or logical lifecycle behavior. Do not add an `IsDeleted` convention to new Paradigm projects.
- Model workflow state with a dedicated status table, stable identifiers, a `StatusId` foreign key, and idempotent reference-data publication.

## Routines and data scripts

- Give stored routines deterministic result-set order. Paginated searches return pagination metadata before result data and use a stable final sort.
- Make pre/post-deployment and DbPublisher scripts rerunnable.
- Use idempotent upsert/merge behavior for roles, permissions, statuses, and other required reference data.
- Use source-deleting synchronization only for an explicitly authoritative closed catalog. Never use it for users, tenant data, or data with an independent lifecycle.
- Require review for destructive transitions, data rewrites, baseline refresh, external publication, cascade changes, and any disabled data-loss protection.

## Secrets and baselines

- Keep credentials and connection strings in environment/user-secret/deployment-secret configuration, never database source or publish profiles.
- Treat a committed BACPAC as an optional local-development baseline. Keep at most one under `src/database/bootstrap`, import it only into an empty managed local database, and always apply current schema source afterward.
- Never regenerate a baseline automatically, import it into an external database by default, or include real credentials or production-only data.
