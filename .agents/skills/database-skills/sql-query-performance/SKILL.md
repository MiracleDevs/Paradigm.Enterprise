---
name: sql-query-performance
description: >-
  Design and optimize SQL Server queries, prioritizing performance and functional correctness. Use when the task involves SELECT, JOIN, CTEs, subqueries, materialization, query optimization, or query-oriented indexes.
---

# SQL Query Performance

## Governance

This skill is governed by:

../governance/database-governance.md

The agent MUST read and follow this document before applying the principles defined by this skill.

This skill defines technical principles specific to SQL queries.
It does not redefine project governance.

## 1. Purpose

Design and optimize SQL Server queries, prioritizing performance, functional correctness, maintainability, and operational cost.

The skill must produce SQL that uses database engine resources appropriately, avoids unnecessary work, and justifies relevant decisions.

Performance is a cross-cutting design constraint, not a justification for applying techniques indiscriminately.

---

## 2. Scope

This skill applies to:

- Creating new queries.
- Optimizing existing queries.
- Queries used by repositories, handlers, services, or procedures.
- Queries with complex joins, aggregations, filters, and result reuse.
- Proposals for indexes needed to improve data access.
- Evaluating the materialization of intermediate results.

This skill does NOT authorize:

- Applying schema or index changes without prior validation by the developer.
- Modifying functional rules to achieve a performance improvement.
- Removing validations or filters without demonstrating that they are unnecessary.
- Claiming a verified improvement without execution evidence.

---

## 3. Mandatory preconditions

Before starting:

1. Identify that the engine is SQL Server.
2. Read the applicable repository Rules.
3. Follow the Rules before applying the rules in this skill.
4. Identify the functional context of the query.
5. Identify the relevant tables, relationships, and data volume, when available.
6. Review existing indexes when the code or context permits.
7. Do not assume that a technique improves performance without evaluating its context.

If a repository Rule conflicts with a recommendation in this skill, the Rule takes precedence.

---

## 4. Fundamental principles

### 4.1 Optimize based on behavior, not syntax

Do not optimize solely because a query contains:

- A Table Scan.
- A CTE.
- Un `IN`.
- A subquery.
- A long procedure.
- A repeated join.

Each element must be evaluated according to:

- Cardinality.
- Selectivity.
- Data volume.
- Execution frequency.
- I/O cost.
- CPU cost.
- Result reuse.
- Maintainability.
- Index maintenance cost.
- Potential for regressions.

### 4.2 Preserve functional correctness

Every optimization must preserve:

- The expected result set.
- Filter semantics.
- Join semantics.
- `NULL` value semantics.
- Aggregation semantics.
- The semantics of relationships between entities.
- Business rules.

Do not replace one query with another solely because it "appears equivalent."

### 4.3 Prefer simple solutions when they are sufficient

Do not introduce:

- Unnecessary temporary tables.
- Unnecessary indexes.
- Unnecessary CTEs.
- Excessively fragmented procedures.
- Additional complexity without demonstrated benefit.

The solution must be sufficiently efficient for the real context.

---

## 5. Mandatory analysis process

### Step 1 — Understand the query

Identify:

- The required result.
- The participating tables.
- The existing relationships.
- The applied filters.
- The projected columns.
- The performed aggregations.
- The required ordering.
- The expected data volume.
- Whether the query is new or existing.

### Step 2 — Identify access patterns

Analyze:

- Scanned tables.
- Search predicates.
- Joins.
- Aggregations.
- Orderings.
- Result reuse.
- Repeated filters.
- Data calculated more than once.
- Potentially unnecessary accesses.

### Step 3 — Evaluate indexes

Review whether existing indexes allow efficient data access.

Evaluate:

- Columns used in filters.
- Columns used in joins.
- Columns used in orderings.
- Selectivity.
- Cardinality.
- The possibility of covering the query.
- Maintenance cost.
- Potentially redundant indexes.

### Step 4 — Evaluate execution strategy

Determine whether it is appropriate to:

- Execute the query directly.
- Use a CTE.
- Use a temporary table.
- Use a table variable.
- Materialize an intermediate result.
- Divide the query into stages.
- Restructure joins.
- Restructure filters.
- Propose indexes.

### Step 5 — Implement

Apply the alternative that best balances:

- Performance.
- Correctness.
- Maintainability.
- Complexity.
- Operational cost.

### Step 6 — Validate

Use execution plans or real metrics when they are available.

If they are not available, describe the proposal as a **reasoned improvement**, not as a verified improvement.

---

## 6. Index rules

### 6.1 General rule

Indexes must be designed according to actual or expected access patterns.

Do not create indexes solely to avoid a Table Scan.

A Table Scan can be correct when:

- The table is small.
- The query needs a large portion of the rows.
- The index does not provide sufficient improvement.
- The cost of using the index exceeds the cost of the scan.

### 6.2 Evaluating an index

Before proposing an index, evaluate:

- Table cardinality.
- Column selectivity.
- Query frequency.
- Filter columns.
- Join columns.
- Ordering columns.
- Columns needed to cover the query.
- Maintenance cost.
- Potential existing indexes that already cover the access pattern.

### 6.3 Composite indexes

Evaluate column order according to:

- Search predicates.
- Selectivity.
- Access patterns.
- Ordering requirements.
- The possibility of covering the query.

Do not assume that every combination of columns produces a useful index.

### 6.4 Covering indexes

Evaluate indexes with `INCLUDE` when they avoid additional access to the table or clustered index.

Do not add columns indiscriminately.

### 6.5 Indexes on small tables

Do not create indexes by default on tables with few rows.

Evaluate the actual cost and access frequency.

### 6.6 Indexes and schema changes

If an index or structural change is needed:

- Propose it.
- Justify it.
- State which query or access pattern it addresses.
- Do not apply it without prior validation by the developer.

---

## 7. Join rules

### 7.1 General rule

Joins must correctly express relationships between entities and allow the engine to execute the query efficiently.

### 7.2 Avoid repeated work

Identify patterns where:

- The same join is repeated.
- The same filters are repeated.
- The same result is recalculated.
- The same tables are accessed multiple times.
- An expensive operation is executed multiple times.

Evaluate whether it is appropriate to:

- Restructure the query.
- Use a CTE.
- Materialize a result.
- Divide execution into stages.

Do not materialize automatically.

### 7.3 Joins and cardinality

Evaluate:

- Expected cardinality of each relationship.
- Potential row multiplication.
- The need for `DISTINCT`.
- The need for pre-aggregation.
- Filters applied before or after the join.

Do not use `DISTINCT` to hide incorrect joins.

### 7.4 Joins and filters

Evaluate whether filters can be applied before expensive joins, while preserving query semantics.

Do not move filters indiscriminately.

### 7.5 Joins and subqueries

Prefer joins when they improve:

- Readability.
- Composition.
- Reuse.
- Optimization capability.
- Cardinality control.

Do not prohibit subqueries when they correctly express the semantics or are the most appropriate alternative.

---

## 8. CTE rules

### 8.1 Recommended use

Use CTEs when they improve:

- Readability.
- Logical composition.
- Separation of query stages.
- Conceptual reuse of a definition.
- Maintainability.

### 8.2 Do not assume materialization

A CTE must not be considered a temporary table or a guarantee of materialization.

If a result is reused multiple times, evaluate whether it should be materialized.

### 8.3 CTEs and performance

Do not replace a CTE with a temporary table solely for that reason.

Evaluate:

- Recomputation cost.
- Number of reuses.
- Cardinality.
- Join complexity.
- Need for statistics.
- Materialization cost.
- I/O cost.
- Maintainability.

### 8.4 Recursive CTEs

Evaluate carefully:

- Cardinality.
- Depth.
- Execution cost.
- Potential cycles.
- The need for limits.
- Iterative or materialized alternatives.

---

## 9. Materialization rules

### 9.1 When to evaluate materialization

Evaluate materializing intermediate results when there is:

- Significant reuse.
- High recomputation cost.
- Relevant cardinality.
- Repeated joins against the same result.
- A need for statistics.
- A need for indexes on the result.
- Clear separation of stages.
- A need to control execution.

### 9.2 Do not materialize automatically

Do not materialize solely because:

- A CTE is used multiple times.
- A query is long.
- There are many joins.
- You want to "avoid scans."

Materialization incurs writing, reading, maintenance, and I/O costs.

### 9.3 Evaluating the alternative

Compare:

- Direct query.
- CTE.
- `#temp`.
- `@table`.

Choose according to the context.

---

## 10. `@table` vs `#temp` rules

### 10.1 General rule

Do not choose solely based on row count.

Evaluate:

- Volume.
- Cardinality.
- Reuse.
- Join complexity.
- Need for statistics.
- Need for indexes.
- Compilation cost.
- Materialization cost.
- Operation duration.
- Potential for parallelism.
- Need to control stages.

### 10.2 Table variables

Evaluate `@table` when:

- The result is small.
- The operation is bounded.
- A complex access strategy is not required.
- Simplicity adds value.
- The materialization cost is low.

Do not assume it is always better for a small number of rows.

### 10.3 Temporary tables

Evaluate `#temp` when:

- The result has relevant cardinality.
- It is reused multiple times.
- Indexes are needed.
- Statistics are needed.
- There are complex joins.
- Stages need to be controlled.
- Materialization can reduce repeated work.

### 10.4 Temporary tables and tempdb

Evaluate the cost of:

- Writing.
- Reading.
- Temporary indexes.
- Contention.
- Used space.
- Temporary table lifetime.

Do not use `#temp` without considering its cost.

---

## 11. `IN`, `EXISTS`, and join rules

### 11.1 General rule

Do not prohibit `IN`.

Choose between `IN`, `EXISTS`, and joins according to:

- Semantics.
- Cardinality.
- Selectivity.
- List size.
- Potential duplicates.
- Execution plan.
- Maintainability.

### 11.2 Small lists

`IN` can be appropriate when the list is small and controlled.

### 11.3 Large or dynamic lists

Evaluate alternatives when the list is large or dynamic:

- Temporary table.
- Table variable.
- TVP.
- Join.
- `EXISTS`.

Do not use large `IN (...)` lists as a general-purpose data transport mechanism.

### 11.4 Semantics

Do not replace `IN` with `JOIN` without evaluating duplicates and `NULL` semantics.

---

## 12. Subquery rules

### 12.1 General rule

Prefer joins when they improve query composition or enable a more appropriate access strategy.

### 12.2 Do not prohibit subqueries

Subqueries can be appropriate when they:

- Correctly express the semantics.
- Avoid row duplication.
- Enable an existence condition.
- Simplify the query.
- Produce an appropriate plan.

### 12.3 Evaluation

Before replacing a subquery:

- Understand its semantics.
- Evaluate cardinality.
- Evaluate duplicates.
- Evaluate `NULL`.
- Evaluate the plan.
- Evaluate maintainability.

---

## 13. Filter rules and SARGability

### 13.1 General rule

Avoid expressions that prevent the engine from using indexes efficiently when an equivalent alternative exists.

Evaluate:

- Functions on filtered columns.
- Implicit conversions.
- Expressions on columns.
- Incompatible comparisons.
- Non-selective predicates.

### 13.2 Do not apply mechanical rules

Do not modify an expression solely to eliminate a function.

Verify that the alternative:

- Preserves semantics.
- Maintains precision.
- Maintains behavior with `NULL`.
- Does not introduce conversion errors.
- Does not change results.

---

## 14. Aggregation and ordering rules

Evaluate:

- Filtering before aggregations.
- Unnecessary aggregations.
- Unnecessary `DISTINCT` operations.
- Unnecessary orderings.
- Columns used in `GROUP BY`.
- Cardinality of intermediate results.
- Potential to reduce data before expensive joins.

Do not remove `ORDER BY` when it is part of the required result.

---

## 15. New SQL

For new SQL:

1. Design the query considering access patterns.
2. Evaluate existing indexes.
3. Avoid repeated work.
4. Avoid unnecessary complexity.
5. Choose CTEs, joins, and materialization according to the context.
6. Evaluate cardinality.
7. Propose indexes when they are needed.
8. Document relevant decisions.
9. Validate functionality.
10. Validate performance when possible.

---

## 16. Existing SQL optimization

For existing SQL:

1. Understand the current functional behavior.
2. Identify the performance problem.
3. Identify access patterns.
4. Review existing indexes.
5. Evaluate alternatives.
6. Modify the query while preserving functionality.
7. Propose index changes when appropriate.
8. Document the justification.
9. Validate functionality.
10. Validate performance when possible.

Do not rewrite an entire query if the problem can be resolved with a targeted change.

---

## 17. Validation

### 17.1 When real metrics are available

Use, when available:

- Execution plan.
- Execution time.
- CPU.
- Logical reads.
- Processed rows.
- Estimated versus actual cardinality.
- Index usage.
- Operation cost.
- Functional regressions.

### 17.2 When real metrics are not available

State:

> Reasoned improvement: the proposal is based on access patterns and optimization criteria, but requires validation through actual execution.

Do not state:

> The query is optimized.

### 17.3 Functional validation

Every optimization must verify:

- Results.
- Cardinality.
- Duplicates.
- `NULL`.
- Filters.
- Aggregations.
- Ordering.
- Edge cases.

---

## 18. Output format

### Analysis

- Query objective.
- Involved tables.
- Access patterns.
- Identified risks.
- Available context.

### Decisions

- Indexes.
- Joins.
- CTE.
- Materialization.
- `@table` / `#temp`.
- `IN` / `EXISTS` / joins.
- Other relevant decisions.

### Proposal

- Modified SQL.
- Proposed indexes.
- Proposed structural changes.
- Additional changes, when applicable.

### Justification

For each relevant decision:

- The problem it seeks to resolve.
- Chosen technique.
- Why it is appropriate.
- Considered alternatives.
- Introduced cost or risk.

### Pending validation

- What the developer must review.
- Which metrics or plans should be compared.
- Which functional cases must be tested.

---

## 19. Checklist

Before completing:

- [ ] The applicable Rules were read.
- [ ] The SQL Server engine was identified.
- [ ] The functional objective was understood.
- [ ] Access patterns were evaluated.
- [ ] Existing indexes were evaluated.
- [ ] Proposed indexes were justified.
- [ ] Joins and cardinality were evaluated.
- [ ] Whether a CTE is appropriate was evaluated.
- [ ] Whether materialization is appropriate was evaluated.
- [ ] `@table` vs `#temp` was evaluated.
- [ ] `IN` / `EXISTS` / joins were evaluated.
- [ ] SARGability was evaluated.
- [ ] Functional correctness was preserved.
- [ ] Decisions were documented.
- [ ] It was stated whether the improvement is verified or requires validation.
- [ ] No schema changes were applied without prior validation.