# Patterns

These rules enforce implementation patterns used across the backend.

---

# PAT-001 Providers

Severity: HIGH  
Scope: Providers

Rule

Providers must implement `IProvider`.

Use an appropriate `ProviderBase` type when its Unit of Work or scoped resolution mechanics are needed. A focused provider may implement `IProvider` directly.

Forbidden

- Providers not implementing `IProvider`
- Providers implemented as static services

---

# PAT-002 Provider Responsibility

Severity: HIGH  
Scope: Providers

Rule

Providers coordinate application workflows.

Providers must interact with persistence only through repositories.

Forbidden

- Direct DbContext usage in providers
- SQL access in providers

---

# PAT-003 Repository Pattern

Severity: HIGH  
Scope: Domain, Data

Rule

Repository interfaces must be defined in the Domain layer.

Repository implementations must exist in the Data layer.

Forbidden

- Repository interfaces inside Data
- Providers implementing repositories

---

# PAT-004 Repository Access

Severity: HIGH  
Scope: Providers

Rule

Providers must access data only through repositories.

Forbidden

- DbContext usage outside the Data layer
- Direct SQL execution in Providers or Controllers

---

# PAT-005 Unit of Work

Severity: MEDIUM  
Scope: Providers

Rule

Multi-repository writes must commit through the Unit of Work. Use an explicit Unit of Work transaction only when participating contexts can share it and the operations must be atomic.

Allowed

- Creating compatible transactions through UnitOfWork
- Committing changes through UnitOfWork

Forbidden

- Claiming that sequential context commits are atomic without a compatible transaction
- Claiming that a database transaction rolls back external side effects

---

# PAT-006 View and DTO Usage

Severity: MEDIUM  
Scope: API layer

Rule

API controllers must expose View or DTO types.

Forbidden

- Exposing entities in controller responses
- Exposing DbContext types in API contracts

---

# PAT-007 Exception Handling

Severity: HIGH  
Scope: API layer

Rule

Exceptions must be handled through the centralized exception handling pipeline.

Forbidden

- Swallowing exceptions
- Returning raw exceptions to API clients
