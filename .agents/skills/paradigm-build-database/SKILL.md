---
name: paradigm-build-database
description: Create, extend, validate, or review a Paradigm database project for SQL Server with Microsoft.Build.Sql or PostgreSQL with Paradigm DbPublisher. Use for new database projects, tables, views, functions, routines, types, pre/post-deployment scripts, seed data, keys, audit fields, BACPAC baselines, DACPAC publishing, or database convention audits.
---

# Build a Paradigm database

Read and apply [Paradigm Good Coding Practices](../../references/good-coding-practices.md) and [Paradigm Database Practices](../../references/database-practices.md) before changing database source or tooling.

## Establish the database boundary

1. Inspect the engine, target platform, existing project/configuration, database ownership, application solution, deployment path, generated-model boundary, and current naming. Do not infer production access or destructive-change permission.
2. Put a new project under `src/database` and include it in the application `.sln` or `.slnx`. Keep direct CLI build/publish support.
3. Use one object per file and group tables, views, functions, routines, types, and scripts by capability.
4. Preserve established legacy names while maintaining an existing project. Apply canonical names to new projects and new objects unless compatibility requires otherwise.

Read [SQL Server projects](references/sql-server.md) for `.sqlproj`/DACPAC/BACPAC work. Read [PostgreSQL DbPublisher](references/postgresql.md) for `project.jsonc` and ordered PostgreSQL scripts.

## Make changes safely

- Declare current desired schema in SQL Server project objects; do not create an accumulating migration catalog around a DACPAC.
- Keep PostgreSQL DbPublisher files rerunnable and explicitly ordered because the publisher executes them sequentially.
- Make reference/status/permission seed scripts idempotent. Use source-deleting synchronization only for an explicitly authoritative closed catalog; never apply it to users or tenant-owned data.
- Keep credentials out of project, publish profile, SQL, BACPAC documentation, and generated output. Use the root `.env` contract from `$paradigm-setup-aspire` for local orchestration.
- Require explicit review for data loss, cascade deletion, constraint replacement, large rewrites, baseline refresh, or an external database publish.

## Validate deterministically

Run the bundled validator:

```powershell
python .agents/skills/paradigm-build-database/scripts/validate_database_project.py --project <sqlproj-or-project.jsonc> --solution <solution> --strict
```

Use `--format json` for automation. Omit `--strict` when auditing a legacy project so noncanonical but established names remain warnings. The validator is read-only; do not make it a fixer.

Then build or compile the engine-specific project and inspect the complete output. Treat idempotency, cascade semantics, destructive transitions, seed ownership, and stored-routine correctness as judgment-based review even when deterministic checks pass.

## Finish

- SQL Server: run `dotnet build <database.sqlproj>`, publish to a disposable database with SqlPackage, and compare the produced DACPAC when converting project formats.
- PostgreSQL: generate the aggregate publish script, inspect its deterministic order, execute against a disposable database, and verify the installed DbPublisher reports failures reliably.
- Regenerate EF/database-first output only after the database build succeeds. Build the application solution and review the generated diff.
- With Aspire, verify first-run creation, optional baseline import, subsequent DACPAC/DbPublisher updates, and restart idempotency before accepting the change.
