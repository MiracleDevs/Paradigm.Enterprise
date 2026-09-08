---
name: database-skills
description: >-
  Orchestrate database work by identifying the target engine and loading the
  applicable shared, engine-specific, and task-specific database guidance. Use
  for SQL Server, PostgreSQL, or MySQL schema, query, procedure, migration,
  integrity, and performance work.
---

# Database skills

This is the database skill entrypoint. Do not select engine-specific guidance
from the directory name or from remembered defaults. Identify the target
engine and version from the project configuration, provider, infrastructure,
or technical documentation before selecting guidance.

## Required loading order

1. Read the shared database governance and practices.
2. Identify the database engine and version.
3. Load exactly one engine profile from `engines`.
4. Load the task specialization or specializations required by the work.
5. Apply repository rules before the technical guidance.

If the engine or version cannot be established, state that limitation and do
not make engine-specific assumptions.

## Engine profiles

| Engine | Engine profile | Shared SQL Server-oriented skills | Task-specific guidance |
| --- | --- | --- | --- |
| SQL Server | The SQL Server rules in `database-engineering` and its specializations | `database-engineering` | `sql-schema-design`, `sql-query-performance`, `sql-procedure-design` |
| PostgreSQL | `engines/postgresql/database-engine.md` | Use only principles that are engine-neutral after checking compatibility | Select the applicable specialization and adapt it to PostgreSQL; do not apply SQL Server syntax or behavior mechanically |
| MySQL | `engines/mysql/database-engine.md` | Use only principles that are engine-neutral after checking compatibility | Select the applicable specialization and adapt it to MySQL; do not apply SQL Server syntax or behavior mechanically |

The files under `engines` are engine reference profiles. They are not
independent discovery entrypoints because they intentionally use
`database-engine.md` rather than `SKILL.md`.

## Task specialization

- Schema, tables, keys, constraints, relationships, indexes, and migrations:
  `sql-schema-design/SKILL.md`
- Queries, joins, filtering, aggregation, materialization, and query
  performance: `sql-query-performance/SKILL.md`
- Stored procedures, transactions, batching, and procedural workflows:
  `sql-procedure-design/SKILL.md`

These specializations are written with SQL Server as their native profile.
For PostgreSQL and MySQL, retain only the engine-neutral reasoning and use the
selected engine profile to replace syntax, optimizer, type, transaction, and
error-handling assumptions.

## Selection examples

```text
PostgreSQL table and query change
  -> shared database guidance
  -> engines/postgresql/database-engine.md
  -> sql-schema-design and sql-query-performance
```

```text
SQL Server stored procedure change
  -> shared database guidance
  -> database-engineering SQL Server orchestration
  -> sql-procedure-design
```

```text
MySQL index review
  -> shared database guidance
  -> engines/mysql/database-engine.md
  -> sql-schema-design and/or sql-query-performance, adapted to MySQL
```

Never load SQL Server, PostgreSQL, and MySQL profiles together for the same
database task. If a system contains multiple engines, split the work by
database boundary and select a profile independently for each boundary.