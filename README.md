# Paradigm.Enterprise

A comprehensive framework that provides base classes and utilities for building .NET WebApi applications following enterprise best practices and design patterns.

## Documentation

For detailed documentation about each component of the framework, please visit the following documentation pages:

- [Overview](docs/overview.md) - Framework overview and core concepts
- [Architecture](docs/architecture.md) - Detailed architectural overview and design decisions
- [Interfaces](docs/interfaces.md) - Core interfaces
- [Domain](docs/domain.md) - Domain entities and business logic
- [Data](docs/data.md) - Data access and repositories
- [Data Providers](docs/data-providers.md) - Database-specific implementations
- [Providers](docs/providers.md) - Business logic providers
- [Services](docs/services.md) - Service abstractions (Cache, Email, BlobStorage)
- [WebApi](docs/webapi.md) - API controllers and middleware
- [Tests](docs/tests.md) - Testing infrastructure and examples
- [Code Generator](docs/code-generator.md) - Automated code generation utilities
- [Sample Application](docs/sample-application.md) - Complete example application using the framework

## Nuget Packages

| Library    | Nuget | Install
|-|-|-|
| Data       | [![NuGet](https://img.shields.io/nuget/v/Paradigm.Enterprise.Data.svg)](https://www.nuget.org/packages/Paradigm.Enterprise.Data/)            | `Install-Package Paradigm.Enterprise.Data` |
| Data.SqlServer       | [![NuGet](https://img.shields.io/nuget/v/Paradigm.Enterprise.Data.SqlServer.svg)](https://www.nuget.org/packages/Paradigm.Enterprise.Data.SqlServer/)            | `Install-Package Paradigm.Enterprise.Data.SqlServer` |
| Data.PostgreSql       | [![NuGet](https://img.shields.io/nuget/v/Paradigm.Enterprise.Data.PostgreSql.svg)](https://www.nuget.org/packages/Paradigm.Enterprise.Data.PostgreSql/)            | `Install-Package Paradigm.Enterprise.Data.PostgreSql` |
| Domain       | [![NuGet](https://img.shields.io/nuget/v/Paradigm.Enterprise.Domain.svg)](https://www.nuget.org/packages/Paradigm.Enterprise.Domain/)            | `Install-Package Paradigm.Enterprise.Domain` |
| Interfaces      | [![NuGet](https://img.shields.io/nuget/v/Paradigm.Enterprise.Interfaces.svg)](https://www.nuget.org/packages/Paradigm.Enterprise.Interfaces/)      | `Install-Package Paradigm.Enterprise.Interfaces` |
| Providers | [![NuGet](https://img.shields.io/nuget/v/Paradigm.Enterprise.Providers.svg)](https://www.nuget.org/packages/Paradigm.Enterprise.Providers/) | `Install-Package Paradigm.Enterprise.Providers` |
| Services.BlobStorage | [![NuGet](https://img.shields.io/nuget/v/Paradigm.Enterprise.Services.BlobStorage.svg)](https://www.nuget.org/packages/Paradigm.Enterprise.Services.BlobStorage/)  | `Install-Package Paradigm.Enterprise.Services.BlobStorage` |
| Services.Cache | [![NuGet](https://img.shields.io/nuget/v/Paradigm.Enterprise.Services.Cache.svg)](https://www.nuget.org/packages/Paradigm.Enterprise.Services.Cache/)  | `Install-Package Paradigm.Enterprise.Services.Cache` |
| Services.Email | [![NuGet](https://img.shields.io/nuget/v/Paradigm.Enterprise.Services.Email.svg)](https://www.nuget.org/packages/Paradigm.Enterprise.Services.Email/)  | `Install-Package Paradigm.Enterprise.Services.Email` |
| Services.Core | [![NuGet](https://img.shields.io/nuget/v/Paradigm.Enterprise.Services.Core.svg)](https://www.nuget.org/packages/Paradigm.Enterprise.Services.Core/)  | `Install-Package Paradigm.Enterprise.Services.Core` |
| WebApi | [![NuGet](https://img.shields.io/nuget/v/Paradigm.Enterprise.WebApi.svg)](https://www.nuget.org/packages/Paradigm.Enterprise.WebApi/)  | `Install-Package Paradigm.Enterprise.WebApi` |
| CodeGenerator | [![NuGet](https://img.shields.io/nuget/v/Paradigm.Enterprise.CodeGenerator.svg)](https://www.nuget.org/packages/Paradigm.Enterprise.CodeGenerator/)  | `dotnet tool install --global Paradigm.Enterprise.CodeGenerator` |

## Getting Started

To get started with the Paradigm.Enterprise framework, add the relevant NuGet packages to your project:

```shell
Install-Package Paradigm.Enterprise.WebApi
```

This will bring in all the necessary dependencies for a typical WebAPI project.

See the [Sample Application](docs/sample-application.md) for a complete example of using the framework.

## Nuget publish process

After modifying the solution you can change the version by executing:
```shell
$ cd ./build
$ ./increment.version.sh "1.0.0" "1.0.1"
```
where the first argument ("1.0.0") is the current version and the second one ("1.0.1") is the new version number.

To publish to nuget you need to execute the following script:
```shell
$ cd ./build
$ ./publish.nuget.sh "{nuget-secret-key}"
```

## Change log

Version `1.0.11`
- Modified `Data` to allow to set the command timeout when executing a stored procedure.

Version `1.0.10`
- Adjusted GetSearchPaginatedFunction in ReadRepositoryBase.

Version `1.0.9`
- Modified `Data.PostgreSql` to automatically create a transaction when execute a stored procedure.

Version `1.0.8`
- Added new methods in `EditProviderBase` to execute on save before the view is mapped to an entity.

Version `1.0.7`
- Added GetArray method to read values from database array fields.

Version `1.0.6`
- `IAuditableEntity` interface refactor to support `DateTime` or `DateTimeOffset` types.

Version `1.0.5`
- Removed `ZiggyCreatures.FusionCache` library from `Services.Cache`.

Version `1.0.4`
- Fixed issue in `Services.Cache` package. Included packages debug symbols.

Version `1.0.3`
- Fixed DataReaderMapperFactory bug in `Data` package. Added new methods in `Services.Cache` package.

Version `1.0.2`
- Fixed ParameterMapperFactory bug in `Data.SqlServer` and `Data.PostgreSql` packages.

Version `1.0.1`
- Implemented `ZiggyCreatures.FusionCache` library in `Services.Cache`. Implemented `Data.PostgreSql` package.

Version `1.0.0`
- Uploaded first version of the Paradigm.Enterprise.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.