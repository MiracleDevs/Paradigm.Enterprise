# Task 2 validation hash contract

`validation-hashes.sha256` is a secret-free snapshot of the bounded persistence-regeneration contract, relative to `examples/beacon-ar`.

The 78 entries comprise:

- tool, SDK, dependency, solution, and relevant project pins/wiring: `.config/dotnet-tools.json`, `global.json`, both `Directory.*` files, `BeaconAr.slnx`, and the Data, Domain, and Interfaces project/generator sources;
- generation controls: `build/regenerate-persistence.ps1`, `efcpt-config.json`, and both official-derived T4 templates;
- live-schema source inputs: every one of the 17 tables and 14 views explicitly selected by `efcpt-config.json`, plus the SQL project;
- generated output snapshot: all 31 generated Domain files and the generated context;
- focused validation evidence: `FoundationSmoke.sql`.

Generate entries only from an explicit path set covering those categories, normalize paths to forward slashes relative to the example root, apply `Sort-Object -Unique`, and calculate lowercase SHA-256 values with `Get-FileHash`. Do not discover outputs with an unrestricted repository-wide glob: the explicit EFPT selection and owned output sets define the boundary.

Verification must fail unless all of the following are true:

1. There are exactly 78 non-comment entries and 78 unique paths.
2. Paths are already in the same deterministic `Sort-Object` order used during generation.
3. Every path exists beneath the example root.
4. Every current lowercase SHA-256 equals its recorded value.
5. The manifest includes exactly 17 `src/database/tables/*.sql` paths, 14 `src/database/views/*.sql` paths, 31 generated Domain `.cs` paths, one generated context, and the local tool manifest.

This manifest proves one exact input/output snapshot. It does not prove byte stability across a second EFPT run, identify the live SQL Server engine image, or replace a sanitized DACPAC deployment-plan review. Those remain separate acceptance evidence.
