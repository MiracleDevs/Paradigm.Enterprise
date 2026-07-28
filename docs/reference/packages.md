# Package reference

Choose packages by the layer or infrastructure capability that consumes them. The Web API package brings the main application stack transitively, but explicit references in the owning project make dependencies easier to understand.

| Package | Target framework | Purpose |
| --- | --- | --- |
| `Paradigm.Enterprise.Interfaces` | `netstandard2.0` | Minimal entity and audit contracts |
| `Paradigm.Enterprise.Domain` | `net9.0`, `net10.0` | Entities, repositories contracts, mapping, validation, DTOs, state, and Unit of Work contracts |
| `Paradigm.Enterprise.Data` | `net10.0` | Entity Framework contexts, repository bases, Unit of Work, and data-reader mapping |
| `Paradigm.Enterprise.Data.SqlServer` | `net10.0` | SQL Server context registration and stored-procedure support |
| `Paradigm.Enterprise.Data.PostgreSql` | `net10.0` | PostgreSQL context registration and stored-procedure support |
| `Paradigm.Enterprise.Providers` | `net10.0` | Application provider contracts and read/edit base workflows |
| `Paradigm.Enterprise.WebApi` | `net10.0` | Controller bases, filters, middleware, and dependency discovery |
| `Paradigm.Enterprise.Services.Core` | `net9.0`, `net10.0` | Shared service marker contract |
| `Paradigm.Enterprise.Services.Cache` | `net9.0`, `net10.0` | Redis cache wrapper and health check |
| `Paradigm.Enterprise.Services.Email` | `net9.0`, `net10.0` | Azure Communication Services email wrapper |
| `Paradigm.Enterprise.Services.BlobStorage` | `net9.0`, `net10.0` | Azure Blob Storage wrapper and health check |
| `Paradigm.Enterprise.Services.TableReader` | `net9.0`, `net10.0` | Tabular file readers and writers |
| `Paradigm.Enterprise.CodeGenerator` | `net10.0` | JSON, stored-procedure, and OpenAPI client generation tool |

Install a package with the .NET CLI:

```powershell
dotnet add package Paradigm.Enterprise.WebApi
```

Use one compatible package version across the application dependency chain. Central package management is recommended for a multi-project solution.

## Common selections

A generated SQL Server API normally references Interfaces from its contract project, Domain from its domain project, Data and Data.SqlServer from its data project, Providers from its application project, and WebApi from the host. Add service packages only where their abstractions or configuration are used.

A library or worker can use Domain, Data, Providers, or a service package without referencing WebApi. Providers are intentionally reusable outside HTTP.

See the [generated API reference](api.md) for the complete public surface.
