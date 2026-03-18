# Patterns

These rules enforce implementation patterns used across the backend.

---

# PAT-001 Providers

Severity: HIGH  
Scope: Providers

Rule

Providers must implement `IProvider`.

Providers must inherit from a `ProviderBase` type.

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

Multi-repository operations must use UnitOfWork transactions.

Allowed

- Creating transactions through UnitOfWork
- Committing changes through UnitOfWork

Forbidden

- Manual transaction management

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