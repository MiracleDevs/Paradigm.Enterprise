# MySQL Database Engine

## Purpose

This skill defines the technical characteristics of MySQL that must be
considered when designing, reviewing, or optimizing databases, queries,
and stored procedures.

It complements the project's general database skills.
It does not replace their principles regarding design, performance,
integrity, maintainability, or governance.

## Applicability

This skill applies when the target database engine is MySQL.

The MySQL version must be determined from:

- Project rules.
- Application configuration.
- Infrastructure files.
- Dependencies and database providers.
- Project technical documentation.

If the version cannot be determined, the agent must avoid assuming
version-specific capabilities and must state this limitation.

The agent must not automatically transfer SQL Server rules, syntax,
optimizer behavior, hints, or strategies to MySQL.

---

## 1. Governance

This skill is governed by:

../../governance/DATABASE-GOVERNANCE.md

The agent MUST read and follow that document before applying the
principles defined by this skill.

This skill defines MySQL-specific technical principles.
It does not redefine project governance.

Project-specific rules, when available, must be resolved according to
the shared governance.

The absence of database-specific rules does not block the task.
In that case, the agent must apply the general governance and the
technical principles of the applicable skills.

---

## 2. General Engine Principles

### 2.1. Do not assume equivalence with SQL Server

The agent MUST NOT mechanically transfer concepts such as:

- @table versus #temp.
- tempdb.
- TRY/CATCH.
- THROW.
- XACT_STATE().
- OPTION (RECOMPILE).
- SQL Server-specific hints.
- Parameter sniffing as a default explanation.
- CTE or subquery behavior.
- SQL Server-specific data types and functions.

When a recommendation comes from a SQL Server-oriented skill, the agent
must first verify whether a valid MySQL equivalent exists.

### 2.2. Consider the storage engine

The agent must identify the storage engine used by the relevant tables.

For transactional tables, it must evaluate the characteristics of
InnoDB, including:

- Transactions.
- Referential integrity.
- Locking and concurrency.
- Indexes.
- Storage behavior.
- Read and write behavior.

The agent must not assume that all tables use the same storage engine.

The storage engine choice must be justified according to the use case
and project constraints.

### 2.3. Consider the version

Available capabilities may vary between MySQL versions.

The agent must verify the compatibility of proposals involving:

- CTEs.
- Recursive CTEs.
- Window functions.
- EXPLAIN ANALYZE.
- Optimizer hints.
- Functional indexes.
- Generated columns.
- Table expressions.
- Partitioning.
- Stored procedure syntax.
- Isolation options.
- JSON functions.

The agent must not present a feature as available without checking its
compatibility with the target version.

---

## 3. Schema Design

### 3.1. Normalization

The general normalization principles defined by the schema design skills
remain applicable.

Third normal form should be the starting point, not an absolute
requirement to denormalize or a prohibition against doing so.

Denormalization may be proposed only when there is a concrete
justification, such as:

- Reducing expensive reads.
- Eliminating repeated calculations.
- Query requirements.
- Performance requirements.
- Integration requirements.
- Audit or historical requirements.

Every denormalization must document:

- Reason.
- Source of truth.
- Consistency mechanism.
- Update strategy.
- Storage cost.
- Write impact.
- Rebuild strategy.
- Divergence risks.

### 3.2. Data types

Data types must be selected according to:

- Domain.
- Range.
- Precision.
- Scale.
- Nullability.
- Expected volume.
- Operations performed.
- Storage.
- Indexing.
- Application compatibility.

The agent must not use a larger type for convenience without
justification.

The agent must not use a smaller type merely to save space if it may
compromise the domain or future evolution.

For monetary values, the agent must evaluate DECIMAL according to the
required precision and scale. It must not assume that FLOAT or
DOUBLE are equivalent to an exact decimal type.

### 3.3. Primary and foreign keys

Key design must consider:

- Domain identity.
- Uniqueness.
- Size.
- Cardinality.
- Relationships.
- Access patterns.
- Index impact.
- Generation by the application.
- Model evolution.

The agent must not choose a key solely by convention.

Foreign keys must represent real relationships and preserve referential
integrity, unless the project has an explicit and justified decision
otherwise.

### 3.4. Indexes

Indexes must be designed from actual or reasonably expected access
patterns.

The agent must evaluate:

- Columns used in filters.
- Columns used in joins.
- Ordering.
- Grouping.
- Selectivity.
- Cardinality.
- Table size.
- Read frequency.
- Write frequency.
- Existing indexes.
- Redundancy.
- Maintenance cost.
- Composite index opportunities.
- Column order within indexes.

The agent must not create indexes merely because a column appears in a
WHERE, JOIN, or foreign key.

The agent must not create indexes by default on small tables when their
maintenance cost is not justified.

The agent must not assume that a composite index is useful without
evaluating column order and query access patterns.

The agent must not assume that an index will be used simply because it
exists.

### 3.5. Functional indexes and generated columns

When a query applies a function to a column, the agent must evaluate:

- Whether the predicate can be rewritten.
- Whether a generated column can be used.
- Whether a functional index compatible with the target version can be
  used.
- Whether the maintenance cost justifies the solution.
- Whether the expression preserves semantics.

The agent must not create a generated column or functional index as an
automatic solution to every SARGability problem.

### 3.6. Schema changes

Schema modifications must be proposed as reproducible and reviewable
changes.

The agent must consider:

- Compatibility with existing data.
- Migration.
- Locking.
- Duration.
- Table size.
- Index impact.
- Rollback or recovery strategy.
- Application compatibility.
- Progressive deployment when appropriate.

The agent must not apply schema changes, indexes, constraints, or type
changes without the prior validation required by project governance.

---

## 4. Queries and Optimization

### 4.1. Optimize behavior, not appearance

The agent must optimize actual query behavior.

It must not automatically consider a query better because it:

- Has fewer lines.
- Uses fewer subqueries.
- Uses fewer CTEs.
- Replaces IN with EXISTS.
- Replaces a subquery with a JOIN.
- Removes DISTINCT.
- Removes ORDER BY.
- References fewer tables.

Every transformation must preserve semantics and be justified.

### 4.2. SARGability

The agent must evaluate whether predicates allow the available indexes
to be used effectively.

It must pay particular attention to:

- Functions applied to filtered columns.
- Implicit conversions.
- Expressions involving indexed columns.
- Incompatible type comparisons.
- Leading wildcards in LIKE.
- Operations that prevent an indexable range.
- Conditions that unnecessarily reduce selectivity.

The agent must not assume that every function applied to a column is
necessarily a problem. It must consider the version, indexes,
selectivity, and execution plan.

### 4.3. IN, EXISTS, and subqueries

IN is NOT prohibited.

The agent must evaluate its use according to:

- Number of values.
- Source of the values.
- Cardinality.
- Potential duplicates.
- NULL values.
- Materialization cost.
- Execution plan.
- Readability.
- Reuse.

A small, controlled list may be perfectly valid.

For large or dynamic sets, the agent should consider alternatives such
as:

- Temporary tables.
- Derived tables.
- CTEs.
- EXISTS.
- JOIN.
- Permanent tables.
- Parameter-passing mechanisms available in the application.

The agent must not replace IN with EXISTS solely because of a
stylistic rule.

Correlated subqueries must be evaluated according to their actual or
reasonably expected cost. The agent must not assume that they always
execute once per row or that the optimizer always transforms them.

The agent must verify whether a transformation changes:

- Duplicates.
- NULL values.
- Cardinality.
- Existence semantics.
- Aggregate results.

### 4.4. CTEs

CTEs are valid for:

- Improving readability.
- Separating logical stages.
- Expressing recursive queries.
- Facilitating composition.
- Making transformations explicit.

They must not be used as an automatic materialization mechanism.

In MySQL, the optimizer may merge or materialize CTEs and derived tables
depending on the case. The agent must evaluate the resulting behavior
rather than assume that a CTE is always materialized or always
re-executed. <Cite ref="turn0search0" />

If an intermediate result is expensive and reused, the agent must
consider whether to:

- Keep it as a CTE.
- Use a derived table.
- Materialize it explicitly.
- Create a temporary table.
- Persist it.
- Modify the query to avoid repeated computation.

Materialization must be justified by cost, reuse, cardinality,
maintenance, and complexity.

### 4.5. Temporary tables

MySQL must not be treated as though it has the same choice between
SQL Server table variables and temporary tables.

When intermediate results need to be materialized, the agent must
evaluate:

- Number of rows.
- Row size.
- Number of references.
- Operation duration.
- Need for indexes.
- Need for statistics.
- Creation cost.
- Write cost.
- Read cost.
- Concurrency.
- Temporary resource usage.
- Possibility of avoiding materialization.

The agent must not impose universal row-count thresholds.

The agent must not recommend a temporary table merely because a query
contains a CTE or subquery.

### 4.6. Joins

The agent must evaluate:

- Cardinality of each relationship.
- Selectivity.
- Join columns.
- Compatible data types.
- Indexes.
- Access order.
- Row multiplication.
- Filters applied before or after the join.
- Actual necessity of each table.

The agent must not remove a JOIN without verifying that it does not
provide data, restrictions, or semantic effects.

The agent must not add DISTINCT to hide duplicates caused by an
incorrect join.

### 4.7. Aggregations and ordering

The agent must evaluate:

- Volume of data aggregated.
- Filters applied before aggregation.
- Grouping.
- Ordering.
- Need for DISTINCT.
- Repeated calculations.
- Possibility of reducing rows before aggregation.
- Sorting cost.
- Pagination opportunities.

The agent must not remove an ORDER BY if it is part of the functional
contract.

The agent must not add LIMIT to hide a query that is supposed to
return all results.

### 4.8. Pagination

When a query supports a paginated screen or API, the agent must
evaluate:

- Number of rows.
- Stable ordering.
- Pagination columns.
- Cost of OFFSET.
- Cursor or keyset pagination opportunities.
- Suitable indexes.
- Consistency between pages.

The agent must not introduce pagination if it changes the functional
contract.

---

## 5. Stored Procedures

### 5.1. Responsibility

Procedures should have a clear responsibility.

The agent should split them when there is a technical or functional
reason, such as:

- Independent stages.
- Reuse.
- Transaction control.
- Error handling.
- Reduced complexity.
- Optimization of a specific stage.
- Different cardinalities.
- Separation between preparation and persistence.

The agent must not split procedures solely because of their line count.

The agent must not fragment them in a way that unnecessarily increases:

- Round trips.
- Repeated access.
- Transactions.
- Complexity.
- Failure points.
- Dependencies between stages.

### 5.2. Syntax and error handling

The agent must use MySQL-specific syntax and error-handling mechanisms.

It must not automatically transfer:

- TRY/CATCH.
- THROW.
- XACT_STATE().
- RAISERROR.
- SQL Server error-handling patterns.

It must consider:

- Handlers.
- Error conditions.
- SIGNAL.
- RESIGNAL.
- Rollback.
- Transaction state.
- Error propagation.
- Client compatibility.
- Partial failure behavior.

### 5.3. Transactions

Transactions should be as broad as consistency requires and as short
as possible.

The agent must evaluate:

- Operations that must be atomic.
- Duration.
- Locks.
- Isolation.
- Concurrent reads.
- Writes.
- Potential deadlocks.
- Rollback.
- Unnecessary work inside the transaction.

The agent must not keep transactions open during external operations,
unnecessary waits, or work that does not require atomicity.

The agent must not remove transactions to improve performance without
evaluating the consistency consequences.

### 5.4. Batch processing

For large write volumes, the agent must evaluate whether batch
processing is appropriate.

It must consider:

- Atomicity.
- Batch size.
- Locks.
- Logging and recovery.
- Duration.
- Retries.
- Idempotency.
- Processing order.
- Consistency.
- Impact on other users.

The agent must not apply batching automatically when the functional
requirement requires a single atomic transaction.

### 5.5. Cursors and row-by-row processing

Set-based processing should be preferred when appropriate.

However, the agent must not mechanically replace every sequential
operation with a complex query if that worsens:

- Readability.
- Correctness.
- Error handling.
- Maintainability.
- Execution cost.

The decision must be justified.

### 5.6. Dynamic SQL

Dynamic SQL should be used only when it provides a real need.

The agent must consider:

- Parameterization.
- Validation.
- SQL injection.
- Permissions.
- Complexity.
- Reuse.
- Stability.
- Preparation cost.
- Optimizer compatibility.

The agent must not concatenate input values directly into SQL.

---

## 6. Optimizer and Execution Plans

### 6.1. Do not assume the plan

The agent must distinguish between:

- Written SQL.
- Estimated plan.
- Executed plan.
- Observed behavior.

The agent must not claim that a query is faster merely because it
appears simpler.

### 6.2. Analysis tools

When available, the agent should use:

- EXPLAIN.
- EXPLAIN FORMAT=JSON.
- EXPLAIN ANALYZE.
- Table statistics.
- Execution metrics.
- Application metrics.
- Index information.
- Concurrency information.

EXPLAIN can be used to analyze how MySQL plans to access tables,
including join order and index usage. EXPLAIN ANALYZE can provide
execution information such as rows, timing, and iteration counts.
<Cite ref="turn0search4" />

The agent must not execute EXPLAIN ANALYZE against write operations
or expensive queries without considering the impact.

### 6.3. Statistics

The agent must consider that statistics influence optimizer decisions.

When appropriate, it must evaluate:

- Outdated statistics.
- Estimated cardinality.
- Data distribution.
- Selectivity.
- Existing indexes.
- Whether statistics should be updated.

The agent must not automatically attribute a problem to missing indexes
without evaluating statistics and the execution plan.

### 6.4. Hints

Optimizer hints should be used only when there is a concrete
justification.

They must not be the first response to a slow query.

Before proposing hints, the agent must evaluate:

- Why the optimizer selected the current plan.
- Whether the problem can be resolved through design, indexes, or
  rewriting.
- Whether the hint is compatible with the target version.
- Whether it may harm other cases.
- Whether it introduces excessive engine dependency.
- Whether it requires periodic validation.

---

## 7. Concurrency and Consistency

The agent must consider:

- Isolation level.
- Locks.
- Deadlocks.
- Transaction duration.
- Access order to tables.
- Inconsistent reads.
- Concurrent writes.
- Indexes and their locking impact.
- InnoDB behavior.

The agent must not use mechanisms equivalent to NOLOCK as a generic
solution to concurrency problems.

The agent must not reduce isolation without explaining the functional
impact.

The agent must not interpret a locking problem as exclusively a query
performance problem.

---

## 8. Validation and Permitted Changes

The agent may:

- Analyze queries.
- Review schemas.
- Propose indexes.
- Propose type changes.
- Propose procedure changes.
- Propose materialization.
- Propose transaction changes.
- Refactor SQL when authorized.
- Document decisions.

The agent MUST NOT assume authorization to apply:

- Indexes.
- Schema changes.
- Constraint changes.
- Type changes.
- Storage engine changes.
- Isolation changes.
- Destructive changes.
- Procedures with functional impact.

Changes that may affect schema or performance must remain proposals
until they receive the validation required by project governance.

### Statuses

Each relevant change must be classified as:

- IMPLEMENTED
- PROPOSED
- PENDING VALIDATION

### Evidence level

The agent must distinguish between:

- *Proven improvement:* execution or measurement evidence exists.
- *Reasoned improvement:* the proposal is supported by technical
  analysis but has not yet been measured.
- *Hypothesis:* a possibility that requires investigation.

The agent must not claim that a query was successfully optimized when
there is insufficient evidence.

---

## 9. Output Format

For each relevant analysis, the agent must present:

### Analysis

- Identified problem.
- Context.
- Engine and version.
- Relevant tables and relationships.
- Known or estimated cardinality.
- Constraints.
- Missing information.

### Decisions

- Proposed change.
- Reason.
- Applied principle.
- Alternatives considered.
- Expected impact.
- Risks.
- MySQL compatibility.

### Validation

- Available evidence.
- Execution plan.
- Metrics.
- Pending tests.
- Analysis limitations.

### Status

- IMPLEMENTED
- PROPOSED
- PENDING VALIDATION

### Traceability

The agent must indicate whether each decision comes from:

- A governance rule.
- A project-specific rule.
- A technical principle of this skill.
- A reasoned decision.
- A pending hypothesis.

---

## 10. Checklist

Before completing a MySQL task, the agent must verify:

- [ ] The engine and version were identified.
- [ ] Shared governance was read.
- [ ] Project-specific rules were reviewed, if available.
- [ ] SQL Server rules were not transferred mechanically.
- [ ] Functional semantics were preserved.
- [ ] Cardinality and selectivity were evaluated.
- [ ] Existing indexes were reviewed before proposing new ones.
- [ ] Indexes were not created merely for stylistic reasons.
- [ ] Universal thresholds were not applied without context.
- [ ] SARGability was evaluated.
- [ ] Joins and row multiplication were evaluated.
- [ ] Subqueries, IN, and EXISTS were evaluated according to context.
- [ ] The agent did not assume that a CTE is always materialized.
- [ ] The agent did not assume that a CTE is always re-executed.
- [ ] Intermediate-result materialization was evaluated.
- [ ] SQL Server rules concerning @table and #temp were not transferred.
- [ ] Transactions and concurrency were evaluated.
- [ ] MySQL-specific error handling was evaluated.
- [ ] Version compatibility was considered.
- [ ] Evidence was distinguished from hypotheses.
- [ ] Schema changes were not applied without validation.
- [ ] Relevant decisions were documented.