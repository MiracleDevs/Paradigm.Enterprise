# Validation and error handling

Validation occurs at several boundaries because each boundary protects a different concern. HTTP model binding checks request shape. Domain entities protect invariants. Providers perform checks that need repositories or services. Database constraints remain the final persistence guard.

## Domain validation

The generic edit provider calls `Validate` after mapping a view into an entity and before staging the repository operation. `DomainValidator` can collect several failures and throw one `DomainException`.

```csharp
public override void Validate()
{
    var validator = new DomainValidator();
    validator.Assert(!string.IsNullOrWhiteSpace(Name), "Name is required.");
    validator.Assert(Name.Length <= 200, "Name is too long.");
    validator.ThrowIfAny();
}
```

Do not rely on controller validation for an invariant. The same provider may be called outside HTTP.

## Provider checks

A uniqueness check, permissions lookup, or external policy may need a repository or service. Place that work in a provider method or lifecycle hook, then let the entity validate its own resulting state.

Avoid resolving infrastructure from an entity's mapping or validation method. Although mapping receives `IServiceProvider`, that parameter exists for compatibility with the framework mapping model, not as permission to turn the entity into a service locator.

## Exception translation

The library defines provider exceptions for not found, unauthenticated, and unauthorized outcomes. Domain code can throw `DomainException`. Database providers may throw constraint exceptions. `IExceptionMatcher` implementations translate recognized exceptions into the response model chosen by the host.

```csharp
builder.Services.AddScoped<IExceptionHandler>(_ =>
{
    var handler = new ExceptionHandler(typeof(ExceptionResources));
    handler.AddMatcher(new DomainExceptionMatcher());
    handler.AddMatcher(new UniqueConstraintMatcher());
    return handler;
});
```

The matcher types in this example are application types. Create them around the database provider and response contract actually used by the host.

Use a fallback that returns a safe message and logs the original exception. Do not send stack traces, SQL, connection data, or internal type names to callers.

## Middleware placement

Call `UseOwnExceptionHandler` before middleware and endpoints whose exceptions it must translate. Authentication middleware can have its own challenge and forbid behavior, so review the order against the chosen authentication stack rather than copying a template mechanically.
