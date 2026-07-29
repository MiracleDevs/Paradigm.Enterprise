# Compatibility example

This solution is a small, generic API sample retained to verify compatibility with the historical `1.0.0` Enterprise package line. It demonstrates the older integer-identifier base types and a manually composed controller.

It is not the canonical example for the current source. Current APIs use explicit identifier type parameters throughout repositories, providers, and controller bases. Follow the repository's [vertical slice tutorial](../docs/sample-application.md) for current code shapes.

## Build

Restore and build the solution from the repository root:

```powershell
dotnet restore example/ExampleApp.sln
dotnet build example/ExampleApp.sln
```

The Web API project uses an in-memory database for the sample. It should not be treated as evidence that relational queries, constraints, stored procedures, authentication, or production configuration are correct.

## What remains useful

The project still illustrates the broad request direction from controller to provider to repository and context. It also demonstrates explicit exception matcher registration and convention-based repository and provider discovery as they existed in the pinned package version.

For current architecture and host guidance, read [Architecture](../docs/architecture.md), [Dependency registration](../docs/guides/dependency-registration.md), and [Secure host configuration](../docs/guides/security-and-host.md).
