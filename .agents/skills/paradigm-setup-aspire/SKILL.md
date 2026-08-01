---
name: paradigm-setup-aspire
description: Set up or review Aspire orchestration for a Paradigm.Enterprise solution, including AppHost, ServiceDefaults, root .env configuration, SQL Server or PostgreSQL resources, database bootstrap ordering, health and telemetry defaults, and Azure Bicep publishing. Use for new Paradigm solutions, replacing Docker starter scripts, adding Aspire to an existing API, or repairing an AppHost resource graph.
---

# Set up Paradigm Aspire

Read and apply [Paradigm Good Coding Practices](../../references/good-coding-practices.md) before changing source, dependencies, configuration, or host policy.

## Establish the topology

1. Inspect the solution, hosts, databases, external services, deployment target, existing `.env`, and startup scripts. Preserve intentional ports and resource names.
2. For a new solution, use `$paradigm-setup-project` first. Put `<Solution>.AppHost` and `<Solution>.ServiceDefaults` under `src`, and put `aspire.config.json` at the repository root.
3. Ask the user to approve every new package after reporting its purpose, alternatives, license, official repository, maintenance/security posture, and important transitive dependencies.
4. Install or refresh Microsoft's Aspire workflow skills instead of copying them into the Paradigm plugin:

```powershell
dotnet tool run aspire agent init --non-interactive --skills aspire,aspire-init,aspire-orchestration,aspire-monitoring,aspire-deployment,aspireify --skill-locations standard
```

Use the installed Aspire skills for CLI lifecycle and diagnostics. Use this skill for the Paradigm-specific topology and policy.

## Configure local orchestration

Read [AppHost patterns](references/apphost-patterns.md) before writing the AppHost or `.env` loader.

- Load the repository-root `.env` before `DistributedApplication.CreateBuilder(args)`. Never overwrite an already-set process environment variable, log values, or publish a secret as a default.
- Commit `.env.example` with placeholders and ignore `.env`. Keep non-secret defaults in appsettings.
- Use `Database__Provider`, `Database__Mode`, `Database__Name`, `Database__Password`, `Database__PublishOnStart`, and `ConnectionStrings__DatabaseConnection`.
- Default new solutions to `SqlServer`, `Managed`, and schema publishing enabled. Require an explicit connection string for `External`; disable automatic publishing there unless the user deliberately enables it.
- Give managed SQL Server or PostgreSQL resources persistent volumes. Reference their database resource from consumers so Aspire supplies `ConnectionStrings__{name}`.
- Add ServiceDefaults to hosted services and map bounded liveness/readiness endpoints. Preserve standard trace context and keep health checks cheap and secret-free.

## Preserve the starter workflow

Create a root `start.sh` for new Aspire solutions. `$paradigm-setup-project` generates the canonical script; when adding Aspire to an existing solution, preserve its intentional commands and bring it to the same contract:

- support `./start.sh start`, `./start.sh stop`, `./start.sh doctor`, and `./start.sh help`, with `start` as the default; run `start` in the foreground and target the checkout's explicit AppHost path for both start and stop;
- require an installed .NET 10 SDK rather than merely a `dotnet` command, create a repository-local tool manifest when absent, and install missing `Paradigm.Enterprise.Cli` and `Aspire.Cli` tools locally rather than globally;
- pin Paradigm CLI to the solution's approved Paradigm version, let the first stable Aspire install record its resolved version in the manifest, and restore without silently upgrading either tool afterward;
- before `start` or `doctor`, check both the Docker command and `docker info`; prompt an interactive developer to install/start Docker and retry, but fail with an actionable message in noninteractive execution;
- invoke Aspire through `dotnet tool run aspire`; do not require the developer's global `PATH` or install tools from an AppHost/bootstrap resource.

Use the official [Aspire CLI installation guidance](https://aspire.dev/get-started/install-cli/) to verify the current package ID. Do not pipe a remote installer into the starter script.

## Order database startup

Use `$paradigm-build-database` to create or review the database project and its bootstrap contract.

- SQL Server: wait for the database; import the optional BACPAC only when the managed database is empty; build and verify the DACPAC; run pre-pre-deployment with a SQLCMD-compatible executor; only then generate the SqlPackage plan and publish; verify completion; then start the API. Restore a committed local SqlPackage tool before AppHost starts.
- PostgreSQL: wait for the database; run the pinned DbPublisher version with `Paradigm_ORM_ConnectionString` explicitly mapped from the Aspire resource; verify the schema before releasing dependents.
- Never import a baseline into an external database automatically. Never install tools from inside a running bootstrap resource.
- Model bootstrap as a finite project or executable resource and make the API use `WaitForCompletion`; do not hide schema work inside API startup.

## Configure publishing

Read [deployment publishing](references/deployment.md) when the user wants Bicep or an Azure target. Require a target choice; do not infer Container Apps versus App Service.

## Verify

Run:

```powershell
dotnet tool run aspire doctor
dotnet tool run aspire restore
dotnet build <solution>
dotnet tool run aspire run --isolated
dotnet tool run aspire describe --format Json
dotnet tool run aspire logs <database-bootstrap>
```

Verify that a first run provisions the database, a second run is idempotent, the API waits for schema completion, generated connection strings reach the correct consumers, and logs/manifests contain no secrets. If a deployment target exists, also run `aspire publish --list-steps` and publish to a disposable output directory.

If the Aspire CLI, container runtime, target credentials, or required daemon is unavailable, complete all static/build checks and record the exact missing prerequisite plus the unrun smoke scenarios. Do not claim first-run, restart-idempotency, external-database, or Bicep runtime verification passed.
