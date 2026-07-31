# Paradigm.Enterprise CLI

Read-only API discovery and application diagnostics for Paradigm.Enterprise.

Install in a repository-local tool manifest:

```powershell
dotnet new tool-manifest
dotnet tool install Paradigm.Enterprise.Cli
dotnet tool run paradigm doctor --project <solution>
```

The single tool includes structured API guidance, built-in metadata and semantic C# validation, offline package policy, explicit network audits, and opt-in source generation commands.

See the [complete command reference](https://miracledevs.github.io/Paradigm.Enterprise/cli.html).
