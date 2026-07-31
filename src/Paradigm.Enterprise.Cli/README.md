# Paradigm.Enterprise CLI

Read-only API discovery and application diagnostics for Paradigm.Enterprise.

Install in a repository-local tool manifest:

```powershell
dotnet new tool-manifest
dotnet tool install Paradigm.Enterprise.Cli --version 1.1.0
dotnet tool run paradigm doctor --project <solution>
```

The tool includes structured API guidance, built-in metadata validation, offline package policy, explicit network audits, and an opt-in boundary for pinned process check packs.

See the [complete command reference](https://miracledevs.github.io/Paradigm.Enterprise/cli.html).
