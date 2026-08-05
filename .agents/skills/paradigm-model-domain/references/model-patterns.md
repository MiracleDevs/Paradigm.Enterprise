# Model patterns

## Generated persistence entity

Keep generated properties and mappings replaceable. Add `partial class` behavior in a separate file. A generated persistence shape may retain public setters only when its generator/scaffolding requires them and the exception is documented and configured for deterministic validation. Prevent invalid transitions through domain methods and validation rather than editing generated output.

## Handwritten entity

Use private/protected setters. Put state transitions behind behavior methods and test success and rejection paths. Keep entity contracts getter-only. Let request/view models remain mutable when binding or serialization requires it.

Entity-owned mapping may copy identity according to the installed contract, but it must call behavior such as `Rename` or `Activate` for transition-sensitive state.

## Separate read and write shapes

Use the write entity for invariants and a view for search/display fields. Map deliberately; do not expose navigation graphs as API contracts.

## Aggregate

Choose a root for changes that must remain immediately consistent. Repositories expose the root. Child removal goes through root/repository aggregate helpers, not a separately acquired child repository.

## State helpers

`StateFactory` uses framework reflection and naming conventions. It organizes transitions but does not persist state, publish events, register types, or provide workflow durability.
