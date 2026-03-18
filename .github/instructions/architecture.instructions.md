---
applyTo: "src/**"
---

# Architecture Instructions

The system follows a strict layered architecture. Layer boundaries must never be violated.

The **HIGH severity (never violate)** rule summary and mandatory self-validation checklist live in:

- [critical-rules.instructions.md](critical-rules.instructions.md)

If guidance conflicts, **critical rules win**.

---

# Architecture Rules

All code must follow:

- /docs/rules/01-architecture.md
- /docs/rules/02-coding-standards.md
- /docs/rules/03-patterns.md
- /docs/rules/04-ddd.md

---

# Layered Architecture

```
Controllers → Providers → Repositories → Database
                ↓
              Domain
```

**Dependency Direction**: Outward layers depend on inner layers, never the reverse.

---

# Layer Responsibilities

**Controllers**
- Receive HTTP requests
- Validate input
- Call providers
- Return responses
- Must remain thin

❌ Forbidden:
- Business logic
- Repository usage
- DbContext usage

---

**Providers**
- Orchestrate application workflows
- Coordinate multiple repositories
- Manage transactions (Unit of Work)
- Transform entities ↔ DTOs

❌ Forbidden:
- Domain business rules
- Direct DbContext usage

---

**Repositories**
- Data access operations
- Query execution
- Entity persistence
- Mapping between database and domain models

❌ Forbidden:
- Business logic
- Direct usage by controllers

---

**Domain**
- Business behavior
- Business invariants
- Domain rules
- Entities and Value Objects

❌ Forbidden dependencies:
- DbContext
- Repositories
- Infrastructure services (logging, web frameworks, etc.)

---

# Critical Reminders (non-exhaustive)

For the canonical HIGH-severity list, see [critical-rules.instructions.md](critical-rules.instructions.md).

**Never allowed:**
- Controller → Repository (must go through Provider)
- Controller → DbContext
- Provider → DbContext (must use repositories)
- Domain → Infrastructure (must remain pure)

**Always required:**
- Controllers use Providers
- Providers use Repositories
- Repositories use DbContext
- Domain remains infrastructure-free

---

# Aggregate Boundaries

- Only aggregate roots may modify internal entities
- External components must interact through aggregate roots
- Do not allow direct modification of nested entities outside the aggregate

---

# Change Safety

When modifying existing code:

1. Preserve existing layer boundaries
2. Avoid introducing new cross-layer dependencies
3. Do not bypass providers or repositories
4. Prefer minimal architectural impact

---

# Before Implementation

Before generating code:

1. Identify which layer the change belongs to
2. Determine required provider/repository interactions
3. Ensure domain rules remain inside entities
4. Verify no architecture rules are violated

Always run the **Mandatory Self-Validation** checklist from [critical-rules.instructions.md](critical-rules.instructions.md) before returning code.

Do not generate code that violates architecture rules even if it appears simpler.