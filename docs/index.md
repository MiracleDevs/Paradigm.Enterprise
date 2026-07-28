# Paradigm.Enterprise

Paradigm.Enterprise is a set of .NET libraries for APIs that use a layered, domain-driven design. It supplies the repetitive mechanics around entities, repositories, providers, transactions, HTTP controllers, exception handling, database access, and common infrastructure services. An application still owns its domain language, security policy, database design, and operational decisions.

The framework is deliberately opinionated. A request normally enters through a controller, crosses a provider that coordinates the use case, reaches persistence through repositories, and commits through a Unit of Work. Domain entities remain responsible for their own invariants. Read-heavy paths can use separate view models and repositories so that query design is not constrained by the write model.

If you are starting a new API, begin with [Create a solution](tutorials/create-solution.md). If you are joining an existing application, read the [architecture](architecture.md) and then follow the [vertical slice tutorial](sample-application.md). The [package matrix](reference/packages.md) helps when you need only one part of the framework.

## What the framework does not decide

Paradigm.Enterprise does not provide an authentication system, choose authorization rules, create database schemas, or make permissive host settings safe for production. The Visual Studio template contains integration points and development defaults, but those are starting points for an application team to review.

The generated [API reference](reference/api.md) describes the current public surface. These guides explain why the surface exists and how the pieces work together.

## Documentation sources

The library source is the authority for signatures and runtime behavior. The Visual Studio template is the authority for generated project structure and scaffolding workflow. The examples and training material are used only to understand recurring practices. Their names, domain logic, and project-specific policies are not part of this documentation.
