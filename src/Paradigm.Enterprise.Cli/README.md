# Paradigm.Enterprise CLI

Consumer-facing solution scaffolding, database validation, API discovery, application diagnostics, dependency policy, and explicit source generation for Paradigm.Enterprise.

Install in a repository-local tool manifest:

```powershell
dotnet new tool-manifest
dotnet tool install Paradigm.Enterprise.Cli
dotnet tool run paradigm doctor --project <solution>
```

The single tool includes structured API guidance, built-in metadata and semantic C# validation, offline package policy, explicit network audits, and opt-in source generation commands.

After cloning a repository that already contains the manifest, restore the recorded tool before invoking it:

```powershell
dotnet tool restore
dotnet tool run paradigm doctor --project <solution>
```

Common workflows:

```powershell
dotnet tool run paradigm inspect --project <solution>
dotnet tool run paradigm validate --project <solution>
dotnet tool run paradigm checks run --project <solution>
dotnet tool run paradigm packages check --project <solution>
dotnet tool run paradigm packages audit --project <solution>
dotnet tool run paradigm checks list
dotnet tool run paradigm scaffold solution --template-root <template> --name <Company.Product> --output <empty-directory> --paradigm-version <version> --dry-run
dotnet tool run paradigm database validate --project <database.sqlproj> --solution <solution> --strict
```

Run `dotnet tool update Paradigm.Enterprise.Cli` from the manifest directory when intentionally updating the repository's tool version. Solution scaffolding creates a baseline GitHub quality workflow and problem matcher in addition to application source and `start.sh`. Generation is opt-in and writes source; build the input project first and review generated changes.

See the [complete command reference](https://miracledevs.github.io/Paradigm.Enterprise/cli.html).
