# Contribute documentation

Documentation should explain the framework to a colleague who needs to make a correct change, not merely repeat type names. Begin with the reason a concept exists, describe how it behaves, and then show the smallest useful example.

The current source is authoritative for API signatures and runtime behavior. The Visual Studio template is authoritative for generated structure and scaffolding. Historical RFCs explain decisions at a point in time but can describe work that was never implemented. Example applications and training documents show usage patterns, not current contracts.

## Writing style

Prefer connected paragraphs. Use a list for an actual sequence, a set of choices, or a checklist. Use a table when readers need to compare the same fields across several items. Avoid numbered headings, decorative separators, hype, emojis, and conclusions that merely repeat the introduction.

Examples must be generic. Do not copy project names, namespaces, domain rules, credentials, URLs, or distinctive data from an application repository. Comments should explain why a line matters rather than narrate obvious syntax.

Use Mermaid only when a relationship or sequence is clearer visually. Keep labels short, avoid icons, and use the established blue, teal, violet, amber, and green palette.

## Build locally

Restore the pinned tool and build the site from the repository root:

```powershell
dotnet tool restore
dotnet restore src/Paradigm.Enterprise.slnx
dotnet build src/Paradigm.Enterprise.slnx --configuration Release --property:GenerateDocumentationFile=true --property:NoWarn=1591%3B1572%3B1573%3B1574
dotnet docfx docs/docfx.json --warningsAsErrors
```

Preview the generated site:

```powershell
dotnet docfx docs/docfx.json --serve
```

The local address is printed by Docfx. Generated API metadata and `_site` output are ignored by Git.

## Review

Check links in both Docfx and GitHub rendering. Verify code against the current source. Search the changed documentation for obsolete type names, copied project identifiers, em dashes, emojis, mojibake, and separator-only lines.

When behavior is uncertain, state the limitation or inspect the source. Do not turn an assumption into a promise.
