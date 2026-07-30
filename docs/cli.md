# Paradigm CLI

`Paradigm.Enterprise.Cli` is a read-only .NET 10 tool for agents and developers. It resolves the actual `Paradigm.Enterprise.*` packages in `obj/project.assets.json`, reads assemblies with `MetadataLoadContext`, and never executes inspected application or package code. It does not build, restore, scaffold, fix, or generate source.

The tool version is released in lockstep with the framework. It remains separate from `Paradigm.Enterprise.CodeGenerator`.

## Install

Use a local manifest for reproducible development:

```powershell
dotnet new tool-manifest
dotnet tool install Paradigm.Enterprise.Cli --version 1.0.33
dotnet tool run paradigm --version
```

For evaluation:

```powershell
dotnet tool install --global Paradigm.Enterprise.Cli --version 1.0.33
```

## Commands

```text
paradigm doctor [--project <path>] [--format text|json]
paradigm api search <query> [--project <path>] [--framework <tfm>] [--package <name>] [--limit <1-100>] [--format text|json]
paradigm api show <symbol> [--project <path>] [--framework <tfm>] [--format text|json]
paradigm inspect [--project <path>] [--framework <tfm>] [--format text|json]
paradigm validate [--project <path>] [--framework <tfm>] [--format text|json]
paradigm --version
```

`--project` accepts a directory, project, `.sln`, or `.slnx`. A directory with several solutions requires an explicit solution. For multi-target restored assets, the highest `net*` target is selected unless `--framework` is supplied. In a heterogeneous solution, each project selects an exact requested target when present, otherwise its highest restored `net*` target no newer than the request; a project with no compatible target fails explicitly. Search returns at most 20 matches by default and never more than 100.

`doctor`, `inspect`, and `validate` evaluate each project's MSBuild `TargetPath` for Debug and Release instead of assuming `bin/<configuration>/<tfm>`. Missing or source-stale outputs produce `PE1002`; rebuild before relying on the analysis. Metadata/type load failures are isolated by project and also prevent a false-clean result.

JSON output has stable top-level fields: `schemaVersion`, `command`, `status`, `packages`, `results`, and `diagnostics`.
Text `api` commands omit the redundant package preamble because each match already identifies its owner. `help`, `--help`, and `--version` are plain text. Argument parsing errors are written to stderr; after `--format json` has parsed successfully, project-selection errors use the stable JSON envelope.

## Diagnostics and exits

| Code | Meaning |
| --- | --- |
| `PE0001` | Requested API symbol was not found |
| `PE0002` | Parsed command has an invalid project selection |
| `PE1001` | Mixed Paradigm package versions |
| `PE1002` | Required restore, build, or metadata artifact is missing |
| `PE2001` | Invalid canonical layer reference |
| `PE3001` | Missing or ambiguous exact-name registration interface |
| `PE3002` | Inconsistent generic identifier types in a capability |
| `PE4001` | Paradigm anonymous controller base lacks recognized independent authorization |

Exit `0` means success or warnings, `1` means query/validation errors, `2` means invalid arguments or project selection, and `3` means project/assets/output/metadata resolution failed. Any `PE1002` error exits `3`, including when other projects were analyzed successfully.
