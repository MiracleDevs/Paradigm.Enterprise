---
applyTo: "src/**"
---

# Critical Rules (HIGH Severity)

This file defines the **HIGH severity rules that must never be violated**.

It is a **generation-time guardrail**: when creating or editing code under src/**, follow these rules *before* returning code.

Applicability by area:

- src/api/** (C#): Architecture + DDD + Patterns + C# Coding Standards apply.
- src/database/** (SQL): Database rules apply.
- src/client/** (TypeScript/Angular): Follow [frontend.instructions.md](frontend.instructions.md); the backend-specific rules below generally do not apply unless you are editing shared C# or SQL artifacts.

Full rule text lives in [docs/rules/](../../docs/rules/). This file intentionally includes only short summaries + rule IDs.

---

# Rule Sources (authoritative)

- [docs/rules/01-architecture.md](../../docs/rules/01-architecture.md)
- [docs/rules/04-ddd.md](../../docs/rules/04-ddd.md)
- [docs/rules/03-patterns.md](../../docs/rules/03-patterns.md)
- [docs/rules/02-coding-standards.md](../../docs/rules/02-coding-standards.md)
- (SQL only) [docs/rules/database/](../../docs/rules/database/)

---

# Never-Violate Rules (HIGH)

## Architecture (always)

- **ARCH-001**: Enforce strict layer dependency direction: Hosts → Providers → Data → Domain → Interfaces.
- **ARCH-002**: Controllers must be thin; no business logic, no repositories, no DbContext; controllers inherit `ApiControllerBase<TProvider>`.
- **ARCH-003**: Providers implement application orchestration; controllers must not implement provider logic; data layer must not contain business logic.
- **ARCH-004**: Domain is infrastructure-independent; no references to Data/Providers/Hosts/framework.
- **ARCH-005**: Data layer is persistence-only; no business logic or workflows.
- **ARCH-006**: Repository interfaces in Domain; implementations in Data; providers never use DbContext directly.
- **ARCH-007**: Use Dependency Injection; no manual instantiation or service locator usage outside DI.

## DDD (Domain layer)

- **DDD-001**: Entities encapsulate behavior; no anemic data containers; do not move business behavior into Providers/Controllers.
- **DDD-002**: Entity state is controlled through methods; avoid public setters/mutable collections.
- **DDD-004**: Preserve aggregate integrity; cross-aggregate modification is forbidden.
- **DDD-005**: Domain purity; no DI, DbContext, repositories, logging, or framework services in entities.

## Patterns (backend)

- **PAT-001**: Providers implement `IProvider` and inherit `ProviderBase`; no static providers.
- **PAT-002**: Providers coordinate workflows; persistence only through repositories; no DbContext/SQL in providers.
- **PAT-003**: Repository interfaces in Domain; implementations in Data.
- **PAT-004**: DbContext/SQL must remain in Data layer; never in Providers or Controllers.
- **PAT-007**: Use centralized exception handling pipeline; do not swallow exceptions or return raw exceptions to clients.

## Coding Standards (C#)

- **CS-001**: Enforce naming conventions (PascalCase, `I`-prefixed interfaces, `_camelCase` private fields).
- **CS-002**: Async methods returning `Task`/`Task<T>` must end with `Async`; avoid `async void` (except event handlers).
- **CS-006**: Nullable reference types must be enabled and respected; do not add nullable warnings.
- **CS-007**: Reflection is forbidden.
- **CS-008**: JSON serialization must use generated `JsonSerializerContext` (no reflection-based serialization).
- **CS-009**: `dynamic` is forbidden.
- **CS-010**: No blocking async calls: `.Result`, `.Wait()`, `GetAwaiter().GetResult()`.
- **CS-011**: Do not introduce new compiler warnings.

## Database (SQL under src/database/**)

- **DB-ARCH-001**: DB-first source of truth; schema lives in the database project.
- **DB-INTEGRITY-001**: Foreign keys required.
- **DB-INTEGRITY-003**: Integrity over convenience; do not drop constraints to simplify queries.
- **DB-NAME-002**: Table-backed view naming must be `<TableName>View`.
- **DB-VIEW-001**: Every table must have a corresponding view.
- **DB-VIEW-002**: Views must reference base tables only (no view-on-view).

---

# Mandatory Self-Validation (run before returning code)

Before completing any task that creates/edits code under src/**:

1. **Identify the layer and file type** being changed (Controller / Provider / Data / Domain / SQL / Frontend).
2. **Validate against the HIGH rules above** that apply to that layer and language.
3. **Verify architectural boundaries** (ARCH-001) by checking references/imports/usings:
   - Controllers only call Providers.
   - Providers only use repositories / Unit of Work.
   - DbContext and SQL stay in Data.
   - Domain stays pure.
4. **Fix violations before returning**. If the requested change would require violating a HIGH rule, do not generate violating code; propose a compliant alternative.
