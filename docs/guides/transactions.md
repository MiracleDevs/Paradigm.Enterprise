# Transactions and commits

Repository write methods stage Entity Framework changes. They do not save automatically. A provider or another application workflow commits through the scoped `IUnitOfWork`.

The generic edit provider calls `CommitChangesAsync` for add, update, save, and delete operations. Do not add a second commit around a normal inherited operation unless another unit of work is genuinely involved.

`CommitChangesAsync` awaits registered commiteable objects sequentially in registration order. Without an active compatible transaction, an earlier context may already be persisted when a later context fails. A Unit of Work containing several contexts is therefore not atomic by registration alone.

## When an explicit transaction is useful

Use an explicit transaction when a custom workflow stages several operations that must succeed or fail as one database transaction. Repositories must be resolved before `CreateTransaction` is called because their constructors register contexts as commiteable objects.

```csharp
var firstRepository = GetRepository<IFirstRepository>();
var secondRepository = GetRepository<ISecondRepository>();

using var transaction = UnitOfWork.CreateTransaction();

try
{
    await firstRepository.AddAsync(firstEntity);
    await secondRepository.UpdateAsync(secondEntity);
    await UnitOfWork.CommitChangesAsync();
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

The ordering is significant. Stage changes, ask the Unit of Work to save registered contexts, then commit the database transaction. Roll back and rethrow on failure so centralized exception handling can translate the error.

## Context boundaries

The transaction is created from the first registered commiteable and attaches the others. All participating contexts must be able to use the same underlying transaction. A Unit of Work does not create a distributed transaction across unrelated databases or providers.

If a workflow crosses independent stores or external services, define the failure and retry model explicitly. A database rollback cannot undo an email, remote API call, or blob upload.

Do not retry a write merely because a transient-failure policy is available. A retry is safe only when the operation is idempotent or duplicate execution is detected. Idempotency keys, durable inboxes, and deduplication are application patterns and are not supplied by the Unit of Work.

When a committed database change must reliably trigger later work, an outbox can store that intent in the same database transaction for separate delivery. A saga can coordinate several independent participants through persisted state and compensation. Paradigm.Enterprise does not implement either pattern. Both require monitoring, duplicate handling, repair, and tests.

## Domain boundaries

A technical transaction can contain several repository operations, but aggregate boundaries still matter. Avoid using a broad transaction to compensate for a model that permits uncontrolled cross-aggregate mutation.

Read [Design principles](../design-principles.md) for consistency boundaries and [Secure delivery](secure-delivery.md) for operational treatment of retries, releases, and recovery.
