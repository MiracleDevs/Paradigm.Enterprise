---
name: sql-procedure-design
description: >-
  Design and optimize SQL Server stored procedures. Use when the task involves stored procedures, multi-stage processes, transactions, batch processing, or optimization of complex procedures.
---

# SQL Procedure Design

## Governance

This skill is governed by:

../governance/database-governance.md

The agent MUST read and follow this document before applying the principles defined by this skill.

This skill defines technical principles specific to SQL queries.
It does not redefine project governance.

## 1. Purpose

Design and optimize SQL Server stored procedures with priority given to:

- Performance.
- Functional correctness.
- Maintainability.
- Execution control.
- Clear responsibilities.
- Ability to optimize by stage.
- Correct transaction handling.
- Observability and diagnostics.

A procedure must be structured so each stage has a clear responsibility and can be independently analyzed and optimized when necessary.

A procedure must not be divided merely because of its number of lines.

---

## 2. Scope

This skill applies to:

- Creating stored procedures.
- Modifying existing stored procedures.
- Read procedures.
- Write procedures.
- Batch processes.
- Multi-stage processes.
- Processes that require temporary materialization.
- Transactional processes.
- Optimizing existing procedures.

This skill does NOT authorize:

- Applying schema or index changes without prior validation.
- Modifying functional behavior.
- Removing transactions without analyzing their consequences.
- Arbitrarily fragmenting procedures.
- Creating helper procedures solely to reduce the number of lines.

---

## 3. Mandatory prerequisites

Before starting:

1. Identify that the database engine is SQL Server.
2. Read the applicable repository Rules.
3. Comply with the Rules before applying this skill's rules.
4. Understand the procedure's functional objective.

## 6. Identify whether the procedure is new or existing.

## 7. Identify the tables involved.

## 8. Identify read and write operations.

## 9. Identify transactional boundaries.

## 10. Identify processing stages.

If a repository Rule conflicts with a recommendation in this skill, the Rule takes precedence.

---

## 4. Fundamental principles

### 4.1 A procedure must have a clear responsibility

A procedure can execute multiple related operations.

It must not become a monolithic process that:

- Retrieves data.
- Transforms data.
- Executes multiple independent processes.
- Updates numerous areas of the system.
- Performs unrelated logic.
- Keeps a transaction open unnecessarily long.

### 4.2 Do not divide by number of lines

Length is not a problem by itself.

Divide when there is:

- A clearly separable responsibility.
- An independent stage.
- Reuse.
- A need for independent optimization.
- A need to isolate a costly operation.
- A need to control cardinality.
- A need to control a transaction.
- A need to improve maintainability.

### 4.3 Each stage must be controllable

A stage should make it possible to identify:

- Which data it receives.
- Which data it produces.
- Which tables it uses.
- What cost it may have.
- Which side effects it produces.

This facilitates optimization and diagnostics.

---

## 5. Mandatory design process

### Step 1 - Understand the process

Identify:

- Input.
- Output.
- Functional rules.
- Read operations.
- Write operations.
- Dependencies.
- Transactions.
- Possible errors.

### Step 2 - Identify stages

Conceptually separate:

```text
Input
  ↓
Validation
  ↓
Data retrieval / preparation
  ↓
Transformation
  ↓
Persistence
  ↓
Post-processing
  ↓
Output
```

Not all operations require all these stages.

### Step 3 - Evaluate complexity

Identify:

- Repeated queries.
- Repeated joins.
- Reused results.
- Costly processing.
- Large tables.
- Independent operations.
- Excessively long transactions.

### Step 4 - Define the structure

Decide whether to:

- Keep a single query.
- Use a CTE.
- Use `#temp`.
- Use `@table`.
- Divide the procedure.
- Create helper procedures.
- Create an independent operation in the application.

### Step 5 - Validate

Review:

- Correctness.
- Performance.
- Transactional integrity.
- Concurrency.
- Blocking.
- Maintainability.
- Potential regressions.

## 6. Procedure division

### 6.1 When to divide

Consider dividing a procedure when clearly independent stages exist.

Examples:

- Data preparation.
- Complex calculation.
- Entity persistence.
- Aggregate updates.
- Subsequent processing.

### 6.2 When NOT to divide

Do not divide solely because:

- The procedure has many lines.
- There are many declarations.
- There are several IF statements.
- There are several SELECT statements.
- The intent is to "make it cleaner."

### 6.3 Helper procedures

Create helper procedures when there is:

- An independent responsibility.
- Reuse.
- A need for isolation.
- A need for independent execution.
- A need for separate maintenance.

Avoid trivial helper procedures.

Example of poor fragmentation:

EXEC GetCustomer;
EXEC GetCustomerAddress;
EXEC GetCustomerPhone;
EXEC GetCustomerEmail;

when all are part of a single operation and fragmentation causes multiple unnecessary round trips or accesses.

### 6.4 Fragmentation and performance

Fragmentation can worsen performance if it causes:

- Multiple repeated accesses.
- Repeated joins.
- Loss of context.
- Greater overhead.
- More database engine calls.
- Greater transactional complexity.

Separation must reduce complexity or improve control, not merely distribute code.

## 7. Materialization between stages

### 7.1 General rule

When a stage produces a costly result that will be reused by multiple stages, consider materializing it.

Alternatives:

- CTE.
- `#temp`.
- `@table`.
- Permanent table, when functionally appropriate.

### 7.2 Evaluation

Consider:

- Cardinalidad.
- Generation cost.
- Number of reuses.
- Need for indexes.
- Need for statistics.
- I/O cost.
- Procedure duration.

### 7.3 Temporary tables

Consider `#temp` when:

- The result is significant in size.
- It is reused.
- Indexes are required.
- Statistics are needed.
- Complex subsequent joins exist.
- Execution stages should be separated.

### 7.4 Table variables

Consider `@table` when:

- The set is small.
- The operation is bounded.
- A complex access strategy is not required.
- Simplicity is preferable.

Do not select automatically based on a fixed number of rows.

## 8. Transactions

### 8.1 General rule

Transactions must be as broad as functional consistency requires and as small as possible.

### 8.2 Avoid excessively long transactions

Do not keep a transaction open during:

- Unnecessary costly queries.
- Processing that does not require atomicity.
- Independent operations.
- External waits.
- Prolonged processing that can be performed outside the transaction.

### 8.3 Atomicity

Do not separate stages if doing so breaks a functional property that requires atomicity.

Procedure division must never produce invalid intermediate states.

### 8.4 Nested transactions

Do not assume that a transaction started within a helper procedure provides transactional independence.

Correctly evaluate:

- `@@TRANCOUNT`.
- `COMMIT`.
- `ROLLBACK`.
- `SAVEPOINT`.
- Error propagation.

## 9. Error handling

Use appropriate SQL Server mechanisms to detect and propagate errors.

Evaluate:

- TRY/CATCH.
- THROW.
- Transaction state.
- XACT_STATE().
- Rollback.
- Error propagation.

Do not hide errors.

Do not use ambiguous return values to replace real errors.

## 10. Concurrency and blocking

Evaluate:

- Transaction duration.
- Isolation level.
- Affected rows.
- Indexes used.
- Locks.
- Potential for deadlocks.
- Concurrent operations.

Do not use NOLOCK as a generic performance solution.

If modifying isolation or read behavior is proposed, document:

- Problem.
- Risk.
- Consistency lost.
- Justification.

## 11. Parameter Sniffing

When a query within a procedure exhibits parameter-dependent behavior, evaluate the possibility of parameter sniffing.

Do not automatically apply:

OPTION (RECOMPILE)

or:

WITH RECOMPILE

Evaluate:

- Cardinality variation among parameters.
- Execution frequency.
- Compilation cost.
- Plan stability.
- Design alternatives.
- Indexes.
- Data distribution.

Every solution must be justified.

## 12. Query reuse

Identify operations that:

- Execute the same join multiple times.
- Calculate the same result.
- Repeatedly retrieve the same data.
- Repeatedly filter the same tables.

Evaluate whether to:

- Consolidate the query.
- Create a preceding stage.
- Materialize.
- Use a CTE.
- Use `#temp`.

Do not assume logical reuse implies physical materialization.

## 13. Batch processing

For large volumes, consider batch processing when a monolithic operation can produce:

- Excessively large transactions.
- Prolonged blocking.
- High memory consumption.
- Excessive log growth.
- Timeouts.

Carefully evaluate:

- Batch size.
- Stable ordering.
- Idempotency.
- Retries.
- Consistency.
- Concurrency.

Do not introduce batching if the operation requires complete atomicity.

## 14. Cursors and row-by-row processing

Avoid row-by-row processing when an appropriate set-based alternative exists.

Evaluate first:

- JOIN.
- `UPDATE ... FROM`.
- `INSERT ... SELECT`.
- MERGE, only when appropriate and safe.
- Aggregations.
- CTE.
- Temporary tables.

 A cursor can be valid when the process is inherently sequential or a specific functional need exists.

Do not automatically replace a cursor without understanding its semantics.

## 15. Dynamic SQL

Avoid dynamic SQL when it is not necessary.

When it is necessary:

Parameterize values.
Avoid unsafe concatenation.
Validate dynamic identifiers.
Limit the scope.
Document the reason.

Do not use dynamic SQL merely to avoid a properly parameterizable query structure.

## 16. Read procedures

Read procedures must:

Return only the necessary data.
Avoid repeated queries.
Avoid unnecessary joins.
Use appropriate indexes.
Avoid unnecessary processing.
Evaluate pagination for large sets.
Avoid loading large volumes when the consumer does not need them.

Pay particular attention to:

Filters.
Ordering.
Cardinality.
Indexes.
Column projection.

## 17. Write procedures

Write procedures must prioritize:

Integrity.
Atomicity.
Concurrency.
Consistency.
Performance.

Evaluate:

- Number of affected rows.
- Indexes involved.
- Existing triggers.
- Blocking.
- Transaction duration.
- Transaction log growth.

Avoid unnecessary additional queries after a write if the required result can be obtained efficiently during the same operation.

## 18. Performance validation

### 18.1 With evidence

When real data exists, evaluate:

Execution Plan.
CPU.
Logical Reads.
Elapsed Time.
Estimated vs. actual rows.
Costly operations.
Spills.
Memory Grants.
Locks.
Deadlocks.
Regressions.

### 18.2 Without evidence

If only code is available:

The optimization is a reasoned improvement and requires validation through real execution.

Do not state that the procedure is optimized.

### 18.3 Comparison

When before-and-after comparison is possible:

                    BEFORE      AFTER
CPU                 ...
Logical Reads       ...
Elapsed Time        ...
Rows                ...
Plan                ...

Metrics must be measured under comparable conditions.

## 19. Functional compatibility

Every modification must validate:

Result.
Cardinality.
Duplicates.
`NULL`.
Errors.
Transactions.
Concurrency.
Intermediate states.
Edge cases.

A performance improvement that changes behavior is incorrect.

## 20. Index and schema proposals

The procedure may reveal a need for:

Indexes.
Schema changes.
Permanent materialization.
Structural changes.

In those cases:

- Identify the problem.
- Propose the solution.
- Justify it.
- State the impact.
- State the risks.
- Require developer validation.

Do not apply structural changes automatically.

## 21. Output format

### Analysis

- Objective.
- Input.
- Output.
- Tables involved.
- Stages.
- Critical operations.
- Performance risks.
- Transactional risks.

### Design

- Procedure structure.
- Stages.
- Queries.
- Materialization.
- Transactions.
- Error handling.

### Proposal

- SQL.
- Helper procedures, if applicable.
- `#temp` / `@table`, if applicable.
- Proposed indexes.
- Proposed structural changes.

### Justification

For each relevant decision:

- Problem.
- Solution.
- Rationale.
- Alternatives considered.
- Cost.
- Risks.

### Pending validation

- Functional validation.
- Execution Plan.
- CPU.
- Logical Reads.
- Time.
- Concurrency.
- Locks.
- Regressions.

## 22. Checklist

Before completing:

- [ ] Applicable Rules were read.
- [ ] SQL Server was identified.
- [ ] The functional process was understood.
- [ ] The stages were identified.
- [ ] It was evaluated whether the procedure has clearly separable responsibilities.
- [ ] It was not split solely because of its length.
- [ ] Repeated queries were evaluated.
- [ ] Result reuse was evaluated.
- [ ] CTE vs. materialization was evaluated.
- [ ] `@table` vs. `#temp` was evaluated.
- [ ] Transactions were evaluated.
- [ ] Transaction duration was evaluated.
- [ ] Blocking and concurrency were evaluated.
- [ ] Error handling was evaluated.
- [ ] Parameter sniffing was evaluated when applicable.
- [ ] Row-by-row processing was avoided when a set-based alternative exists.
- [ ] Existing indexes were evaluated.
- [ ] Proposed indexes were justified.
- [ ] Functionality was preserved.
- [ ] It was stated whether the improvement is proven or requires validation.
- [ ] No schema changes were applied without prior validation.
