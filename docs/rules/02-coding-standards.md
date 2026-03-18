# Coding Standards

These rules define coding standards enforced during Pull Request reviews.

---

# CS-001 Naming Conventions

Severity: HIGH
Scope: All C#

Rule

- Types must use PascalCase
- Interfaces must start with `I`
- Public members must use PascalCase
- Private fields must use `_camelCase`

Forbidden

- Interfaces without `I` prefix
- Public members using camelCase
- Private fields without `_`

---

# CS-002 Async Method Naming

Severity: HIGH
Scope: All C#

Rule

Async methods returning `Task` or `Task<T>` must end with `Async`.

Forbidden

- Async methods without `Async` suffix
- `async void` methods outside event handlers

---

# CS-003 File Scoped Namespaces

Severity: MEDIUM
Scope: All C#

Rule

All files must use file-scoped namespaces.

Forbidden

Traditional block namespace declarations.

---

# CS-004 One Type Per File

Severity: MEDIUM
Scope: Production code

Rule

Each `.cs` file must contain a single public type.

Forbidden

Multiple unrelated public types in one file.

---

# CS-005 XML Documentation

Severity: MEDIUM
Scope: Public API

Rule

Public and protected members must include XML documentation.

Forbidden

Public API without documentation.

---

# CS-006 Nullable Reference Types

Severity: HIGH
Scope: All C#

Rule

Nullable reference types must be enabled and respected.

Forbidden

- Disabling nullable warnings globally
- Ignoring nullable warnings in modified code

---

# CS-007 Avoid Reflection

Severity: HIGH
Scope: All C#

Rule

Reflection must not be used.

Forbidden

- Assembly.GetTypes
- Activator.CreateInstance
- MethodInfo.Invoke

---

# CS-008 JSON Serialization

Severity: HIGH
Scope: API and shared contracts

Rule

JSON serialization must use generated `JsonSerializerContext`.

Forbidden

Reflection-based serialization.

---

# CS-009 Dynamic Types

Severity: HIGH
Scope: All C#

Rule

The `dynamic` type must not be used.

Forbidden

- dynamic
- ExpandoObject

---

# CS-010 No Blocking Async Calls

Severity: HIGH
Scope: Async methods

Rule

Blocking async calls must not be used.

Forbidden

- .Result
- .Wait()
- GetAwaiter().GetResult()

---

# CS-011 No Compiler Warnings

Severity: HIGH
Scope: All C#

Rule

New compiler warnings must not be introduced.

Forbidden

Committing code that adds warnings.

---

# CS-013 Favor Latest C# Features When Possible

Severity: MEDIUM
Scope: All C#

Rule

Favor the latest C# language features when they improve readability, safety, or performance and are supported by the project's target framework. Align with the repository's `.editorconfig` preferences.

Preferred (examples)

- Null checks: use `is null` / `is not null` instead of `== null` / `!= null`.
- Pattern matching: prefer switch expressions, property patterns, list patterns.
- Collection expressions: use `[a, b, c]` for arrays/lists where appropriate.
- Primary constructors for classes/structs that mainly store constructor parameters (where the style fits the type).
- Nullable reference types and null-checking patterns.
- Simple `using` statements (e.g., `using var`, using declaration).
- Expression-bodied members where they stay readable.
- Range/index operators (`^1`, `..`) when they clarify intent.
- Raw string literals or UTF-8 string literals when beneficial.

Allowed

- Using an older style when it matches existing code in the same file or when a newer feature would hurt readability (e.g., complex primary constructors).
- Restricting a feature (e.g., primary constructors) in specific layers if the team has agreed.

Forbidden

- Introducing obsolete or deprecated patterns in new code when a modern, supported alternative exists and is consistent with this rule.

---

# CS-015 Formatting and Style

Severity: LOW
Scope: All C#

Rule

- Follow the repository `.editorconfig` (in particular under `src/api`: 4 spaces, CRLF, and modern C# / pattern matching preferred).
- Run the configured formatter before committing.
- Prefer `var` for local variables; use explicit types only when it improves clarity or the type is not obvious from the right-hand side.
- Do not use braces `{}` for single-line `if`, `else`, or `using` bodies; write the single statement without braces.
- Use `is null` / `is not null` for null checks instead of `== null` / `!= null`.

Strings

- Use `string.IsNullOrWhiteSpace(...)` to check for empty/whitespace strings unless the business requires a different approach.
- Always use `StringComparison.OrdinalIgnoreCase` for string comparisons unless the business requires a different approach.

Allowed

- Minor style differences that do not affect readability; prefer running the formatter.
- Braces when the body has multiple statements or when team convention in a specific file uses them for consistency.

Forbidden

- Changing `.editorconfig` for the solution without team agreement.
- Large, purely formatting-only changes mixed with functional changes in the same PR.
