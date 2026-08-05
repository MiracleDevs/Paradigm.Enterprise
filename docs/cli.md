# Paradigm CLI

`Paradigm.Enterprise.Cli` is the only command-line tool for applications that consume Paradigm.Enterprise NuGet packages. The single `paradigm` entry point provides restored-package API discovery, application metadata validation, built-in semantic C# checks, dependency policy and auditing, and explicit source generation. Applications do not reference the tool at runtime, and there is no separate generator or checks executable to install.

Metadata commands inspect the consuming project's restored assets and assemblies without launching the application. `checks run` asks MSBuild and Roslyn for the real project compilation, which can execute build targets, analyzers, and source generators from restored dependencies. Run it only on projects and packages you trust. Generation is an explicit mutating boundary: JSON and mapper generation load the trusted application assembly supplied by the caller, and every generation mode writes source that must be reviewed.

The CLI is still unreleased. It carries ordinary package metadata and reports its package version, but it has no compatibility branches, framework-version implementation selection, or parallel CLI versions.

## Install and restore

Use a repository-local tool manifest so each client repository records the tool it expects:

```powershell
dotnet new tool-manifest
dotnet tool install Paradigm.Enterprise.Cli
dotnet tool run paradigm --version
dotnet tool run paradigm doctor --project <solution>
```

After cloning a repository that already contains `.config/dotnet-tools.json`, run `dotnet tool restore`. Run `dotnet tool update Paradigm.Enterprise.Cli` from the manifest directory only when intentionally updating the recorded version. A global installation is useful for evaluation, but it does not make the client repository reproducible.

During CLI development, pack to a local directory and use that directory as an additional NuGet source:

```powershell
dotnet pack src/Paradigm.Enterprise.Cli/Paradigm.Enterprise.Cli.csproj --configuration Release --output artifacts
dotnet tool install --tool-path .paradigm/tools --add-source artifacts Paradigm.Enterprise.Cli
.paradigm/tools/paradigm --version
```

The command signatures below use bare `paradigm` for readability. With the recommended local manifest, invoke the same command as `dotnet tool run paradigm ...`.

## Complete command reference

```text
paradigm doctor [--project <path>] [--format text|json]
paradigm api search <query> [--project <path>] [--framework <tfm>] [--package <name>] [--limit <1-100>] [--format text|json]
paradigm api show <symbol> [--project <path>] [--framework <tfm>] [--package <name>] [--format text|json]
paradigm api guide <symbol> [--project <path>] [--framework <tfm>] [--package <name>] [--format text|json]
paradigm inspect [--project <path>] [--framework <tfm>] [--format text|json]
paradigm validate [--project <path>] [--framework <tfm>] [--format text|json]
paradigm checks list [--format text|json]
paradigm checks run [--project <path>] [--framework <tfm>] [--config <path>] [--format text|json]
paradigm packages check [--project <path>] [--framework <tfm>] [--config <path>] [--format text|json]
paradigm packages audit [--project <path>] [--framework <tfm>] [--config <path>] [--warnings-as-errors] [--format text|json]
paradigm generate json --project-name <name> --assembly <dll> --output <directory> [--settings <json>] [--format text|json]
paradigm generate mappers --project-name <name> --assembly <dll> --output <directory> [--settings <json>] [--format text|json]
paradigm generate client --document <url> --output <directory> [--settings <json>] [--format text|json]
paradigm --version
```

`help`, `--help`, or no arguments print the same reference. Unknown commands, missing required values, unsupported options, and invalid values print an error and return exit code `2`.

## Common options and path resolution

`--project` accepts a directory, a `.csproj`, a `.sln`, or a `.slnx`. When omitted, resolution starts in the current directory. A directory containing more than one solution requires an explicit solution path. Commands that operate on a solution process every listed C# project.

`--framework` selects a target framework from restored `project.assets.json`. When it is omitted for a multi-target project, the highest compatible `net*` target is selected. `doctor` intentionally checks the resolved project without accepting this option. `checks list` is project-independent and accepts neither `--project` nor `--framework`.

`--format text` is the default human-readable output. `--format json` returns the complete machine-readable response for automation. The common JSON envelope uses schema `1.1` and contains `schemaVersion`, `command`, `status`, `packages`, `results`, and `diagnostics`; it may also contain `guide` or `checks`. Help and version output remain plain text.

`--config` selects an explicit Paradigm configuration for `checks run`, `packages check`, or `packages audit`. Without it, the CLI searches upward for `.paradigm/config.json` and stops at the Git root. The file uses schema `1.0`. Every suppression requires a diagnostic `code`, either `symbol` or `location`, a `reason`, and an `expires` date. Expired suppressions are errors. The package policy's `includePrerelease` value controls whether the online update audit considers prerelease versions.

```json
{
  "schemaVersion": "1.0",
  "packagePolicy": {
    "includePrerelease": false
  },
  "suppressions": [
    {
      "code": "PE3106",
      "location": "LegacyAdapter.cs",
      "symbol": null,
      "reason": "Remove with the bounded-context migration.",
      "expires": "2026-10-01"
    }
  ]
}
```

## Environment diagnosis

`doctor` resolves the selected workspace, reports the running .NET runtime, confirms restore output, reports the chosen framework, and verifies the evaluated application assembly. It evaluates MSBuild output paths instead of assuming `bin/<configuration>/<tfm>`. A missing restore, missing build, or source-stale build produces `PE1002`. Restore and build the client before relying on later metadata commands.

```powershell
dotnet tool run paradigm doctor --project MyApplication.slnx
```

## Package API discovery

`api search` searches public types and visible members in resolved Paradigm package assemblies. The required query is a case-insensitive substring. `--package` restricts results to one resolved package name, and `--limit` defaults to `20` with an accepted range of `1` through `100`.

`api show` requires a type symbol and returns the exact type shape when the symbol is unambiguous. Use a fully qualified name when short names collide. `api guide` performs the same lookup and adds curated usage, discovery, caution, and verification guidance when the resolved symbol passes the guide's structural feature probes. It returns `PE6001` when the symbol exists but no compatible curated guide exists.

```powershell
dotnet tool run paradigm api search RepositoryBase --project MyApplication.slnx
dotnet tool run paradigm api show Paradigm.Enterprise.Data.Repositories.IRepository --project MyApplication.slnx
dotnet tool run paradigm api guide Paradigm.Enterprise.Providers.Providers.ProviderBase --project MyApplication.slnx --format json
```

These commands inspect only resolved Paradigm package assemblies and required targeting assemblies. They do not load or execute the consuming application.

## Inspect and validate an application

`inspect` loads the built application assembly together with restored Paradigm assemblies and reports discovered application entities, views, repositories, providers, controllers, and related metadata. `validate` performs the same inspection, then applies layer, identifier, discovery, entity-state, repository, and authorization conventions. Configuration suppressions apply to validation diagnostics.

```powershell
dotnet build MyApplication.slnx
dotnet tool run paradigm inspect --project MyApplication.slnx
dotnet tool run paradigm validate --project MyApplication.slnx --format json
```

Both commands require current build output. They inspect metadata in a `MetadataLoadContext` and do not start the application.

## Built-in C# checks

`checks list` reports the check implementations compiled into the CLI and does not inspect a project. `checks run` evaluates the selected project's real Compile items, references, language settings, and source generators through MSBuild and Roslyn, then runs the built-in C# checks. There is no check-pack download, runtime discovery step, or second tool.

```powershell
dotnet tool run paradigm checks list
dotnet tool run paradigm checks run --project MyApplication.slnx --config .paradigm/config.json
```

The built-in check diagnostics are:

| Code | Meaning |
| --- | --- |
| `PE3103` | EF pagination is composed outside the required stored-procedure boundary |
| `PE3104` | Entity state is assigned outside entity-owned behavior |
| `PE3105` | A handwritten file declares more than one top-level semantic type |
| `PE3106` | A class member is out of canonical order, outside its exact member region, or uses invalid member-region spacing |

`PE3106` requires exactly one empty line after `#region`, before `#endregion`, and between adjacent member regions. Generated `.g.cs`, `.generated.cs`, `.designer.cs`, and `<auto-generated>` files are excluded from `PE3105` and `PE3106`. Deliberately invalid fixtures should be isolated and tested through focused fixture projects.

## Dependency policy and audit

`packages check` is offline. It reads restored assets, reports direct and transitive packages, enforces aligned Paradigm package versions, and validates suppression expiry. Human-readable output limits repeated package rows; use JSON for the complete result.

`packages audit` first applies the same offline policy, then invokes the .NET package-list JSON interface with `--no-restore` to retrieve vulnerability, deprecation, and outdated-package data. This command uses network-backed NuGet sources. Known vulnerabilities and incomplete audits are errors. Stable direct updates and deprecations are warnings unless `--warnings-as-errors` is present.

```powershell
dotnet tool run paradigm packages check --project MyApplication.slnx
dotnet tool run paradigm packages audit --project MyApplication.slnx --warnings-as-errors
```

## Source generation

Generation is part of `Paradigm.Enterprise.Cli`. Each mode is independent, so a client generates only the requested output. `--output` is always required and is resolved to a full directory path. `--settings` selects a reviewed replacement for the settings JSON bundled in the tool package.

`generate json` requires `--project-name` and `--assembly`. It loads the built Providers assembly and writes JSON serializer contexts under `--output`. `generate mappers` requires the same options, loads the built Data assembly, and writes stored-procedure mappers under its own output directory. The current mapper templates target SQL Server; PostgreSQL generation remains a known design gap.

`generate client` requires `--document` and reads that trusted OpenAPI URL before writing the configured TypeScript client beneath `--output`. It does not require `--project-name` or `--assembly`.

```powershell
dotnet tool run paradigm generate json --project-name MyApplication --assembly src/MyApplication.Providers/bin/Debug/net10.0/MyApplication.Providers.dll --output src/MyApplication.Providers/Serialization
dotnet tool run paradigm generate mappers --project-name MyApplication --assembly src/MyApplication.Data/bin/Debug/net10.0/MyApplication.Data.dll --output src/MyApplication.Data/Mappers
dotnet tool run paradigm generate client --document https://localhost:7001/openapi/v1.json --output src/MyApplication.Web/Client
```

Build assemblies before JSON or mapper generation. Generate clients only from a trusted OpenAPI endpoint. Review all generated diffs. A generation failure produces `PE8001` and exit code `1`.

## Diagnostics and exit codes

| Code | Meaning |
| --- | --- |
| `PE0001` to `PE0003` | API query or project-selection problem |
| `PE1001` | Mixed Paradigm package versions |
| `PE1002` | Required restore, build, assets, output, or metadata artifact is missing |
| `PE2001` | Invalid canonical layer reference |
| `PE3001` to `PE3002` | Discovery-interface or identifier inconsistency |
| `PE3101` to `PE3106` | Entity, repository, query, mutation, or source-layout rule |
| `PE4001` | Anonymous Paradigm controller base lacks independent authorization |
| `PE5001` | Invalid local configuration |
| `PE5002` | Command canceled |
| `PE6001` | No compatible curated guide exists |
| `PE7004` to `PE7008` | Suppression expiry, incomplete audit, vulnerability, deprecation, or update policy |
| `PE8001` | Explicit source generation failed |

Exit code `0` means success or warnings. Exit code `1` means a completed command reported an error diagnostic or was canceled. Exit code `2` means invalid arguments or project selection. Exit code `3` means project assets, build output, or metadata resolution failed.

Maintainers should also read [CLI development](contributing/cli-development.md) for source ownership, execution flow, extension points, packaging, and verification.
