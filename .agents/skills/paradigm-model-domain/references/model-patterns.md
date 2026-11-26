# Model patterns

## Generated persistence entity

Database-first Domain entity and view scaffold files MUST come from an actual EF Core Power Tools run using the reviewed T4 templates. Never manually author, copy, or script a replacement scaffold, including temporary stubs. If generation is unavailable, stop dependent work and report the missing prerequisite. Keep generated properties and mappings replaceable. A generated persistence shape may retain public setters only when its generator/scaffolding requires them and the exception is documented and configured for deterministic validation.

## Handwritten behavior and non-persistence models

Domain classes unrelated to database elements may be authored manually as separate files, including value objects, policies/specifications, calculators, state helpers, and other non-persistence models. They need no EFPT generation, artificial database element, or `EntityBase` inheritance. Request/response models belong in their appropriate owning layer. None of these types may substitute for a database-first entity/view scaffold.

For an existing EFPT-generated entity/view, add behavior and validation in a separate partial file. Do not declare the database-derived property/key/navigation shape there. Put state transitions behind behavior methods and test success and rejection paths. Keep entity contracts getter-only.

Entity-owned mapping may copy identity according to the installed contract, but it must call behavior such as `Rename` or `Activate` for transition-sensitive state.

## Separate read and write shapes

Use the EFPT-generated write entity with its partial behavior for invariants and an EFPT-generated database view for database-first search/display fields. Keep genuinely separate transport models explicit. Map deliberately; do not expose navigation graphs as API contracts or create a handwritten record to replace a required database view.

## Aggregate

Choose a root for changes that must remain immediately consistent. Repositories expose the root. Child removal goes through root/repository aggregate helpers, not a separately acquired child repository.

## State helpers

`StateFactory` uses framework reflection and naming conventions. It organizes transitions but does not persist state, publish events, register types, or provide workflow durability.
