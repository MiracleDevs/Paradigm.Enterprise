# Paradigm Good Coding Practices

Apply these rules to handwritten application and framework code. Treat generated code as a documented exception: change its generator or template when possible, and never make a generated file look handwritten merely to satisfy style guidance.

## Source layout

- Keep one top-level semantic type per file. A class, record, struct, interface, enum, or delegate gets its own file named after that type. Nested helper types may remain with their owner when they have no independent meaning.
- Organize files by business meaning, capability, or bounded context. Split a folder when it becomes a mixed catalog or is difficult to scan; do not create broad `Common`, `Helpers`, or `Utils` collections without one coherent responsibility.
- Prefer domain-bounded EF contexts such as `AccountingDbContext`, `InventoryDbContext`, and a deliberately small `SharedDbContext`. Do not create an application-wide context merely for convenience. A shared context must own genuinely shared data, not become a cross-context shortcut.

## Class member layout

Order class members exactly as follows. Wrap every non-empty category in the matching region; omit empty regions. Keep the region names and capitalization exact. Leave exactly one empty line after `#region`, exactly one empty line before `#endregion`, and exactly one empty line between adjacent regions.

1. `#region Nested Types`
2. `#region Constants`
3. `#region Fields`
4. `#region Properties`
5. `#region Constructors`
6. `#region Static Constructors`
7. `#region Public Methods`
8. `#region Overrides`
9. `#region Protected Methods`
10. `#region Private Methods`
11. `#region Event Handlers`

Place an override in `Overrides` regardless of its declared accessibility. Place callbacks that handle an event in `Event Handlers`, after other private methods. Do not use thematic regions in a production class as a substitute for this order. Tests may group test cases by behavior, but helper members still follow the member order.

```csharp
    #region Fields

    private readonly object _gate = new();

    #endregion

    #region Properties

    public string Name { get; }

    #endregion
```

## Small, narrow, immutable surfaces

- Apply least visibility. Start with `private`; use `internal`, `protected`, or `public` only when a known caller, extension point, serialization requirement, framework convention, or discovery rule needs it.
- Prefer sealed implementation classes unless inheritance is an intentional supported extension point.
- Prefer immutable values and state: `const` for compile-time constants, `readonly` fields, getter-only or `init` properties, immutable records/value objects, and read-only collection exposure. Introduce mutation only behind behavior that owns an invariant or because a documented binder, serializer, ORM, or generated-code boundary requires it.
- Keep mutable request/view models at the transport boundary. Do not let their mutability leak into entities or long-lived services.

## Browser and API security

- For browser authentication, prefer a backend-for-frontend or server session with an encrypted, `Secure`, `HttpOnly` cookie over tokens in `localStorage`. Choose the narrowest workable `SameSite` value and a constrained `Path`/`Domain`; use short lifetimes, rotation, revocation, and server-side validation appropriate to the threat model.
- Cookie authentication requires CSRF protection for state-changing requests. Use ASP.NET Core antiforgery support or a reviewed equivalent, validate the origin where appropriate, and keep CORS explicit and narrow. Cookies reduce token theft through JavaScript but do not remove XSS or CSRF risk.
- Use standards-based OIDC/OAuth flows. Public browser clients use Authorization Code with PKCE; never implement cryptography, token parsing, password storage, or authentication protocols as ad hoc custom code.
- Machine-to-machine and non-browser APIs may require bearer tokens rather than cookies. Decide from the client and threat model instead of applying the browser rule universally.
- Use HTTPS, safe secret storage, output encoding, input limits, a restrictive Content Security Policy where applicable, safe error responses, dependency auditing, and authorization at every protected operation. Never log tokens, cookie values, credentials, or unnecessary personal data.
- Re-check current primary guidance before designing a new security feature. Start with [ASP.NET Core SameSite guidance](https://learn.microsoft.com/aspnet/core/security/samesite), [ASP.NET Core antiforgery guidance](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery), [OWASP browser storage guidance](https://cheatsheetseries.owasp.org/cheatsheets/HTML5_Security_Cheat_Sheet.html), and the current [OAuth browser-based apps guidance](https://datatracker.ietf.org/doc/draft-ietf-oauth-browser-based-apps/).

## Dependencies

- Implement a small, well-bounded problem with platform or custom code when that code is simpler to understand, test, and maintain than a dependency. Do not reimplement security, cryptography, complex file formats, standards, database providers, or other specialist behavior merely to avoid a package.
- Before adding any NuGet `PackageReference` or running `dotnet add package`, explain the need and alternatives, verify the package is open source, report its license and official repository, prefer MIT when otherwise comparable, inspect maintenance/security/transitive-dependency risk, and obtain the user's explicit permission. Do not edit the project until permission is granted.
- Prefer the .NET and ASP.NET Core shared frameworks and packages already approved by the consuming repository. A package that already exists elsewhere in a solution is not automatically approved for a new project; explain the new coupling before adding it.

## Operability by default

- Most APIs require structured logging, distributed trace propagation, meaningful metrics, and liveness/readiness health checks. Record why an API legitimately omits one.
- Prefer `ILogger`, `System.Diagnostics.ActivitySource`, `System.Diagnostics.Metrics`, ASP.NET Core/OpenTelemetry integration already present in the host, and `AddHealthChecks` before custom infrastructure.
- Trace an incoming request through its application use case and external dependencies. Preserve correlation without inventing a parallel identifier when the standard trace context is sufficient.
- Health checks must be cheap, bounded by timeouts, free of secrets, and split between process liveness and dependency-aware readiness. Do not turn health endpoints into broad diagnostics or run destructive checks.
- Test telemetry names and critical health-state transitions where operators depend on them; do not assert incidental log prose.
