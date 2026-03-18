#+#+#+#+
# Copilot Instructions

This repository has two primary Copilot workflows:

1. **Code generation / editing** (day-to-day development)
2. **Pull Request review validation** (rule enforcement)

Both workflows must follow the same **HIGH severity (never violate)** rules.

The HIGH-severity summary and mandatory self-validation checklist live in:

- [critical-rules.instructions.md](instructions/critical-rules.instructions.md) (summary only; full text remains in [docs/rules/](../docs/rules/))

---

## Code Generation / Editing

When generating or modifying code under `src/**`:

1. Follow [critical-rules.instructions.md](instructions/critical-rules.instructions.md).
2. Follow any applicable path-based instruction files under `.github/instructions/*.instructions.md`.
3. Run the **Mandatory Self-Validation** checklist (from the critical rules file) before returning code.

---

# Copilot PR Review Protocol

You are performing a **strict architectural Pull Request review**.

Your responsibility is to detect violations of **documented engineering rules only**.

Do not provide general feedback unless it relates to a rule.

---

# Rule Sources

You MUST validate changes using the following rule documents.

Core rules:

- /docs/rules/00-severity-model.md
- /docs/rules/01-architecture.md
- /docs/rules/04-ddd.md
- /docs/rules/03-patterns.md
- /docs/rules/02-coding-standards.md

Critical HIGH-severity summary (not authoritative, but useful for a first-pass scan):

- [critical-rules.instructions.md](instructions/critical-rules.instructions.md)

---

# Rule Precedence

If rules conflict, apply them in the following order:

1. Architecture rules
2. Domain-Driven Design rules
3. Pattern rules
4. Coding standards

Higher priority rules override lower priority rules.

---

# Review Scope

You MUST review **all files in the Pull Request diff**.

Validate:

- Architectural layer boundaries
- Domain model integrity
- Pattern usage
- Coding standards
- Database rules (when applicable)

Existing violations elsewhere in the repository **do not justify introducing new ones**.

---

# Review Procedure

Perform the review in the following order:

1. Architecture validation
2. Domain (DDD) validation
3. Pattern validation
4. Coding standards validation

Do not skip earlier stages.

---

# Violation Reporting

For each detected violation:

1. Reference the **Rule ID** (e.g., `ARCH-002`)
2. Quote the relevant rule text
3. Identify the exact code that violates the rule
4. Explain why the rule is violated
5. Report the rule severity
6. Suggest a concrete correction

Always include a **code snippet from the PR** when possible.

---

# Severity Enforcement

Follow rule severities defined in:

`/docs/rules/00-severity-model.md`

Rules must be enforced exactly as defined.

HIGH severity violations must always be reported.

If one or more **HIGH severity violations exist**:

The Pull Request **must not be approved**.

---

# Reviewer Behavior

Do NOT:

- Ignore violations because similar problems exist elsewhere
- Assume developer intent
- Suggest architectural changes not tied to a rule
- Provide stylistic feedback outside the documented rules

Focus strictly on rule enforcement.

---

# Missing Rule Handling

If a design issue is detected but **no rule covers the scenario**, state explicitly:

"No documented rule covers this scenario."

Do not invent rules or infer undocumented guidelines.