# Inventory CRUD compatibility example

This self-contained solution is a small inventory API retained as compatibility coverage for the current Paradigm.Enterprise package line. Its projects consume `$(ParadigmEnterpriseVersion)` from the repository's `build/Paradigm.Version.props`, so the example stays aligned with the packages built from this source tree.

The example demonstrates current generic identifier and behavior-owned entity patterns through a deliberately small, manually composed controller. Follow the repository's [vertical slice tutorial](../../docs/sample-application.md) for a fuller explanation of current code shapes.

## Build

Restore and build the solution from the repository root:

```powershell
dotnet restore examples/inventory-crud/InventoryCrud.sln
dotnet build examples/inventory-crud/InventoryCrud.sln
```

The Web API project uses an in-memory database for the sample. It should not be treated as evidence that relational queries, constraints, stored procedures, authentication, or production configuration are correct.

## What remains useful

The project illustrates the broad request direction from controller to provider to repository and context. It also demonstrates explicit exception matcher registration and convention-based repository and provider discovery.

For current architecture and host guidance, read [Architecture](../../docs/architecture.md), [Dependency registration](../../docs/guides/dependency-registration.md), and [Secure host configuration](../../docs/guides/security-and-host.md).
