# Architecture Rules

These rules define the architectural constraints for backend services.

All Pull Request reviews MUST validate these rules.

---

# ARCH-001: Layer Dependency Direction

**Severity:** HIGH  
**Scope:** Entire solution

**Rule**

Dependencies must follow this strict direction:


Hosts → Providers → Data → Domain → Interfaces


**Allowed**

- Hosts referencing Providers and Shared libraries.
- Providers referencing Data.
- Data referencing Domain.
- Domain referencing Interfaces.

**Forbidden**

- Hosts referencing Data or Domain directly.
- Providers referencing Hosts.
- Data referencing Providers or Hosts.
- Domain referencing Data, Providers, or Hosts.

---

# ARCH-002: Controllers Must Be Thin

**Severity:** HIGH  
**Scope:** Controllers

**Rule**

Controllers must act only as API endpoints and orchestration entry points.

Controllers must inherit from:


ApiControllerBase<TProvider>


**Allowed**

- Input validation.
- Calling Providers.
- Returning DTOs or Views.

**Forbidden**

- Business logic in controllers.
- Direct repository access.
- Direct DbContext access.
- Complex workflow orchestration.

---

# ARCH-003: Providers Implement Application Logic

**Severity:** HIGH  
**Scope:** Providers

**Rule**

Providers implement application-level orchestration and workflows.

Providers coordinate:

- multiple repositories
- multiple entities
- external services

**Allowed**

- Using repositories.
- Using Unit of Work.
- Calling other providers via DI.

**Forbidden**

- Controllers implementing provider logic.
- Data layer implementing business logic.

---

# ARCH-004: Domain Layer Isolation

**Severity:** HIGH  
**Scope:** Domain

**Rule**

Domain must be independent from infrastructure.

**Allowed**

- Entities
- Value objects
- Domain validation
- Domain behavior

**Forbidden**

Domain must not reference:

- Data
- Providers
- Hosts
- ASP.NET Core
- Infrastructure libraries

---

# ARCH-005: Data Layer Responsibilities

**Severity:** HIGH  
**Scope:** Data layer

**Rule**

The Data layer is responsible only for persistence.

**Allowed**

- Repository implementations
- EF Core DbContext
- Query mapping
- Stored procedure execution

**Forbidden**

- Business logic
- Cross-entity workflows
- HTTP logic
- Controller dependencies

---

# ARCH-006: Repository Pattern

**Severity:** HIGH  
**Scope:** Data, Domain

**Rule**

Repository interfaces belong to the Domain layer.  
Repository implementations belong to the Data layer.

**Allowed**


Domain → IRepository
Data → Repository implementation


**Forbidden**

- Repositories defined in Data.
- Providers directly accessing DbContext.

---

# ARCH-007: Dependency Injection

**Severity:** HIGH  
**Scope:** Hosts

**Rule**

All services must be resolved through Dependency Injection.

**Allowed**

- Constructor injection
- Service registration in host startup

**Forbidden**

- Manual instantiation of services
- Service locator patterns outside DI container

---

# ARCH-008: Multiple DbContexts

**Severity:** MEDIUM  
**Scope:** Data

**Rule**

The system must use multiple DbContexts grouped by feature.

**Allowed**

- Feature-specific DbContexts
- Contexts scoped to bounded domains

**Forbidden**

- Single monolithic DbContext containing all entities

---

# ARCH-009: Provider Interaction

**Severity:** MEDIUM  
**Scope:** Providers

**Rule**

Providers may depend on other Providers only through Dependency Injection.

**Allowed**


ProviderA → ProviderB (via DI)


**Forbidden**

- Manual provider instantiation
- Static access to providers

---

# Summary

| Layer | Responsibility |
|------|------|
| Hosts | HTTP pipeline and controllers |
| Providers | Application workflows |
| Data | Persistence |
| Domain | Business rules |
| Interfaces | Contracts |