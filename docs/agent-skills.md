# Agent Skills

Paradigm.Enterprise ships concise [Agent Skills](https://agentskills.io/) for architecture, project setup, vertical slices, domain modeling, repositories, providers, Web API work, review, and governed promotion of learned practices. The skills load the shared [Paradigm Common Guidance](https://github.com/MiracleDevs/Paradigm.Enterprise/blob/main/.agents/skills/paradigm-common-guidance/SKILL.md), which centralizes coding, database, and solution-layout practices. The skills provide judgment and workflow; the CLI supplies exact API metadata and deterministic built-in rules for the packages restored by an application.

The canonical skills are under `.agents/skills`, which is also the path declared by the Codex plugin metadata. They work with agents that support the open format, including GitHub Copilot and Codex. Keep each skill's `agents/openai.yaml` metadata beside its `SKILL.md`; do not duplicate guidance in another directory.

Database work has one discoverable entrypoint at `.agents/skills/database-skills/SKILL.md`. It identifies the target engine and maps the task to one engine profile under `engines` plus the required task specialization. The engine profiles use `database-engine.md` as references and are loaded through that map, not discovered as independent skills.

## GitHub Copilot

Install the repository skills with the GitHub CLI:

```powershell
gh skill install https://github.com/MiracleDevs/Paradigm.Enterprise
```

Commit `.github/agents` only when the role-based custom agents are useful to the application. The Paradigm skills do not depend on those agents.

## Codex

The repository is a skills-only Codex plugin described by `.codex-plugin/plugin.json`. Install it from the repository using the current [Codex plugin workflow](https://developers.openai.com/plugins/build/plugins), or copy/link the repository into a configured local marketplace. Restart or open a new Codex thread after installation so skill metadata is rediscovered.

## Efficient use

Invoke the narrowest skill for the task. With the recommended local manifest, use `dotnet tool run paradigm api show` or a bounded `api search` only when an exact signature is unknown. Use `dotnet tool run paradigm inspect`, `validate`, and `checks run` against the consuming solution; they resolve its restored packages rather than relying on the tool's build-time framework references. Bare `paradigm` is only guaranteed after a global install.

Source code is authoritative for API behavior. Architecture and security guides are authoritative for design. Tests and examples provide supporting evidence. Production applications are non-normative examples and must not be copied into reusable guidance.

Use `paradigm-evolve-guidance` when a repeatable review lesson should become a fixture, built-in metadata or semantic rule, reviewed dry-run fixer, or concise judgment-only instruction. Prefer deterministic enforcement whenever stable evidence exists.

Use `paradigm-setup-aspire` for AppHost, ServiceDefaults, root `.env`, database bootstrap ordering, and Bicep publishing. Its governed SQL Server bootstrap assets build the DACPAC and install SQLCMD 18 plus pinned SqlPackage inside a repository-owned image, keeping those tools off the developer host. Install Microsoft's current [Aspire workflow skills](https://aspire.dev/get-started/aspire-skills/) in the consuming repository with `aspire agent init`; the Paradigm skill adds project-specific policy instead of vendoring that bundle.

Use `paradigm-build-database` for SDK-style SQL Server projects, DACPAC/BACPAC handling, PostgreSQL DbPublisher projects, schema scripts, keys, audit fields, and seed data. The built-in `paradigm database validate` command is read-only and supports strict new-project enforcement plus warning-oriented legacy audits.
