# Paradigm CLI

`Paradigm.Enterprise.Cli` is the single .NET 10 tool for applications that consume Paradigm.Enterprise NuGet packages. It provides restored-package API discovery, metadata validation, built-in semantic C# checks, dependency policy/auditing, and explicit source generation through one `paradigm` entry point. Applications do not reference it at runtime.

The metadata diagnostic commands inspect the consuming project's restored assets and assemblies without launching the application. `checks run` asks MSBuild/Roslyn for the real project compilation, which can execute trusted build targets, analyzers, and source generators from restored dependencies; run it only on projects and packages you trust. `generate` is an explicit mutating boundary: JSON and mapper generation load the trusted application assembly supplied by the caller and write reviewed generated source.

The NuGet package and `--version` output carry ordinary release metadata, but the unreleased CLI has no lockstep requirement, compatibility branches, or alternate implementation selected by framework version. It inspects the packages currently restored by the client project.

## Install

Use a local manifest so the client repository records and restores its tool:

```powershell
dotnet new tool-manifest
dotnet tool install Paradigm.Enterprise.Cli
dotnet tool run paradigm --version
dotnet tool run paradigm doctor --project <solution>
```

After cloning a repository that already contains `.config/dotnet-tools.json`, run only `dotnet tool restore`. Use `dotnet tool update Paradigm.Enterprise.Cli` from the manifest directory when intentionally updating the recorded tool version. Use `dotnet tool install --global Paradigm.Enterprise.Cli` only for evaluation. During CLI development, pack to a local feed and pass `--add-source <artifacts>` to `dotnet tool install`; consumers install the published package from their configured NuGet source.

The command signatures below use `paradigm` for readability. With the recommended local manifest, invoke them as `dotnet tool run paradigm ...`; bare `paradigm` is available after a global or tool-path installation.

## Commands

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

`--project` accepts a directory, project, `.sln`, or `.slnx`. A directory with several solutions requires an explicit solution. For multi-target restored assets, the highest `net*` target is selected unless `--framework` is supplied. Search returns at most 20 matches by default and never more than 100.

`doctor`, `inspect`, and `validate` evaluate each project's MSBuild `TargetPath` for Debug and Release instead of assuming `bin/<configuration>/<tfm>`. Missing or source-stale outputs produce `PE1002`; rebuild before relying on the result. API commands inspect only resolved Paradigm package assemblies and required targeting assemblies. Curated guides are emitted only when the requested symbol and structural feature probes match.

JSON schema `1.1` preserves `schemaVersion`, `command`, `status`, `packages`, `results`, and `diagnostics`, and may add structured `guide` or `checks` data. `help`, `--help`, and `--version` remain plain text.

## Built-in C# checks

`checks run` requires a restorable/buildable consuming project and evaluates its real Compile items, references, language settings, and source generators. It currently reports:

| Code | Meaning |
| --- | --- |
| `PE3103` | EF pagination is composed outside the required stored-procedure boundary |
| `PE3104` | Entity state is assigned outside entity-owned behavior |
| `PE3105` | A handwritten file declares more than one top-level semantic type |
| `PE3106` | A class member is out of canonical order or outside its exact member region |

Generated `.g.cs`, `.generated.cs`, `.designer.cs`, and `<auto-generated>` files are excluded from `PE3105`/`PE3106`. Run these checks against consuming applications and handwritten tooling. Deliberately invalid negative-test fixtures should be excluded from the owning test project's Compile items and analyzed only by their focused fixture tests.

Optional `.paradigm/config.json` suppressions require a diagnostic code, symbol or location, reason, and expiry. Suppression expiry is an error. The first-party C# checks are compiled into the CLI; there is no second tool, executable path, check-pack version, download, or runtime discovery step.

## Generation

Generation modes run independently so a client is never forced to generate unrelated outputs:

- `generate json` loads the built Providers assembly from `--assembly` and writes serializer contexts beneath the source directory supplied by `--output`.
- `generate mappers` loads the built Data assembly and writes stored-procedure mappers beneath the separate source directory supplied by `--output`. The current mapper templates target SQL Server; PostgreSQL generation remains a known design gap.
- `generate client` reads the trusted OpenAPI URL supplied by `--document`, uses the bundled TypeScript settings, and writes the configured client file beneath `--output`.

Pass `--settings` to use a reviewed replacement for the bundled generator JSON. Build application assemblies before JSON or mapper generation. Generate clients only from a trusted OpenAPI endpoint, then review every generated diff. Generation failures return `PE8001` and a nonzero exit; they are no longer swallowed as successful runs.

## Other diagnostics and exits

| Code | Meaning |
| --- | --- |
| `PE0001`–`PE0003` | API query or project-selection problem |
| `PE1001` | Mixed Paradigm package versions |
| `PE1002` | Required restore, build, or metadata artifact is missing |
| `PE2001` | Invalid canonical layer reference |
| `PE3001`–`PE3002` | Discovery-interface or identifier inconsistency |
| `PE3101`–`PE3106` | Entity, repository, query, mutation, or source-layout rule |
| `PE4001` | Anonymous Paradigm controller base lacks independent authorization |
| `PE5001` | Invalid local configuration |
| `PE6001` | No compatible curated guide exists |
| `PE7004`–`PE7008` | Suppression expiry, incomplete audit, vulnerability, deprecation, or update policy |
| `PE8001` | Explicit source generation failed |

Exit `0` means success or warnings, `1` means query/validation/generation errors, `2` means invalid arguments or project selection, and `3` means project/assets/output/metadata resolution failed.

`packages check` is offline and enforces aligned Paradigm packages plus suppression expiry. `packages audit` explicitly uses the .NET package-list JSON interface with `--no-restore` for vulnerability, deprecation, and outdated data. Known vulnerabilities and incomplete audits are errors; stable direct updates and deprecations are warnings unless `--warnings-as-errors` is set.
