# Paradigm.Enterprise

Paradigm.Enterprise is a set of .NET libraries for building layered, domain-driven APIs with consistent entities, repositories, providers, transactions, controllers, exception handling, database integrations, and infrastructure services.

The framework supplies reusable mechanics. Each application still owns its domain model, authorization rules, database design, configuration, and operational policy.

## Documentation

Read the [documentation site](https://miracledevs.github.io/Paradigm.Enterprise/) for the guided experience, or start with the Markdown sources:

- [Framework overview](docs/overview.md)
- [Architecture](docs/architecture.md)
- [Create a solution](docs/tutorials/create-solution.md)
- [Build a vertical slice](docs/sample-application.md)
- [Package reference](docs/reference/packages.md)
- [API reference](https://miracledevs.github.io/Paradigm.Enterprise/reference/api.html)

To build and preview the site locally:

```powershell
dotnet tool restore
dotnet restore src/Paradigm.Enterprise.slnx
dotnet build src/Paradigm.Enterprise.slnx --configuration Release --property:GenerateDocumentationFile=true --property:NoWarn=1591%3B1572%3B1573%3B1574
dotnet docfx docs/docfx.json --serve
```

## Packages

| Area | Packages |
| --- | --- |
| Application stack | `Paradigm.Enterprise.Interfaces`, `Domain`, `Data`, `Providers`, `WebApi` |
| Databases | `Paradigm.Enterprise.Data.SqlServer`, `Data.PostgreSql` |
| Infrastructure | `Paradigm.Enterprise.Services.Cache`, `Email`, `BlobStorage`, `TableReader` |
| Tooling | `Paradigm.Enterprise.CodeGenerator` |

Install only the package required by the owning project:

```powershell
dotnet add package Paradigm.Enterprise.WebApi
```

Use a consistent package version across the application. The [package matrix](docs/reference/packages.md) lists responsibilities and target frameworks.

## Start a new API

The [Visual Studio template](https://github.com/MiracleDevs/Paradigm.Web.ApiTemplate) creates the expected project boundaries, database-first scaffolding, generated interface analyzer, host composition root, and code-generation tool. Follow [Create a solution](docs/tutorials/create-solution.md) before applying production security and configuration.

The repository's [example](example/README.md) targets a historical package line and is retained as a compatibility sample. It is not the canonical guide for current APIs.

## Build and test

```powershell
dotnet restore src/Paradigm.Enterprise.slnx
dotnet build src/Paradigm.Enterprise.slnx
dotnet test src/Paradigm.Enterprise.slnx
```

## Contributing

Read the [contribution guide](.github/CONTRIBUTING.md), [documentation guide](docs/contributing/documentation.md), and [RFC process](docs/rfc/README.md) before proposing a substantial change. Engineering rules used by automated review remain under [docs/rules](docs/rules/).

## Release history and license

See [CHANGELOG.md](CHANGELOG.md) for package history. The project is licensed under the [MIT License](LICENSE).
