# Provider transaction patterns

Resolve all repositories first; construction registers their contexts. In an `EditProviderBase`, use its protected `UnitOfWork`. In a custom `IProvider`/`ProviderBase`, constructor-inject `IUnitOfWork`; `ProviderBase` does not provide it. Then create a compatible transaction, stage work, call `CommitChangesAsync`, and commit the transaction. On failure, roll back and rethrow for centralized translation.

Several registered contexts are saved sequentially and are not atomic without a shared compatible transaction. Do not claim atomicity across unrelated databases.

A database transaction cannot undo email, blob, cache, queue, or remote API work. Choose and test an explicit policy:

- perform best-effort work after commit;
- make retries idempotent/deduplicated;
- store an outbox intent in the same transaction and deliver later;
- persist workflow state and compensate independent participants.

Paradigm does not implement an outbox, dispatcher, saga, or delivery guarantee.
