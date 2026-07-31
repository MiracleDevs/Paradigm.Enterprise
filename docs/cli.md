# Paradigm CLI

`Paradigm.Enterprise.Cli` is a read-only .NET 10 tool for agents and developers. It resolves the actual `Paradigm.Enterprise.*` packages in `obj/project.assets.json`, reads assemblies with `MetadataLoadContext`, and never executes inspected application or package code. It does not build, restore, scaffold, fix, or generate source. `checks run` is a separate explicit trust boundary that executes only pinned, registered, repository-local check packs.

The tool version is released in lockstep with the framework. It remains separate from `Paradigm.Enterprise.CodeGenerator`.

## Install

Use a local manifest for reproducible development:

```powershell
dotnet new tool-manifest
dotnet tool install Paradigm.Enterprise.Cli --version 1.1.0
dotnet tool run paradigm --version
```

For evaluation:

```powershell
dotnet tool install --global Paradigm.Enterprise.Cli --version 1.1.0
```

## Commands

```text
paradigm doctor [--project <path>] [--format text|json]
paradigm api search <query> [--project <path>] [--framework <tfm>] [--package <name>] [--limit <1-100>] [--format text|json]
paradigm api show <symbol> [--project <path>] [--framework <tfm>] [--package <name>] [--format text|json]
paradigm api guide <symbol> [--project <path>] [--framework <tfm>] [--package <name>] [--format text|json]
paradigm inspect [--project <path>] [--framework <tfm>] [--format text|json]
paradigm validate [--project <path>] [--framework <tfm>] [--format text|json]
paradigm checks list [--project <path>] [--config <path>] [--format text|json]
paradigm checks run [--project <path>] [--framework <tfm>] [--config <path>] [--pack <id>] [--format text|json]
paradigm packages check [--project <path>] [--framework <tfm>] [--config <path>] [--format text|json]
paradigm packages audit [--project <path>] [--framework <tfm>] [--config <path>] [--warnings-as-errors] [--format text|json]
paradigm --version
```

`--project` accepts a directory, project, `.sln`, or `.slnx`. A directory with several solutions requires an explicit solution. For multi-target restored assets, the highest `net*` target is selected unless `--framework` is supplied. In a heterogeneous solution, each project selects an exact requested target when present, otherwise its highest restored `net*` target no newer than the request; a project with no compatible target fails explicitly. Search returns at most 20 matches by default and never more than 100.

`doctor`, `inspect`, and `validate` evaluate each project's MSBuild `TargetPath` for Debug and Release instead of assuming `bin/<configuration>/<tfm>`. Missing or source-stale outputs produce `PE1002`; rebuild before relying on the analysis. Metadata/type load failures are isolated by project and also prevent a false-clean result.

API commands inspect only resolved Paradigm package assemblies (and the targeting/framework assemblies needed to understand them), not every application assembly. `api guide` combines reflected, version-specific structure with a curated descriptor only when its symbol and feature probes match. It never invents guidance for an unknown shape.

JSON schema `1.1` preserves `schemaVersion`, `command`, `status`, `packages`, `results`, and `diagnostics`, and may add structured `guide` or `checks` data.
Text `api` commands omit the redundant package preamble because each match already identifies its owner. `help`, `--help`, and `--version` are plain text. Argument parsing errors are written to stderr; after `--format json` has parsed successfully, project-selection errors use the stable JSON envelope.

## Diagnostics and exits

| Code | Meaning |
| --- | --- |
| `PE0001` | Requested API symbol was not found |
| `PE0002` | Parsed command has an invalid project selection |
| `PE0003` | Requested symbol is ambiguous |
| `PE1001` | Mixed Paradigm package versions |
| `PE1002` | Required restore, build, or metadata artifact is missing |
| `PE2001` | Invalid canonical layer reference |
| `PE3001` | Missing or ambiguous exact-name registration interface |
| `PE3002` | Inconsistent generic identifier types in a capability |
| `PE3101` | Public setter on handwritten non-view entity state |
| `PE3102` | Repository contract/public member exposes `IQueryable` |
| `PE3103` | Semantic check found EF pagination outside a stored-procedure boundary |
| `PE3104` | Semantic check found entity state assigned outside entity behavior |
| `PE4001` | Paradigm anonymous controller base lacks recognized independent authorization |
| `PE5001`–`PE5005` | Check-pack configuration/start, timeout, exit, protocol/size, or diagnostic-ownership failure |
| `PE6001` | No compatible curated guide exists |
| `PE7002`–`PE7008` | CLI alignment, minimum version, suppression expiry, incomplete audit, vulnerability, deprecation, or update policy |

Exit `0` means success or warnings, `1` means query/validation errors, `2` means invalid arguments or project selection, and `3` means project/assets/output/metadata resolution failed. Any `PE1002` error exits `3`, including when other projects were analyzed successfully.

## Check-pack configuration

Packs are never discovered or downloaded implicitly. Register them in `.paradigm/config.json` with a pinned version, repository-relative executable, owned diagnostic prefix, timeout, and maximum output size. The CLI invokes the executable directly without a shell and exchanges schema `1.0` JSON over stdin/stdout. It isolates nonzero exits, malformed or oversized output, protocol mismatches, duplicate diagnostics, prefix violations, timeouts, and cancellation.

`Paradigm.Enterprise.Checks.CSharp` is the separately installed reference pack. Install its pinned version into `.paradigm/tools`; do not commit that directory. Suppressions require a diagnostic code, symbol or location, reason, and expiry. `validate` remains built-in-only.

Configured C# packs target consuming application projects. Framework source and test projects may intentionally contain internal hooks or negative fixtures that trigger application-facing checks, so do not use them as a false-clean consumer baseline. CLI warning severity exits successfully by default, but repository review policy may still make a warning PR-blocking.

Fixers are separate reviewed tools/scripts. A repeatable fixer must provide `--dry-run` and is never invoked by validation, check packs, or PR review.

## Dependency policy

`packages check` is offline. It reads restored assets and enforces aligned Paradigm packages, CLI/framework lockstep, configured minimums, and suppression expiry. `packages audit` explicitly uses the .NET 10 JSON package-list interface with `--no-restore` for vulnerability, deprecation, and outdated data.

Known vulnerabilities, mixed versions, below-minimum versions, CLI mismatch, expired suppressions, and incomplete audit results are errors. Newer stable direct packages and deprecated packages are warnings; prerelease packages are ignored unless configured. `--warnings-as-errors` promotes audit warnings.
