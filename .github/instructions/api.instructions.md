---
applyTo: "src/api/Controllers/**"
---

# API Controller Instructions

These instructions apply specifically to ASP.NET API controllers.

For general architecture rules, see architecture.instructions.md.

Critical HIGH-severity rules (must never be violated) and the mandatory self-validation checklist:

- [critical-rules.instructions.md](critical-rules.instructions.md)

---

# Controller Patterns

Use `ApiControllerBase<TProvider>` as base class.

Each controller should have one primary provider injected via constructor.

Example:
```csharp
public class UserController : ApiControllerBase<UserProvider>
{
    public UserController(UserProvider provider) : base(provider) { }
}
```

---

# API Contracts

API boundaries must use DTO or View types.

Rules:
- DTOs must be `record` types
- Entities must not be returned directly to clients
- DbContext types must not appear in API contracts

---

# Async Conventions

All I/O operations must be asynchronous.

Rules:
- Async methods must end with `Async` suffix
- Return `Task<IActionResult>` or `Task<ActionResult<T>>`
- Avoid `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`

---

# Exception Handling (HIGH)

Follow PAT-007: use the centralized exception handling pipeline.

Forbidden:

- Swallowing exceptions
- Returning raw exceptions to clients

If you must translate failures into HTTP results, do so through the established error/exception pipeline patterns used by the API.