---
applyTo: "**"
excludeAgent: "code-review"
---

# Pull Request Development Instructions

These instructions apply when preparing or modifying code for a Pull Request.

For code under `src/**`, the HIGH-severity (never violate) summary and mandatory self-validation checklist live in:

- [critical-rules.instructions.md](critical-rules.instructions.md)

All changes must follow the engineering rules defined in `/docs/rules`.

---

# Change Strategy

When implementing a change:

1. Respect existing architecture.
2. Make minimal modifications.
3. Avoid introducing new dependencies across layers.

Do not bypass Providers or Repositories.

---

# Safety Rules

When modifying code:

- Do not introduce new architectural violations
- Do not move business logic into controllers
- Do not introduce DbContext usage outside the Data layer
- Run the **Mandatory Self-Validation** checklist in [critical-rules.instructions.md](critical-rules.instructions.md) before returning code

---

# Code Consistency

New code must follow:

- Naming conventions
- DTO and entity rules
- Async method conventions
- File organization rules

All changes must compile without warnings.