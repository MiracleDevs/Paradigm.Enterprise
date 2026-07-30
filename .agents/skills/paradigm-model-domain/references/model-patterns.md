# Model patterns

## Generated persistence entity

Keep generated properties and mappings replaceable. Add `partial class` behavior in a separate file. If public setters are required by scaffolding, prevent invalid transitions through domain methods and validation rather than editing the generated file.

## Separate read and write shapes

Use the write entity for invariants and a view for search/display fields. Map deliberately; do not expose navigation graphs as API contracts.

## Aggregate

Choose a root for changes that must remain immediately consistent. Repositories expose the root. Child removal goes through root/repository aggregate helpers, not a separately acquired child repository.

## State helpers

`StateFactory` uses framework reflection and naming conventions. It organizes transitions but does not persist state, publish events, register types, or provide workflow durability.
