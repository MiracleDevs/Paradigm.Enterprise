# Domain Driven Design Rules

---

# DDD-001 Entity Behavior

Severity: HIGH  
Scope: Domain

Rule

Domain entities must encapsulate business behavior.

Generated database-first persistence shapes may require public setters and parameterless construction. Treat them as a persistence exception: place behavior and invariants in non-generated partial classes or map them to handwritten domain models.

Forbidden

- Handwritten entity state exposed without a persistence or serialization requirement
- Generated shapes treated as the sole home of business behavior
- Intrinsic domain rules implemented in Controllers

---

# DDD-002 Entity Encapsulation

Severity: HIGH  
Scope: Domain

Rule

Entities must control their state through methods.

Required

- Private or protected setters and backing collections in handwritten models
- State changes performed through domain methods
- Partial or handwritten behavior around database-generated mutable shapes

Forbidden

- Public mutable collections outside generated persistence requirements
- Direct external modification of entity state

---

# DDD-003 Value Object Immutability

Severity: MEDIUM  
Scope: Domain

Rule

Value Objects must be immutable.

Required

- Implemented as `record` or immutable class
- Validation performed in constructor

Forbidden

- Public mutable properties
- Parameterless constructors

---

# DDD-004 Aggregate Integrity

Severity: HIGH  
Scope: Domain

Rule

Entities must not modify other aggregates directly.

Changes to related entities must occur through the aggregate root.

Forbidden

- Cross-aggregate state modification
- Direct modification of child entities from outside the aggregate

---

# DDD-005 Domain Purity

Severity: HIGH  
Scope: Domain

Rule

Domain entities must remain infrastructure-independent.

Forbidden

- Dependency injection in entities
- References to DbContext
- References to repositories
- References to logging or framework services
