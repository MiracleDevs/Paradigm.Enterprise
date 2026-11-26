# Database-first models and contracts

Use this contract when setting up or extending a database-backed Paradigm application. Start with the reviewed Web API template and preserve its EF Core Power Tools T4 files, EFPT configuration, and Domain-to-Interfaces analyzer reference. Project creation alone does not reverse engineer a database or create working data endpoints.

## Generate the persistence model

Database-first Domain entity and database-view scaffold files MUST be produced by an actual EF Core Power Tools generation run using the reviewed T4 templates. NEVER author these files manually, copy a model from another application as a substitute, emit equivalent classes through a handwritten script, or use another scaffolder as a fallback. This includes temporary POCOs, records, stubs, and manually reconstructed files presented as generated output. An `EntityBase` inheritance clause, generated-looking filename, ownership comment, or `GeneratedCodeAttribute` does not prove EFPT provenance.

Configure EF Core Power Tools on the application's single Data project. Adapt the template's `efpt.sample.config.json` into an explicit module/context configuration: select the module's tables and views, enable the reviewed custom T4 templates, and set output paths/namespaces deliberately. The template uses Data `<Module>/Context` for contexts and sibling Domain `<Module>/Entities` for entities and database view classes. Preserve database identifiers and one consistent `TId`.

Run EF Core Power Tools against the reviewed database objects before building dependent repositories and providers. Inspect its actual output and build. If the database metadata, required schema objects, EFPT host, or reverse-engineering environment is unavailable, stop the dependent model/feature work, finish independent setup, and report the exact missing prerequisite and unrun generation step. Never create a temporary model to make the build pass or claim generation succeeded.

For consumer-facing entity tables, include the corresponding database `{Entity}View` read projection where required by the database guidance. Existing databases may need a reviewed schema change before that view can be generated; do not silently invent its shape or claim a view exists. Internal tables and helper/reporting projections remain consumer-specific decisions.

The reviewed T4 output has these roles:

| Generated type | Owner and expected shape |
| --- | --- |
| `Product` | Domain partial entity deriving from `EntityBase<TId, IProduct, Product, ProductView>` for the normal mapped entity/view pair |
| `ProductView` | Domain partial database-view class deriving from `EntityBase<TId>` and implementing the paired `IProduct` contract |
| Context and mappings | Data, using the generated Domain types and the reviewed template's context base/configuration |

Keep generated properties, mapping members, and EF configuration replaceable. Handwritten companion partial files may add behavior and validation to an existing EFPT-generated type; they must not recreate the database-derived fields, keys, navigation shape, or generated mapping members. Change generation inputs or the owning T4 template and rerun EFPT when the scaffold shape must change. Use the project's genuine ownership markers and required generated-code metadata for validation. Keyless or externally owned database objects need an explicitly reviewed mapping decision that still uses EFPT generation; it is not an exception allowing manual scaffold files. Never invent an identifier merely to satisfy a base class.

## Domain classes unrelated to database elements

Domain classes that do not represent database elements MAY be created manually. This includes value objects, domain policies/specifications, calculation classes, state helpers, and other non-persistence models. Handwritten Domain code is not limited to partial files: a separate class with a genuine non-database role needs no EFPT run, artificial database object, or `EntityBase` inheritance. Request/response DTOs and use-case inputs may also be handwritten in their appropriate owning layer.

Classify by the type's actual role. These classes must not replace, duplicate, or disguise a database-first entity/view scaffold. Missing EFPT prerequisites block only the work dependent on database generation; independent non-database Domain classes may proceed.

## Verify interface generation

The template's Interfaces project contains the source generator. Domain references it with `OutputItemType="Analyzer"`; the generated interfaces are emitted into the Domain compilation in the matching `.Interfaces.<Module>...` namespace. Do not assume the namespace means those generated types live in the Interfaces assembly, and do not replace the analyzer reference with an ordinary contract-project reference.

Build after EFPT generation. For a normal `Product` entity, inspect actual analyzer output and compiled types for `IProduct<TId> : IEntity<TId>` and its concrete-identifier `IProduct` specialization, getter-only scalar members, and appropriate read-only navigation accessors. The generator intentionally skips `*View` classes: paired Domain views implement the entity's contract; do not invent duplicate view interfaces or put concrete view records in Interfaces. Genuine separate request/value contracts remain distinct from database-generated views.

When contracts are missing or wrong, inspect the entity's recognized `EntityBase` syntax, identifier type, namespace, analyzer reference, compiler diagnostics, and generated output. Fix the generation input or reviewed generator; do not maintain a parallel handwritten interface to hide a failed generator.

## Acceptance evidence

For the first feature, verify an actual EFPT-generated entity/view pair, context mappings, and analyzer-produced interface before adding repository/provider/controller behavior. Record the EFPT configuration, selected source objects, reviewed T4 paths, generation result, and generated-file diff. Inspect or reproduce generation when provenance is uncertain; markers alone are insufficient. Reject manually created or unverified substitute scaffold files and do not mark database-first setup complete until actual generation is verified. Then verify the read path returns the canonical Domain view. A solution that compiles with placeholder context/provider files has not passed this database-first acceptance check.

Use [solution layout](solution-layout.md) for containing folders and `$paradigm-build-repository` for typed stored-procedure callers, parameter/result mappers, and SQL Server table-valued inputs.
