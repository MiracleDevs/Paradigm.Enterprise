# Understand the generated solution

The template creates solution folders for shared components, application modules, the host, tools, and tests. Folder numbers control presentation in Visual Studio; they do not become runtime dependencies.

The generated solution contains these projects:

| Project suffix | Responsibility |
| --- | --- |
| `Interfaces` | Generated and hand-written entity contracts |
| `Domain` | Entities, views, mappings, validation, and domain behavior |
| `Data` | `DbContext`, repositories, generated database code, and stored procedures |
| `Providers` | Application workflows and generated JSON contexts |
| `WebApi` | Composition root, middleware, controllers, and host policy |
| `HealthChecks` | Host-specific health response rendering |

The template also creates an empty tests solution folder. Add test projects by responsibility rather than putting all test types in one assembly by default.

## The unusual Interfaces reference

The Domain project references the Interfaces project as an analyzer. The analyzer inspects domain entities and generates matching interfaces. This is why the source-level dependency can look different from a conventional hand-written contract project.

The generator derives contracts from recognized `EntityBase` shapes. Scalar properties become interface properties, navigation collections become read-only accessors, and the identifier type is inferred when possible. Generated files are output, not customization points.

## Generated and partial code

Database reverse engineering produces context and entity files. Application behavior belongs in partial files that survive regeneration. Typical partial files contain domain validation, mapping overrides, navigation helpers, computed behavior, and additions to model configuration.

Before editing a generated file, inspect its header and the T4 template that owns it. A successful one-off edit can still be a defect if it disappears during the next scaffold.

## Composition root

The Web API project is the only place that should assemble the full application. It registers contexts, Unit of Work, repositories, providers, mappers, services, JSON contexts, exception matchers, health checks, and host middleware.

The generated host contains examples of Swagger, CORS, health responses, and authentication integration points. Treat these as editable host policy. The libraries do not require permissive CORS, a particular identity provider, or a particular health response format.

## Code generators

There are two distinct generation concerns. The analyzer derives application interfaces during compilation. The installable `paradigm` CLI creates JSON serializer contexts, stored-procedure mappers, and optionally a client from OpenAPI through separate `generate` subcommands. EF Core Power Tools and its T4 templates own database reverse engineering.

Keep those responsibilities separate when troubleshooting. Rebuilding the solution will not reverse engineer a database, and reverse engineering will not regenerate an OpenAPI client. Client repositories install one CLI tool rather than carrying an application-local generator executable.
