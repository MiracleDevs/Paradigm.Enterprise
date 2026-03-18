# Domain Driven Design Rules

---

# DDD-001 Entity Behavior

Severity: HIGH  
Scope: Domain

Rule

Domain entities must encapsulate business behavior.

Forbidden

- Public setters for entity state
- Entities implemented as pure data containers
- Business logic implemented in Providers or Controllers

---

# DDD-002 Entity Encapsulation

Severity: HIGH  
Scope: Domain

Rule

Entities must control their state through methods.

Required

- Private or protected setters
- Private backing collections
- State changes performed through domain methods

Forbidden

- Public mutable collections
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