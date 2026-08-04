# Paradigm Database Practices

Apply these rules to handwritten SQL Server and PostgreSQL database source. Preserve established legacy names when renaming would create compatibility risk; use the canonical form for new projects and objects.

## Layout and ownership

- Keep one semantic database object per file and name the file after the object.
- Organize tables, views, functions, routines, types, and deployment data by business capability. Keep maintenance scripts separate from deployable model source.
- Keep the database project under `src/database` and include it in the application solution while retaining direct CLI build/publish support.
- Treat the database project as schema source. Treat generated EF/entities as replaceable output and put behavior in partial handwritten files.
- Give every consumer-facing major entity or transactional table a schema-bound `{Entity}View`. Preserve the base mapping surface, retain foreign-key IDs, and add commonly used descriptive fields through bounded joins while preserving one row per entity. Every view object and file ends in `View`, including PostgreSQL materialized views; helper and reporting views may use another descriptive stem, such as `QuotePricingView`. Internal, status, history, audit, and idempotency tables need a concrete consumer before receiving a view.

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
- Give transactional/entity tables an auto-incrementing `Id` by default. Give system catalogs such as statuses and closed enumerations stable, explicitly assigned identifiers; never let deployment order choose their values.
- Publish every system catalog through a rerunnable post-deployment data script. Mirror its identifiers and stable machine codes in a service-side .NET enum and review database seed and enum changes together; a localized/display name may differ. Never delete, reuse, or renumber a published status identifier. Retire it with `IsActive` while preserving foreign-key targets.
- Model current workflow state with a dedicated seeded status table and `StatusId` foreign key. Also add an append-only `<Entity>StatusHistory` table with its own auto-incrementing `Id`, `<Entity>Id`, `StatusId`, `CreatedByUserId`, and `CreationDate`; write a row for every accepted transition.
- Treat status-history rows as immutable transition facts. Write the initial status history when the entity is created, use non-cascading foreign keys, and route every later current-status update plus history insert through one transaction-owning workflow with concurrency protection. Do not expose generic history update/delete behavior, update or logically delete history rows, or add modification audit fields unless the domain deliberately supports correcting history.

## Routines and data scripts

- Give stored routines deterministic result-set order. Paginated searches return pagination metadata before result data and use a stable final sort.
- Make pre/post-deployment and DbPublisher scripts rerunnable.
- Use idempotent upsert/merge behavior for roles, permissions, statuses, and other required reference data.
- Use source-deleting synchronization only for an explicitly authoritative closed catalog. Never use it for users, tenant data, or data with an independent lifecycle.
- Require review for destructive transitions, data rewrites, baseline refresh, external publication, cascade changes, and any disabled data-loss protection.

For SQL Server, keep `scripts/prepredeployment/PrePreDeployment.sql` as an idempotent phase executed by the finite bootstrap after optional baseline import and verification of a successful DACPAC build, but before SqlPackage creates the deployment plan. Use it only for reviewed compatibility cleanup that must precede plan generation, such as removing an obstructing legacy object. Exclude it from model build and do not confuse it with DACPAC `PreDeploy`, which runs after plan generation. In Aspire solutions, build the DACPAC and install SQLCMD 18 plus pinned SqlPackage in the repository-owned bootstrap image so those tools are not host prerequisites. Execute pre-pre with SQLCMD; do not split `GO` batches with ad hoc string logic.

## Secrets and baselines

- Keep credentials and connection strings in environment/user-secret/deployment-secret configuration, never database source or publish profiles.
- Treat a committed BACPAC as an optional local-development baseline. Keep at most one under `src/database/bootstrap`, import it only into an empty managed local database, and always apply current schema source afterward.
- Never regenerate a baseline automatically, import it into an external database by default, or include real credentials or production-only data.
