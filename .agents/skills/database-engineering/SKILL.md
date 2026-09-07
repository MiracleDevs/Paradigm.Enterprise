name: database-engineering
description: >-
        Orchestrates SQL Server database design, implementation, and optimization. Use when a task involves tables, queries, stored procedures, indexes, performance, data integrity, or data-access refactoring.

# Database Engineering

## Governance

This skill is governed by:

../governance/DATABASE-GOVERNANCE.md

The agent MUST read and follow this document before applying the principles defined by this skill.

This skill defines technical principles specific to SQL queries.
It does not redefine project governance.

## 1. Purpose

Orchestrate the design, implementation, and optimization activities for SQL Server databases.

This skill acts as the entry point for tasks related to:

- Schema design.
- Query design and optimization.
- Stored procedure design and optimization.
- Indexes.
- Performance.
- Data integrity.
- Data-access refactoring.

Its primary responsibility is to determine which specialization must be involved and ensure that repository rules are followed.

---

## 2. Scope

This skill applies when a task involves:

- Tables.
- Columns.
- Relationships.
- Keys.
- Constraints.
- Indexes.
- SQL queries.
- Stored Procedures.
- Database performance.
- Result materialization.
- Data-access optimization.
- Database-related migrations.

---

## 3. Required source of rules

Before performing any database work:

1. Identify the task context.
2. Read the applicable repository Rules.
3. Repository Rules take precedence over the generic recommendations in these skills.

Never assume the content of the Rules.

Never replace the Rules with memorized knowledge.

Never ignore them because the task appears trivial.

---

## 4. Specializations

Use the following skills as applicable:

### `sql-schema-design`

Apply when the task involves:

- Tables.
- Columns.
- PK.
- FK.
- Constraints.
- Relationships.
- Normalization.
- Denormalization.
- Structural indexes.
- Schema migrations.

### `sql-query-performance`

Apply when the task involves:

- SELECT.
- JOIN.
- WHERE.
- GROUP BY.
- ORDER BY.
- CTE.
- Subqueries.
- `IN`.
- `EXISTS`.
- `@table`.
- `#temp`.
- Materialization.
- Query optimization.
- Query-oriented indexes.

### `sql-procedure-design`

Apply when the task involves:

- Stored Procedures.
- Multi-stage processes.
- Transactions.
- Batch processing.
- Complex procedures.
- Parameter sniffing.
- Error handling.
- Concurrency.

---

## 5. Skill composition

A task may require more than one skill.

Example:

```text
Create a table and optimize the queries that use it
        ↓
sql-schema-design
        ↓
sql-query-performance
```

Another example:

```text
Optimize a Stored Procedure that uses several temporary tables
        ↓
sql-procedure-design
        ↓
sql-query-performance
        ↓
sql-schema-design
```

The agent must use all skills required to solve the problem correctly.

Do not limit the work to the first identified skill.

## 6. Required workflow

Every database task must follow:

┌──────────────────────────┐
│ Identify the task        │
└────────────┬─────────────┘
             ↓
┌──────────────────────────┐
│ Read Rules               │
└────────────┬─────────────┘
             ↓
┌──────────────────────────┐
│ Analyze context          │
└────────────┬─────────────┘
             ↓
┌──────────────────────────┐
│ Select skills            │
└────────────┬─────────────┘
             ↓
┌──────────────────────────┐
│ Design / modify          │
└────────────┬─────────────┘
             ↓
┌──────────────────────────┐
│ Analyze performance      │
└────────────┬─────────────┘
             ↓
┌──────────────────────────┐
│ Validate functionality   │
└────────────┬─────────────┘
             ↓
┌──────────────────────────┐
│ Document decisions       │
└──────────────────────────┘

## 7. Performance as a cross-cutting concern

Performance must be considered in all database tasks.

This requires evaluating, as applicable:

- Cardinality.
- Selectivity.
- I/O.
- CPU.
- Joins.
- Indexes.
- Materialization.
- Result reuse.
- Data volume.
- Execution frequency.
- Maintenance cost.

Do not apply performance techniques mechanically.

## 8. Principle of proportionality

Optimization must be proportional to the problem.

Do not introduce complexity to solve nonexistent problems.

Examples:

- Do not create unnecessary indexes on small tables.
- Do not use #temp for trivial sets.
- Do not split simple procedures.
- Do not materialize results that are used only once without a clear reason.
- Do not replace IN solely because of a stylistic preference.
- Do not eliminate a Table Scan without analyzing cardinality and selectivity.

## 9. Rule on indexes

Indexes must correspond to access patterns.

Before proposing an index:

- Identify the query or queries that require it.
- Identify the columns involved.
- Evaluate selectivity.
- Evaluate cardinality.
- Review existing indexes.
- Evaluate overlap.
- Evaluate maintenance cost.
- Justify the proposal.

A proposed index must identify the problem it is intended to solve.

## 10. Rule on materialization

When repeated work is performed on the same result, evaluate materialization.

Consider:

- CTE.
- `@table`.
- `#temp`.
- Permanent table.

The choice must consider:

- Cardinality.
- Reuse.
- Statistics.
- Indexes.
- I/O.
- CPU.
- Complexity.
- Process duration.

Do not assume that materializing is always better.

## 11. Rule on CTEs

Use CTEs when they provide:

- Readability.
- Composition.
- Logical separation.
- Conceptual reuse.

Do not consider a CTE a guarantee of materialization.

If a result is reused and its recomputation may be costly, evaluate materialization.

## 12. Rule on IN, EXISTS, and JOIN

Do not establish an absolute prohibition.

Select the alternative according to:

- Semantics.
- Cardinality.
- Selectivity.
- Duplicates.
- NULL.
- Set size.
- Execution plan.
- Maintainability.

In particular, avoid large IN (...) lists used as a data-transfer mechanism.

## 13. Rule on subqueries

Prefer joins when they provide:

- Better composition.
- Better reuse.
- Greater clarity.
- A better access strategy.

Do not eliminate subqueries automatically.

First evaluate their semantics and behavior.

## 14. Rule on procedures

Procedures must be split by responsibility, not by length.

Evaluate splitting when there is:

- An independent stage.
- Reuse.
- A need for independent optimization.
- A need to control cardinality.
- A need to isolate a costly operation.
- A need to control a transaction.

Avoid artificial fragmentation.

## 15. Validation

Every modification must distinguish between:

### Verified improvement

There is before-and-after evidence through:

- Execution Plan.
- CPU.
- Logical Reads.
- Time.
- Rows.
- Other relevant metrics.

### Reasoned improvement

No execution metrics are available, but the decision is technically substantiated.

### Hypothesis

The proposal requires execution and validation before determining whether it actually improves behavior.

Never present a hypothesis as a verified improvement.

## 16. Changes that require human validation

The agent may:

- Analyze.
- Design.
- Refactor SQL.
- Propose indexes.
- Propose schema changes.
- Propose migrations.
- Propose materialization.
- Propose procedure splitting.

The agent MUST NOT assume authorization to apply:

- New indexes.
- Index removal.
- PK changes.
- FK changes.
- Constraint changes.
- Denormalization.
- Destructive changes.
- Structural schema changes.

These changes require prior validation by the developer.

## 17. Functional preservation

No optimization may silently modify:

- Business rules.
- Expected cardinality.
- Results.
- Relationships.
- Behavior with NULL.
- Error handling.
- Atomicity.
- Consistency.

When functional uncertainty exists, stop the optimization and request clarification.

## 18. Decision documentation

Every relevant design or performance decision must be able to answer:

- What problem exists?
- Why is it a problem?
- What solution is proposed?
- Why this solution?
- What alternatives were considered?
- What cost does it introduce?
- What risk does it introduce?
- How must it be validated?

Do not document trivial decisions that add no information.

## 19. Expected result

Every task must produce, as applicable:

- SQL code.
- DDL.
- Proposed indexes.
- Migrations.
- Procedures.
- Refactorings.
- Technical justification.
- Risks.
- Pending validations.

The result must clearly distinguish:

- IMPLEMENTADO
- PROPUESTO
- PENDING VALIDATION

## 20. General checklist

Before completing any task:

- [ ] SQL Server was identified.
- [ ] The applicable Rules were read.
- [ ] The required skills were selected.
- [ ] The functional context was understood.
- [ ] Performance was evaluated.
- [ ] Access patterns were evaluated.
- [ ] Indexes were evaluated when applicable.
- [ ] Cardinality was evaluated.
- [ ] Result reuse was evaluated.
- [ ] Materialization was evaluated when applicable.
- [ ] Functional semantics were preserved.
- [ ] The impact on the schema was evaluated.
- [ ] Relevant decisions were documented.
- [ ] Verified improvements were distinguished from reasoned improvements.
- [ ] Pending validations were identified.
- [ ] Structural changes require developer validation.
