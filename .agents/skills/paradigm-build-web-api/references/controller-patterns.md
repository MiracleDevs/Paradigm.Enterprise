# Controller patterns

## Protected route

Use `[ApiController]`, route metadata, application authorization metadata, and `ControllerBase`. Constructor-inject the provider contract. Translate successful results at the edge and let configured exception handling translate known failures.

## Deliberately anonymous generic CRUD

Use the exact read/edit base for the installed framework version and carry the same identifier type. Enable endpoint exposure control. Document why anonymous access is intentional and test it.

## Independently filtered Paradigm base

`ApiAuthorizationAttribute` is a separately enforced framework filter and can secure a controller that inherits `AllowAnonymous` when correctly configured. Treat its client-secret model as a distinct authorization scheme; test missing, invalid, and valid credentials.

## Source-generated JSON

Keep serializer contexts application-owned or generator-owned. Register their resolvers with MVC and cover each endpoint in serialization tests; missing nested metadata often appears only at runtime.
