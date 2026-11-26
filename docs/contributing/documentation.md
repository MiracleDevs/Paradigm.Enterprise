# Contribute documentation

Documentation should explain the framework to a colleague who needs to make a correct change, not merely repeat type names. Begin with the reason a concept exists, describe how it behaves, and then show the smallest useful example.

The current source is authoritative for API signatures and runtime behavior. The Web API template is authoritative for generated structure and scaffolding. Historical RFCs explain decisions at a point in time but can describe work that was never implemented. Example applications and training documents show usage patterns, not current contracts.

## Writing style

Prefer connected paragraphs. Use a list for an actual sequence, a set of choices, or a checklist. Use a table when readers need to compare the same fields across several items. Avoid numbered headings, decorative separators, hype, emojis, and conclusions that merely repeat the introduction.

Examples must be generic. Do not copy project names, namespaces, domain rules, credentials, URLs, or distinctive data from an application repository. Comments should explain why a line matters rather than narrate obvious syntax.

Use Mermaid when a relationship, boundary, lifecycle, or process becomes clearer visually. There is no target count or diagram quota. Choose the form that matches the question: flowcharts for responsibilities and processes, architecture views for dependencies and boundaries, state diagrams for lifecycles, and event-oriented models for causes and outcomes. Use a sequence diagram when the ordering of messages among participants is itself important, not as the default representation for every process.

Keep labels short, avoid icons, and use the established blue, teal, violet, amber, and green palette. Style node fills and borders, but leave connectors and edge labels theme-driven so diagrams remain readable in light and dark modes.

## Build locally

The repository script restores the pinned tool, builds the libraries with XML documentation, validates the Docfx build, and starts the local server:

```bash
bash build/serve.documentation.sh
```

It serves `http://localhost:8080` by default. Pass a port as the first argument when that address is unavailable:

```bash
bash build/serve.documentation.sh 8090
```

Set `DOCS_HOST` when the server must bind to a hostname other than `localhost`. Generated API metadata and `_site` output are ignored by Git.

MySQL and Oracle API metadata use `metadata/MySqlApi.csproj` and `metadata/OracleApi.csproj`, documentation-only projects that compile the adapters' actual source with the SDK's nullable and implicit-using settings. `metadata/Directory.Build.targets` asks each runtime project to resolve its compiler references through MSBuild, using restored package assets and built project references rather than copied dependency DLLs in `bin`. Restore and build the source solution before running Docfx; the metadata projects continue working after quality steps remove optional dependency copies. Source extraction avoids MySQL's unused Linux `System.Windows.Extensions` reference to `System.Drawing.Common`, Oracle's optional ODP.NET authentication plugins, and Docfx's incompatible transitive project-reference metadata path. `metadata/Directory.Build.props` gives each metadata project separate intermediate and output directories so their restore and compiler caches cannot collide. These projects have no package or project references and are neither packaged nor published; do not add handwritten copies of the adapter APIs or relax documentation warnings to maintain them. Other binary metadata inputs still require `--property:CopyLocalLockFileAssemblies=true` when building and packing the source solution.

## Review

Check links in both Docfx and GitHub rendering. Verify code against the current source. Search the changed documentation for obsolete type names, copied project identifiers, em dashes, emojis, mojibake, and separator-only lines.

When behavior is uncertain, state the limitation or inspect the source. Do not turn an assumption into a promise.
