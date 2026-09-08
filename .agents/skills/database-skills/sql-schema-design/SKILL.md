---
name: sql-schema-design
description: >-
  Design data schemas for SQL Server, prioritizing integrity, normalization, and performance. Use when the task involves tables, columns, PKs, FKs, constraints, relationships, structural indexes, or schema migrations.
---

# SQL Schema Design

## Governance

This skill is governed by:

../governance/database-governance.md

The agent MUST read and follow this document before applying the principles defined by this skill.

This skill defines specific technical principles for SQL queries.
It does not redefine project governance.

## 1. Purpose

Design data schemas for SQL Server that prioritize:

- Data correctness and integrity.
- Third normal form as the baseline design.
- Performance.
- Maintainability.
- Controlled schema evolution.
- Compatibility with application access patterns.

The skill must produce designs that reduce the need for subsequent optimizations without introducing unnecessary complexity.

---

## 2. Scope

This skill applies to:

- Creating tables.
- Modifying existing tables.
- Designing relationships.
- Designing PKs and FKs.
- Designing constraints.
- Designing indexes.
- Evaluating normalization.
- Evaluating denormalization.
- Designing columns and data types.
- Proposing migrations.
- Reviewing existing schemas.

This skill does NOT authorize:

- Applying schema changes without prior developer validation.
- Creating indexes indiscriminately.
- Denormalizing without justification.
- Removing constraints to improve performance without demonstrating the impact.
- Modifying functional rules to simplify the design.

---

## 3. Mandatory Preconditions

Before starting:

1. Identify SQL Server as the database engine.
2. Read the applicable repository Rules.
4. Comply with the Rules before applying this skill's rules.
4. Identify the model's functional context.
5. Identify the involved entities and relationships.
6. Identify whether the schema is new or existing.
7. Review naming, schema, and migration conventions.
8. Identify known access patterns.
9. Do not assume a structure is suitable without evaluating its context.

If a repository Rule conflicts with a recommendation in this skill, the Rule takes precedence.

---

## 4. Fundamental Principles

### 4.1 Normalization as the baseline design

Tables must be designed using third normal form (3NF), unless there is a technical or functional justification for an exception.

Normalization must avoid:

- Unnecessary data duplication.
- Partial dependencies.
- Transitive dependencies.
- Update anomalies.
- Deletion anomalies.
- Insertion anomalies.

### 4.2 Performance as a cross-cutting constraint

The design must consider from the outset:

- Expected cardinality.
- Access patterns.
- Selectivity.
- Relationships.
- Indexes.
- Data volume.
- Query frequency.
- Maintenance cost.

Do not wait for a performance problem to arise before evaluating the structure.

### 4.3 Integrity before convenience

Do not remove referential integrity, constraints, or validations to simplify queries.

If there is a conflict between integrity and performance, evaluate alternatives and document the decision.

### 4.4 Simplicity

Do not introduce:

- Unnecessary tables.
- Unnecessary relationships.
- Unnecessary indexes.
- Duplicate columns.
- Excessive abstractions.
- Premature denormalization.

---

## 5. Mandatory Design Process

### Step 1 — Understand the domain

Identify:

- Entities.
- Attributes.
- Relationships.
- Cardinalities.
- Business rules.
- The identity of each entity.
- Dependencies between data.
- Required and optional data.

### Step 2 — Identify access patterns

Evaluate:

- Frequent queries.
- Common filters.
- Frequent joins.
- Sort orders.
- Aggregations.
- High-cardinality relationships.
- Data queried together.
- Data updated together.

If access patterns are unknown, state that explicitly.

### Step 3 — Design the normalized model

Define:

- Tables.
- Columns.
- PKs.
- FKs.
- Constraints.
- Relationships.
- Data types.
- Nullability.

### Step 4 — Evaluate performance

Identify:

- Required indexes.
- Potential bottlenecks.
- High-cardinality relationships.
- Queries that may require composite indexes.
- Potential materialization or denormalization needs.

### Step 5 — Implement the proposal

Generate:

- DDL.
- Indexes.
- Constraints.
- Migrations, when applicable.

### Step 6 — Validate

Review:

- Integrity.
- Functional correctness.
- Application compatibility.
- Expected performance.
- Maintenance cost.
- Future evolution.

---

## 6. Normalization Rules

### 6.1 First normal form (1NF)

Tables must contain atomic values.

Avoid:

- Comma-separated lists.
- Multiple values in a single column.
- Repeated columns representing a collection.
- Structures that hinder filters, joins, or constraints.

### 6.2 Second normal form (2NF)

Columns must depend on the entire primary key.

For tables with composite keys, specifically evaluate:

- Partial dependencies.
- Attributes that depend only on part of the key.
- The possibility of separating entities.

### 6.3 Third normal form (3NF)

Columns must not depend transitively on the primary key.

Avoid:

- Duplicating attributes derived from another entity.
- Storing data that belongs to a related entity.
- Maintaining redundant information without justification.

### 6.4 Exceptions to normalization

Evaluate denormalization only when a concrete need exists, for example:

- High-frequency critical queries.
- High join cost.
- Need to reduce repeated work.
- Reporting requirements.
- Specific read requirements.
- Need to materialize results.

Every exception must document:

- The problem it solves.
- Why the normalized solution is insufficient.
- The inconsistencies or costs it introduces.
- How consistency will be maintained.
- How performance will be validated.

---

## 7. Primary Key Rules

### 7.1 General rule

Every table must have a primary key that uniquely identifies each row.

### 7.2 PK design

Evaluate:

- Row identity.
- Stability.
- Uniqueness.
- Size.
- Use in relationships.
- Storage cost.
- Index cost.
- Domain compatibility.

### 7.3 Composite keys

Use composite keys when they correctly represent row identity.

Do not avoid them automatically.

Evaluate:

- Semantics.
- Cardinality.
- Relationships.
- Indexes.
- Join complexity.
- Maintainability.

### 7.4 Surrogate keys

Evaluate surrogate keys when:

- The natural identity is complex.
- The natural identity may change.
- The entity is referenced by multiple tables.
- A stable technical identity is needed.

Do not use surrogate keys to conceal a poorly defined domain identity.

---

## 8. Foreign Key Rules

### 8.1 Referential integrity

Relationships must be represented by FKs when applicable.

Do not rely solely on application validations to guarantee integrity.

### 8.2 Cardinality

Define correctly:

- 1:1.
- 1:N.
- N:N.

Do not use unnecessary junction tables.

### 8.3 N:N relationships

Many-to-many relationships must be represented using a junction table.

Evaluate:

- PK.
- FK.
- Uniqueness.
- Relationship-specific attributes.
- Indexes.

### 8.4 FKs and indexes

Evaluate indexes on FKs when an access pattern justifies them.

Do not automatically create indexes on all FKs.

---

## 9. Column and Data Type Rules

### 9.1 Appropriate types

Select types according to:

- Data domain.
- Range.
- Precision.
- Scale.
- Size.
- Comparisons.
- Sorting.
- Application compatibility.

### 9.2 Avoid excessively broad types

Do not use types larger than necessary without justification.

Evaluate the impact on:

- Storage.
- Indexes.
- Memory.
- I/O.
- Comparisons.
- Joins.

### 9.3 Nullability

Define `NULL` or `NOT NULL` according to domain semantics.

Do not use `NULL` as a default value to avoid design decisions.

### 9.4 Derived values

Evaluate whether a value should:

- Be calculated in a query.
- Be stored.
- Be materialized.
- Be maintained through a controlled process.

Do not store derived values without clear justification.

---

## 10. Constraint Rules

Evaluate:

- `NOT NULL`.
- `UNIQUE`.
- `CHECK`.
- `DEFAULT`.
- `PRIMARY KEY`.
- `FOREIGN KEY`.

Constraints must represent integrity rules that correspond to the model.

Do not remove constraints to improve performance without evidence.

---

## 11. Index Rules

### 11.1 General rule

Indexes must be designed according to actual or expected access patterns.

Do not create indexes indiscriminately.

### 11.2 Indexes on large tables

Specifically evaluate:

- Columns used in filters.
- Columns used in joins.
- Columns used in sort orders.
- Selectivity.
- Cardinality.
- Access frequency.
- The possibility of covering queries.

### 11.3 Indexes on small tables

Do not create indexes by default on tables with few rows.

Evaluate the actual cost and access frequency.

### 11.4 Composite indexes

Evaluate column order according to:

- Search predicates.
- Selectivity.
- Access patterns.
- Sorting.
- The possibility of covering queries.

Do not assume that any combination of columns produces a useful index.

### 11.5 Covering indexes

Evaluate `INCLUDE` when it avoids additional access to the table or clustered index.

Do not add columns indiscriminately.

### 11.6 Redundant indexes

Review existing indexes before proposing new ones.

Avoid:

- Duplicate indexes.
- Indexes that overlap unnecessarily.
- Indexes that provide no value.
- Excessively wide indexes.

### 11.7 Maintenance cost

Evaluate the impact on:

- INSERT.
- UPDATE.
- DELETE.
- Storage.
- I/O.
- Fragmentation.
- Index maintenance.

---

## 12. Denormalization Rules

### 12.1 General rule

Denormalization is an exception, not the baseline design.

### 12.2 When to evaluate

Evaluate denormalization when:

- A critical query exists.
- Join cost is high.
- Significant reuse exists.
- Repeated work must be reduced.
- The read pattern justifies the additional cost.

### 12.3 Do not denormalize automatically

Do not duplicate data solely to:

- Avoid a join.
- Simplify a query.
- Avoid an FK.
- Avoid a junction table.
- Avoid a normalized relationship.

### 12.4 Consistency

Every denormalization must define:

- Source of truth.
- Update mechanism.
- Error handling.
- Rebuild strategy.
- Consistency validation.
- Transactional impact.

---

## 13. Schema Evolution Rules

### 13.1 Compatibility

Evaluate the impact on:

- Existing code.
- Queries.
- Repositories.
- Migrations.
- Integrations.
- Existing data.

### 13.2 Destructive changes

Do not remove columns, tables, or relationships without:

- Identifying dependencies.
- Evaluating data migration.
- Evaluating compatibility.
- Obtaining prior validation.

### 13.3 Migrations

Migrations must be:

- Reproducible.
- Controlled.
- Compatible with the current schema state.
- Safe for existing data.
- Reviewable.

---

## 14. Change Proposals

When a need for change is identified:

- Propose the change.
- Justify it.
- State the problem it solves.
- State the functional impact.
- State the performance impact.
- State the risks.
- Do not apply it without prior developer validation.

---

## 15. Output Format

### Analysis

- Model objective.
- Involved entities.
- Relationships.
- Access patterns.
- Identified risks.
- Available context.

### Proposed Design

- Tables.
- Columns.
- PK.
- FK.
- Constraints.
- Indexes.
- Relationships.
- Data types.

### Justification

For each relevant decision:

- Problem it intends to solve.
- Decision made.
- Why it is suitable.
- Alternatives considered.
- Cost or risk introduced.

### Migration Proposal

- DDL.
- Indexes.
- Constraints.
- Structural changes.
- Impact on existing data.

### Pending Validation

- What the developer must review.
- What functional cases must be tested.
- What metrics or plans should be compared.
- What risks must be validated.

---

## 16. Checklist

Before completing:

- [ ] The applicable Rules were read.
- [ ] SQL Server was identified as the database engine.
- [ ] The functional objective was understood.
- [ ] Entities and relationships were identified.
- [ ] 3NF normalization was evaluated.
- [ ] Exceptions to normalization were justified.
- [ ] PKs and FKs were defined.
- [ ] Constraints were evaluated.
- [ ] Data types were evaluated.
- [ ] Nullability was evaluated.
- [ ] Existing indexes were evaluated.
- [ ] Proposed indexes were justified.
- [ ] Maintenance cost was evaluated.
- [ ] Schema evolution was evaluated.
- [ ] Data integrity was preserved.
- [ ] Decisions were documented.
- [ ] No changes were applied without prior validation.