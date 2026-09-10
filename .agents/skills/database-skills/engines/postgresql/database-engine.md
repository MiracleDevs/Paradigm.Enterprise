# PostgreSQL Database Engine

## Purpose

This skill defines PostgreSQL-specific technical principles for
designing, reviewing, and optimizing databases, queries, and stored
procedures.

It complements the project's general database skills.
It does not replace their principles regarding design, performance,
integrity, maintainability, or governance.

This skill is intended for both new SQL and optimization of existing SQL.

---

## 1. Governance

This skill is governed by:

../../governance/DATABASE-GOVERNANCE.md

The agent MUST read and follow that document before applying the
principles defined by this skill.

This skill defines PostgreSQL-specific technical principles.
It does not redefine project governance.

The agent must also read the project's database-specific rules when
the optional directory exists:

.\docs\rules\database

If that directory does not exist, the agent must continue using the
general project governance and the principles defined by the applicable
database skills.

The absence of database-specific rules is not an error and does not
authorize arbitrary decisions.

---

## 2. Engine and Version

The agent must identify:

- PostgreSQL version.
- Relevant extensions.
- Storage and deployment context.
- ORM or database provider, when applicable.
- Relevant configuration that may affect execution.
- Whether the task concerns PostgreSQL-compatible syntax or
  PostgreSQL-specific features.

The agent must not assume that a feature is available without checking
the target version.

This applies especially to:

- CTE behavior.
- MATERIALIZED and NOT MATERIALIZED.
- EXPLAIN ANALYZE.
- INCLUDE indexes.
- Partial indexes.
- Expression indexes.
- Generated columns.
- JSON and JSONB features.
- Partitioning.
- MERGE.
- Stored procedure and function syntax.
- Parallel query capabilities.
- Extensions.

The agent must not mechanically transfer SQL Server or MySQL-specific
rules to PostgreSQL.

---

## 3. General Principles

### 3.1. Optimize behavior, not syntax

The agent must optimize actual execution behavior.

It must not consider a query better merely because it:

- Has fewer lines.
- Uses fewer subqueries.
- Uses fewer CTEs.
- Replaces IN with EXISTS.
- Replaces a subquery with a JOIN.
- Removes DISTINCT.
- Removes ORDER BY.
- Uses fewer tables.

Every transformation must preserve functional semantics and be
justified.

### 3.2. Preserve semantics

The agent must verify that proposed changes preserve:

- Returned rows.
- Duplicates.
- NULL behavior.
- Aggregation results.
- Ordering requirements.
- Transactional behavior.
- Concurrency behavior.
- Error behavior.
- Constraints.
- Application contracts.

The agent must not use DISTINCT to hide an incorrect join.

The agent must not remove ORDER BY when ordering is part of the
functional contract.

The agent must not add LIMIT merely to conceal an inefficient query.

### 3.3. Proportionality

The agent must consider the size and importance of the problem.

It should not introduce complex optimizations when:

- The table is very small.
- The query is executed rarely.
- The cost is negligible.
- The complexity would exceed the benefit.
- The proposed change introduces unnecessary maintenance.

It must not use universal thresholds without considering the actual
context.

---

## 4. Schema Design

### 4.1. Normalization

Third normal form should be the starting point.

The agent must evaluate:

- Functional dependencies.
- Redundant data.
- Update anomalies.
- Insert anomalies.
- Delete anomalies.
- Data ownership.
- Referential integrity.
- Evolution of the model.

Denormalization may be proposed when justified by:

- Expensive repeated calculations.
- Frequent read patterns.
- Reporting requirements.
- Integration requirements.
- Historical or audit requirements.
- Performance requirements.

Every denormalization must document:

- Reason.
- Source of truth.
- Consistency mechanism.
- Update strategy.
- Rebuild strategy.
- Storage cost.
- Write impact.
- Risks of divergence.

### 4.2. Data types

Types must be selected according to:

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

The agent must not choose a type merely by habit.

For monetary values, the agent must evaluate numeric or
numeric(precision, scale) according to the required precision and
scale.

It must not assume that real or double precision are equivalent to
exact decimal arithmetic.

### 4.3. Primary keys

The agent must evaluate:

- Domain identity.
- Uniqueness.
- Cardinality.
- Key size.
- Foreign key impact.
- Index impact.
- Application generation.
- Ordering requirements.
- Model evolution.

The agent must not choose a surrogate key merely to avoid analyzing
the domain's natural identity.

The choice between UUID, integer, composite, or another key must be
justified by the actual use case.

### 4.4. Foreign keys and constraints

Foreign keys and constraints should represent real business and
relational invariants.

The agent must evaluate:

- Cardinality.
- Optionality.
- Delete behavior.
- Update behavior.
- Referential integrity.
- Constraint validation cost.
- Indexing implications.
- Application compatibility.

The agent must not remove constraints for performance without
evidence and explicit validation.

### 4.5. Indexes

Indexes must be designed from actual or reasonably expected access
patterns.

The agent must evaluate:

- Filtering.
- Joins.
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
- Composite index column order.
- Possibility of partial indexes.
- Possibility of expression indexes.
- Possibility of covering indexes using INCLUDE.

The agent must not create an index merely because a column appears in
a WHERE, JOIN, or foreign key.

The agent must not create indexes by default on small tables when their
maintenance cost is not justified.

The agent must not assume that an index will be used simply because it
exists.

### 4.6. Partial indexes

The agent may consider partial indexes when a query repeatedly accesses
a well-defined subset of rows.

It must evaluate:

- Whether the predicate is stable and appropriate.
- Whether the query predicate can use the index.
- Selectivity.
- Maintenance cost.
- Data distribution.
- Whether the index remains useful as data changes.
- Whether the partial predicate preserves the intended semantics.

It must not create a partial index merely because a WHERE clause
contains a condition.

### 4.7. Expression indexes

When a query applies a function or expression to a column, the agent
must evaluate:

- Whether the predicate can be rewritten.
- Whether an expression index is appropriate.
- Whether a generated column is more suitable.
- Whether the expression is compatible with the target version.
- Whether the maintenance cost is justified.
- Whether the expression preserves semantics.

The agent must not create an expression index automatically for every
SARGability problem.

### 4.8. Schema changes

Schema changes must be proposed as reproducible and reviewable changes.

The agent must consider:

- Existing data.
- Migration duration.
- Locking.
- Table size.
- Index creation cost.
- Constraint validation.
- Rollback or recovery strategy.
- Application compatibility.
- Deployment sequencing.
- Progressive deployment when appropriate.

The agent must not apply schema changes, indexes, constraints, or type
changes without the validation required by project governance.

---

## 5. Query Performance

### 5.1. SARGability

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
necessarily a problem.

It must consider:

- Data distribution.
- Existing indexes.
- Query selectivity.
- PostgreSQL planner behavior.
- Whether an expression index is appropriate.
- Whether a rewrite is simpler and safer.

### 5.2. IN, EXISTS, and subqueries

IN is NOT prohibited.

The agent must evaluate its use according to:

- Number of values.
- Source of the values.
- Cardinality.
- Potential duplicates.
- NULL behavior.
- Materialization cost.
- Execution plan.
- Readability.
- Reuse.

A small, controlled list may be perfectly valid.

For large or dynamic sets, the agent should consider:

- Temporary tables.
- CTEs.
- Derived relations.
- EXISTS.
- JOIN.
- Permanent tables.
- Application-provided relations.

The agent must not replace IN with EXISTS solely because of a
stylistic rule.

Correlated subqueries must be evaluated according to their actual or
reasonably expected cost.

The agent must not assume that they always execute once per row or
that PostgreSQL always transforms them into an equivalent join.

The agent must verify whether a transformation changes:

- Duplicates.
- NULL behavior.
- Cardinality.
- Existence semantics.
- Aggregate results.

### 5.3. CTEs

CTEs are valid for:

- Improving readability.
- Separating logical stages.
- Recursive queries.
- Composing complex queries.
- Making transformations explicit.
- Reusing expensive intermediate results.

A CTE must not be treated as an automatic materialization mechanism.

The agent must evaluate whether PostgreSQL will fold or materialize the
CTE, and whether explicit MATERIALIZED or NOT MATERIALIZED is
appropriate.

The agent must consider:

- Number of references.
- Cost of the CTE.
- Predicate pushdown.
- Repeated computation.
- Cardinality.
- Index usage.
- Memory and temporary-file usage.
- Maintainability.

The agent must not use MATERIALIZED merely because a CTE is reused.

The agent must not use NOT MATERIALIZED merely because materialization
is considered undesirable.

The decision must be justified by the query's access patterns and
expected execution behavior.

### 5.4. Temporary tables

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
- Whether a CTE or derived relation is sufficient.

The agent must not impose universal row-count thresholds.

The agent must not recommend a temporary table merely because a query
contains a CTE or subquery.

### 5.5. Joins

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

### 5.6. Aggregations and ordering

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

### 5.7. Pagination

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

## 6. PostgreSQL-Specific Performance Features

### 6.1. Execution plans

The agent must distinguish between:

- Written SQL.
- Estimated plan.
- Executed plan.
- Observed behavior.

It must not claim that a query is faster merely because it appears
simpler.

### 6.2. EXPLAIN

When available, the agent should use:

- EXPLAIN.
- EXPLAIN (ANALYZE).
- EXPLAIN (BUFFERS).
- EXPLAIN (ANALYZE, BUFFERS).
- EXPLAIN (FORMAT JSON).
- Relevant statistics.
- Application metrics.
- Index information.
- Concurrency information.

EXPLAIN shows the plan selected by PostgreSQL, including scan methods,
join algorithms, and estimated costs. EXPLAIN ANALYZE provides
execution information that can be compared with the estimates.
<Cite ref="turn0search3" />

The agent must not execute EXPLAIN ANALYZE against write operations or
expensive queries without considering the impact.

### 6.3. Statistics

The agent must consider that statistics influence planner decisions.

When appropriate, it must evaluate:

- Outdated statistics.
- Estimated cardinality.
- Data distribution.
- Selectivity.
- Existing indexes.
- Whether statistics should be updated.
- Whether the planner's estimates differ substantially from actual
  execution.

The agent must not automatically attribute a problem to missing
indexes without evaluating statistics and the execution plan.

### 6.4. Planner behavior

The agent must evaluate the planner's actual behavior rather than
assuming a fixed strategy.

It must not assume that PostgreSQL always chooses:

- An index scan.
- A sequential scan.
- A nested loop.
- A hash join.
- A merge join.
- A particular join order.
- A particular CTE strategy.

A sequential scan may be correct for a small table or a low-selectivity
predicate.

### 6.5. Parallel query

The agent may consider parallel execution when the workload justifies
it.

It must evaluate:

- Query size.
- Available resources.
- Parallel safety.
- Cost of parallel setup.
- Whether the query is CPU-bound or I/O-bound.
- Concurrency.
- Whether parallel execution improves the relevant workload.

It must not assume that enabling parallelism automatically improves
performance.

### 6.6. Materialized views

The agent may consider materialized views when:

- The same expensive result is reused frequently.
- Slightly stale data is acceptable.
- Refresh cost is acceptable.
- The result can be indexed effectively.
- The maintenance strategy is clear.

The agent must document:

- Refresh strategy.
- Data freshness.
- Refresh cost.
- Indexes.
- Dependencies.
- Rebuild strategy.
- Failure behavior.
- Consistency expectations.

It must not introduce a materialized view merely because a query is
slow.

---

## 7. Stored Procedures and Functions

### 7.1. Responsibility

Procedures and functions should have a clear responsibility.

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

The agent must not split procedures solely because of line count.

The agent must not fragment them in a way that unnecessarily increases:

- Round trips.
- Repeated access.
- Transactions.
- Complexity.
- Failure points.
- Dependencies between stages.

### 7.2. Functions versus procedures

The agent must distinguish between:

- Functions that return values or result sets.
- Procedures invoked with CALL.
- SQL functions.
- PL/pgSQL functions.
- SQL-language procedures.
- Application-side orchestration.

The choice must be based on:

- Transaction requirements.
- Reuse.
- Data access.
- Error handling.
- Performance.
- Application contract.
- Maintainability.

The agent must not move logic into the database merely because it can.

### 7.3. Error handling

The agent must use PostgreSQL-specific error-handling mechanisms.

It must not automatically transfer:

- TRY/CATCH.
- THROW.
- XACT_STATE().
- RAISERROR.
- SQL Server error-handling patterns.

It must consider:

- EXCEPTION blocks.
- RAISE.
- SQLSTATE.
- Transaction behavior.
- Error propagation.
- Rollback behavior.
- Client compatibility.
- Partial failure behavior.

The agent must evaluate the cost and scope of exception handling rather
than adding broad exception blocks indiscriminately.

### 7.4. Transactions

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

### 7.5. Batch processing

For large write volumes, the agent must evaluate whether batch
processing is appropriate.

It must consider:

- Atomicity.
- Batch size.
- Locks.
- WAL generation.
- Duration.
- Retries.
- Idempotency.
- Processing order.
- Consistency.
- Impact on other users.

The agent must not apply batching automatically when the functional
requirement requires a single atomic transaction.

### 7.6. Cursors and row-by-row processing

Set-based processing should be preferred when appropriate.

However, the agent must not mechanically replace every sequential
operation with a complex query if that worsens:

- Readability.
- Correctness.
- Error handling.
- Maintainability.
- Execution cost.

The decision must be justified.

### 7.7. Dynamic SQL

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
- Planner behavior.

The agent must not concatenate input values directly into SQL.

---

## 8. Concurrency and Consistency

The agent must consider:

- MVCC.
- Isolation level.
- Locks.
- Deadlocks.
- Transaction duration.
- Access order to tables.
- Inconsistent reads.
- Concurrent writes.
- Indexes and their locking impact.
- Vacuum and table maintenance.
- Long-running transactions.
- Transaction ID and bloat implications when relevant.

The agent must not use mechanisms equivalent to NOLOCK as a generic
solution to concurrency problems.

The agent must not reduce isolation without explaining the functional
impact.

The agent must not interpret a locking problem as exclusively a query
performance problem.

---

## 9. Validation and Permitted Changes

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

## 10. Output Format

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
- PostgreSQL compatibility.

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

## 11. Checklist

Before completing a PostgreSQL task, the agent must verify:

- [ ] The engine and version were identified.
- [ ] Shared governance was read.
- [ ] Project-specific rules were reviewed, if available.
- [ ] SQL Server and MySQL rules were not transferred mechanically.
- [ ] Functional semantics were preserved.
- [ ] Cardinality and selectivity were evaluated.
- [ ] Existing indexes were reviewed before proposing new ones.
- [ ] Indexes were not created merely for stylistic reasons.
- [ ] Universal thresholds were not applied without context.
- [ ] SARGability was evaluated.
- [ ] Joins and row multiplication were evaluated.
- [ ] Subqueries, IN, and EXISTS were evaluated according to context.
- [ ] CTE behavior was evaluated according to PostgreSQL.
- [ ] MATERIALIZED and NOT MATERIALIZED were considered when relevant.
- [ ] Intermediate-result materialization was evaluated.
- [ ] Temporary tables were considered according to context.
- [ ] Partial indexes were considered when appropriate.
- [ ] Expression indexes were considered when appropriate.
- [ ] Planner estimates and actual execution were distinguished.
- [ ] Transactions and concurrency were evaluated.
- [ ] PostgreSQL-specific error handling was evaluated.
- [ ] Version compatibility was considered.
- [ ] Evidence was distinguished from hypotheses.
- [ ] Schema changes were not applied without validation.
- [ ] Relevant decisions were documented.