# Paradigm.Enterprise

Paradigm.Enterprise is a set of .NET libraries for building layered, domain-driven APIs with consistent entities, repositories, providers, transactions, controllers, exception handling, database integrations, and infrastructure services.

The framework supplies reusable mechanics. Each application still owns its domain model, authorization rules, database design, configuration, and operational policy.

## Documentation

Read the [documentation site](https://miracledevs.github.io/Paradigm.Enterprise/) for the guided experience, or start with the Markdown sources:

- [Framework overview](docs/overview.md)
- [Architecture](docs/architecture.md)
- [Domain model](docs/domain.md)
- [Design principles](docs/design-principles.md)
- [Create a solution](docs/tutorials/create-solution.md)
- [Build a vertical slice](docs/sample-application.md)
- [Secure host configuration](docs/guides/security-and-host.md)
- [Secure delivery](docs/guides/secure-delivery.md)
- [Package reference](docs/reference/packages.md)
- [API reference](https://miracledevs.github.io/Paradigm.Enterprise/reference/api.html)
- [Agent Skills](docs/agent-skills.md)
- [Paradigm CLI](docs/cli.md)

To build and preview the site locally:

```bash
bash build/serve.documentation.sh
```

The site is served at `http://localhost:8080`. Pass a different port as the first argument or set `DOCS_HOST` to bind to another hostname.

## Packages

| Area | Packages |
| --- | --- |
| Application stack | `Paradigm.Enterprise.Interfaces`, `Domain`, `Data`, `Providers`, `WebApi` |
| Databases | `Paradigm.Enterprise.Data.SqlServer`, `Data.PostgreSql` |
| Infrastructure | `Paradigm.Enterprise.Services.Cache`, `Email`, `BlobStorage`, `TableReader` |
| Tooling | `Paradigm.Enterprise.CodeGenerator`, `Paradigm.Enterprise.Cli`, `Paradigm.Enterprise.Checks.CSharp` |

Install only the package required by the owning project:

```powershell
dotnet add package Paradigm.Enterprise.WebApi
```

Use a consistent package version across the application. The [package matrix](docs/reference/packages.md) lists responsibilities and target frameworks.

Install the read-only diagnostic CLI through a repository-local tool manifest:

```powershell
dotnet new tool-manifest
dotnet tool install Paradigm.Enterprise.Cli --version 1.1.0
dotnet tool install Paradigm.Enterprise.Checks.CSharp --version 1.1.0
dotnet tool run paradigm doctor --project src/Paradigm.Enterprise.slnx
```

The CLI provides version-aware `api search/show/guide`, built-in validation, offline dependency checks, explicit network audits, and opt-in pinned process check packs. `Paradigm.Enterprise.Checks.CSharp` is the first-party semantic C# check pack. See [CLI documentation](docs/cli.md).

AI coding agents can use the concise workflows under [`.agents/skills`](.agents/skills) together with the version-aware CLI. See [Agent Skills](docs/agent-skills.md) for Copilot and Codex installation.

## Start a new API

The [Visual Studio template](https://github.com/MiracleDevs/Paradigm.Web.ApiTemplate) creates the expected project boundaries, database-first scaffolding, generated interface analyzer, host composition root, and code-generation tool. Follow [Create a solution](docs/tutorials/create-solution.md) before applying production security and configuration.

The repository's [example](example/README.md) targets the current package line and demonstrates the generic identifier and behavior-owned entity patterns used by the current APIs.

## Build and test

```powershell
dotnet restore src/Paradigm.Enterprise.slnx
dotnet build src/Paradigm.Enterprise.slnx
dotnet test src/Paradigm.Enterprise.slnx
```

The test wrapper is also available as `bash build/test.sh`.

## Contributing

Read the [contribution guide](.github/CONTRIBUTING.md), [documentation guide](docs/contributing/documentation.md), and [RFC process](docs/rfc/README.md) before proposing a substantial change. Engineering rules used by automated review remain under [docs/rules](docs/rules/).

## Release history and license

See [CHANGELOG.md](CHANGELOG.md) for package history. The project is licensed under the [MIT License](LICENSE).
