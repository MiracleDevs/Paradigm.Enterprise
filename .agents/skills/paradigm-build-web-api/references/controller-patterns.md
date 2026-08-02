# Controller patterns

## Protected route

Use `[ApiController]`, route metadata, application authorization metadata, and `ControllerBase`. Constructor-inject the provider contract. Translate successful results at the edge and let configured exception handling translate known failures.

## Deliberately anonymous generic CRUD

Use the exact read/edit base exposed by the installed packages and carry the same identifier type. Enable endpoint exposure control. Document why anonymous access is intentional and test it.

## Independently filtered Paradigm base

`ApiAuthorizationAttribute` is a separately enforced framework filter and can secure a controller that inherits `AllowAnonymous` when correctly configured. Treat its client-secret model as a distinct authorization scheme; test missing, invalid, and valid credentials.

## Source-generated JSON

Keep serializer contexts application-owned or generator-owned. Register their resolvers with MVC and cover each endpoint in serialization tests; missing nested metadata often appears only at runtime.

## Microsoft Entra actors

Authorize the token before resolving or persisting the current application user. If the application is
delegated-user-only, apply that requirement to default, fallback, and every named business policy so an
application token cannot reach user provisioning. Cover explicit user/app `idtyp`, legacy `scp`, legacy
roles-only application tokens with distinct `oid`/`sub`, combined scope/role user tokens, unknown identity
types, and missing actor identifiers.
