# Severity Model

This document defines the meaning of rule severity levels used in Pull Request reviews.

---

## HIGH Severity

HIGH severity indicates architectural or correctness violations.

A Pull Request must not be approved when HIGH severity violations exist.

Examples:

- Architecture violations
- Domain model violations
- Data integrity risks
- Security risks
- Async or concurrency misuse
- Breaking established patterns

Required Action

- The violation must be fixed before merge.

---

## MEDIUM Severity

MEDIUM severity indicates maintainability or consistency issues.

The code may function correctly but does not follow project conventions.

Examples:

- Naming convention violations
- Missing documentation
- Incorrect DTO patterns
- Minor architectural drift

Required Action

- Should be corrected during review.

---

## LOW Severity

LOW severity indicates style or organizational improvements.

Examples:

- File organization
- Minor readability issues
- Code formatting preferences

Required Action

- Optional improvement.