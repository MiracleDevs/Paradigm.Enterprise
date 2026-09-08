# Scaffold from a database

The Visual Studio template is designed for database-first development with EF Core Power Tools. Reverse engineering creates the context and persistence-shaped domain types. Partial classes then add behavior without modifying generated files.

```mermaid
flowchart LR
  SCHEMA[(Database schema)] --> EFPT[EF Core Power Tools]
  T4[T4 templates] --> EFPT
  EFPT --> CONTEXT[Generated DbContext]
  EFPT --> ENTITY[Generated entity and view types]
  ENTITY --> ANALYZER[Interface analyzer]
  ANALYZER --> CONTRACT[Generated interfaces]
  ENTITY --> PARTIAL[Hand-written partial behavior]
  CONTEXT --> REPOSITORY[Repositories]
  CONTRACT --> PROVIDER[Providers]
  PARTIAL --> PROVIDER

  style SCHEMA fill:#ecfdf5,stroke:#059669,stroke-width:1.5px,color:#0f172a
  style EFPT fill:#dbeafe,stroke:#2563eb,stroke-width:1.5px,color:#0f172a
  style T4 fill:#e0e7ff,stroke:#4f46e5,stroke-width:1.5px,color:#0f172a
  style CONTEXT fill:#ccfbf1,stroke:#0f766e,stroke-width:1.5px,color:#0f172a
  style ENTITY fill:#ede9fe,stroke:#7c3aed,stroke-width:1.5px,color:#0f172a
  style ANALYZER fill:#fef3c7,stroke:#d97706,stroke-width:1.5px,color:#0f172a
  style CONTRACT fill:#fef9c3,stroke:#ca8a04,stroke-width:1.5px,color:#0f172a
  style PARTIAL fill:#fae8ff,stroke:#a21caf,stroke-width:1.5px,color:#0f172a
  style REPOSITORY fill:#d1fae5,stroke:#047857,stroke-width:1.5px,color:#0f172a
  style PROVIDER fill:#bfdbfe,stroke:#1d4ed8,stroke-width:1.5px,color:#0f172a
```

## Prepare deterministic tooling and configuration

Prefer the official EF Core Power Tools CLI from a repository-local tool manifest and pin its exact approved version. Inspect that installed CLI's help and use its current `efcpt-config.json` schema; do not rename or translate keys from the Visual Studio extension's older `efpt.config.json` format. Review the context name, namespaces, entity/context output paths, explicit table/view/routine selection, nullable mappings, database naming, and T4 template root. Set object-list refresh deliberately so regeneration cannot silently broaden or shrink the owned model. Keep credentials and connection names out of the file.

For multiple bounded contexts, use one checked-in EFPT configuration per context. Require every selected database object and generated CLR type to have exactly one owner, and validate that the configurations are disjoint and together cover the intended persistence inventory. Keep cross-context foreign keys in the database; model them as scalar IDs unless an explicit read contract owns a projection across the boundary. Register SQL Server contexts that participate in one workflow with the same scoped connection provider and identical connection-string name, and use an explicit Unit of Work transaction for atomic multi-context saves.

Start T4 customization from the official `Paradigm.Web.ApiTemplate` files. Record the source remote, exact revision, and original hashes beside the templates. When installed EFPT or Paradigm APIs have advanced, document and apply only reusable compatibility adaptations; never embed application entity-name lists or public-view whitelists in T4. Preserve the early ownership marker and compiled `GeneratedCodeAttribute`, then regenerate twice, build, and review the complete diff. Put application relationship corrections and domain behavior in supported partial/configuration seams.

Add the named connection string expected by the host through user secrets for local development:

```powershell
dotnet user-secrets set `
  "ConnectionStrings:ApplicationDatabase" `
  "Server=(localdb)\MSSQLLocalDB;Database=Sample;Integrated Security=true" `
  --project src/Sample.WebApi
```

The name passed to `RegisterContext<TContext>` must match the configuration key.

## Reverse engineer

Build the database project and publish its DACPAC to a disposable database before reverse engineering. A live database preserves view and computed-column metadata that DACPAC input may not expose completely. From the Data project/output root, run the pinned local command with the disposable connection supplied at execution time:

```powershell
dotnet tool restore
$configs = @(
  './efcpt-access-config.json',
  './efcpt-masterdata-config.json',
  './efcpt-operations-config.json',
  './efcpt-sales-config.json'
)

foreach ($config in $configs) {
  dotnet tool run efcpt -- $env:ConnectionStrings__ApplicationDatabase mssql `
    --input $config `
    --output .
}
```

The Visual Studio extension is optional convenience only. If a team uses it, maintain its extension-specific configuration independently and do not describe it as the deterministic build/regeneration path.

After generation, review the complete diff before adding behavior. Verify primary-key types, nullability, table and keyless `ToView` mappings, navigation properties, stored-procedure signatures, context naming, output paths, and the exact auto-generated ownership marker. Build immediately. Generation is not a substitute for reviewing the database contract.

Every consumer-facing major entity or transactional table should have a schema-bound `{Entity}View`. The view retains the entity's identifiers and scalar mapping surface, expands commonly used foreign keys with bounded descriptive joins, and remains one row per entity. A generated getter-only `I{Entity} : IEntity<TId>` contract should compile against both shapes. View-only joined fields remain on the view. Helper/reporting views and internal/status/history tables require a concrete consumer rather than an artificial entity interface.

## Add hand-written behavior

Create partial files beside generated entity and context files. Put invariants and mapping behavior in entity partials. Put extra model configuration in the context partial hook exposed by the template. Add repository contracts and implementations only after the generated types have stable names and identifiers.

Do not place application behavior inside T4 output. Do not add database credentials to generated configuration.

## Regenerate safely

Before every regeneration, commit or shelve unrelated work so the generated diff is visible. When generated and handwritten partial files share a directory, treat only an exact generated-filename manifest as replaceable: back up and restore that complete manifest across all contexts, and never recursively clear the directory. Validate each ownership marker and the disjoint object inventory, regenerate all configs twice, build after generation, and require byte-stable output. Then inspect interface-generator output and mapper dependencies. If a manual change disappeared, move the behavior to a partial file or change the owning template rather than reapplying it by hand.

Repositories retrieve editable entities and read views. They do not map entities into API DTOs or contain handwritten SQL query/command strings. Use EF for simple bounded CRUD/read work and typed stored procedures for locking, pagination/reporting, complex multi-entity work, or performance-sensitive paths. Providers own mapping, validation orchestration, transactions, commits, and external side effects.

Continue with [Build a vertical slice](../sample-application.md).
