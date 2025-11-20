# RFC: Protected Aggregate Deletion Methods in EditRepositoryBase

- **RFC ID**: 2025-01-15-protected-aggregate-deletion-methods
- **Status**: Draft
- **Author(s)**: Pablo Ordóñez
- **Created**: 2025-01-15
- **Last Updated**: 2025-01-15

## Summary

This RFC proposes adding protected helper methods to `EditRepositoryBase` that allow aggregate root repositories to delete aggregated child entities directly through the DbContext, without requiring separate repositories for each aggregated entity. Additionally, this RFC proposes changing `DeleteRemovedAggregatesAsync` from an async method to a synchronous method, as the underlying operations are synchronous. This change maintains Domain-Driven Design (DDD) aggregate boundaries by preventing direct access to child entities while still enabling proper cleanup of removed aggregates during update operations.

## Motivation

Currently, when implementing `DeleteRemovedAggregatesAsync` in aggregate root repositories, developers must create separate repositories for aggregated entities (e.g., `ISalesOrderLineRepository`) to delete removed child entities. This violates DDD aggregate principles because:

1. **Aggregate Boundary Violation**: Separate repositories expose aggregated entities directly, allowing them to be modified or deleted outside the aggregate root
2. **Unnecessary Repository Proliferation**: Forces creation of repositories for entities that should only be accessed through their aggregate root
3. **Architectural Inconsistency**: Contradicts the principle that only aggregate roots should have repositories
4. **Misleading Async API**: The current `DeleteRemovedAggregatesAsync` method is async but performs no async operations, requiring unnecessary `await Task.CompletedTask` in implementations

**Current Problematic Pattern:**

```csharp
// SalesOrderRepository.cs
protected override async Task DeleteRemovedAggregatesAsync(SalesOrder entity)
{
    // Violates aggregate boundary - requires ISalesOrderLineRepository
    await this.GetRepository<ISalesOrderLineRepository>().DeleteAsync(entity.SalesOrderLineTracker.Deleted);
}
```

By adding protected methods that directly access the DbContext's `Set<T>()` method, aggregate root repositories can delete aggregated entities without exposing them through separate repositories, maintaining proper aggregate boundaries. Additionally, making `DeleteRemovedAggregatesAsync` synchronous accurately reflects that the operations are synchronous. Or at least make a sync version and leave the async as deprecated for the next version, until we can safely delete it.

## Detailed Design

### Proposed Changes

#### 1. Add Protected RemoveAggregate Methods

Add two protected methods to `EditRepositoryBase<TEntity, TContext>`:

```csharp
/// <summary>
/// Removes an aggregated entity from the database context.
/// This method allows aggregate root repositories to delete child entities
/// without requiring separate repositories, maintaining aggregate boundaries.
/// If the entity is null, no operation is performed.
/// </summary>
/// <typeparam name="TAggregatedEntity">The type of the aggregated entity.</typeparam>
/// <param name="entity">The aggregated entity to remove.</param>
protected void RemoveAggregate<TAggregatedEntity>(TAggregatedEntity? entity)
    where TAggregatedEntity : EntityBase
{
    if (entity is null)
        return;

    EntityContext.Set<TAggregatedEntity>().Remove(entity);
}

/// <summary>
/// Removes multiple aggregated entities from the database context.
/// This method allows aggregate root repositories to delete child entities
/// without requiring separate repositories, maintaining aggregate boundaries.
/// If the collection is null or empty, no operation is performed.
/// </summary>
/// <typeparam name="TAggregatedEntity">The type of the aggregated entity.</typeparam>
/// <param name="entities">The aggregated entities to remove.</param>
protected void RemoveAggregate<TAggregatedEntity>(IEnumerable<TAggregatedEntity>? entities)
    where TAggregatedEntity : EntityBase
{
    if (entities is null)
        return;

    var entityList = entities.ToList();
    if (entityList.Count == 0)
        return;

    EntityContext.Set<TAggregatedEntity>().RemoveRange(entityList);
}
```

#### 2. Change DeleteRemovedAggregatesAsync to Synchronous

Change `DeleteRemovedAggregatesAsync` from an async method to a synchronous method:

**Current Implementation:**
```csharp
/// <summary>
/// Deletes the removed aggregates.
/// </summary>
protected virtual async Task DeleteRemovedAggregatesAsync(TEntity entity)
{
    await Task.CompletedTask;
}
```

**Proposed Implementation:**
```csharp
/// <summary>
/// Deletes the removed aggregates.
/// </summary>
/// <param name="entity">The entity.</param>
protected virtual void DeleteRemovedAggregates(TEntity entity)
{
}
```

**Update Method Calls in UpdateAsync Methods:**

**Current:**
```csharp
public virtual async Task<TEntity> UpdateAsync(TEntity entity)
{
    await DeleteRemovedAggregates(entity);
    GetDbSet().Update(entity);
    return await Task.FromResult(entity);
}

public async Task UpdateAsync(IEnumerable<TEntity> entities)
{
    foreach (var entity in entities)
        await DeleteRemovedAggregates(entity);

    GetDbSet().UpdateRange(entities);
}
```

**Proposed:**
```csharp
public virtual async Task<TEntity> UpdateAsync(TEntity entity)
{
    DeleteRemovedAggregates(entity);
    GetDbSet().Update(entity);
    return await Task.FromResult(entity);
}

public async Task UpdateAsync(IEnumerable<TEntity> entities)
{
    foreach (var entity in entities)
        DeleteRemovedAggregates(entity);

    GetDbSet().UpdateRange(entities);
}
```

### Implementation Location

**File**: `src/Paradigm.Enterprise.Data/Repositories/EditRepositoryBase.cs`

**Location**:
- Add `RemoveAggregate` methods in the `#region Protected Methods` section
- Update `DeleteRemovedAggregatesAsync` method signature and implementation
- Update calls to `DeleteRemovedAggregatesAsync` in `UpdateAsync` methods (lines 53 and 65)

### Usage Example

**Before (Current Pattern - Violates Aggregate Boundaries):**

```csharp
public class SalesOrderRepository : EditRepositoryBase<SalesOrder, ApplicationDbContext>
{
    protected override async Task DeleteRemovedAggregatesAsync(SalesOrder entity)
    {
        // Requires ISalesOrderLineRepository - violates aggregate boundary
        await this.GetRepository<ISalesOrderLineRepository>().DeleteAsync(entity.SalesOrderLineTracker.Deleted);
    }
}
```

**After (Proposed Pattern - Maintains Aggregate Boundaries):**

```csharp
public class SalesOrderRepository : EditRepositoryBase<SalesOrder, ApplicationDbContext>
{
    protected override void DeleteRemovedAggregatesAsync(SalesOrder entity)
    {
        // Direct access to DbSet - maintains aggregate boundary
        // Null/empty checks handled internally by RemoveAggregate
        RemoveAggregate(entity.SalesOrderLineTracker.Deleted);
    }
}
```

### Design Decisions

1. **Synchronous Methods**: The methods are synchronous because EF Core's `Remove` and `RemoveRange` operations are synchronous - they only mark entities for deletion in the change tracker. The actual database deletion occurs when `SaveChangesAsync` is called.

2. **Null/Empty Handling**: The `RemoveAggregate` methods handle null and empty collections internally to simplify implementation classes. This eliminates the need for callers to check before invoking the methods, reducing boilerplate code.

3. **Synchronous DeleteRemovedAggregatesAsync**: Changed from async to synchronous because:
   - The underlying `RemoveAggregate` methods are synchronous
   - There's no I/O or async work being performed
   - Simplifies the implementation and removes unnecessary `Task.CompletedTask` returns
   - More accurately reflects the actual operation (synchronous change tracker updates)

4. **Protected Access**: Methods are protected to ensure only derived repository classes can use them, preventing misuse from outside the repository hierarchy.

5. **Generic Constraint**: Both methods require `TAggregatedEntity : EntityBase` to ensure type safety and consistency with the framework's entity model.

6. **Direct DbContext Access**: Using `EntityContext.Set<TAggregatedEntity>()` is appropriate here because:
   - We're within the same DbContext instance
   - This is an internal implementation detail of the repository
   - It maintains aggregate boundaries while enabling necessary cleanup operations

7. **Method Naming**: `RemoveAggregate` clearly indicates the purpose - removing aggregated entities from within an aggregate root repository. The method name `DeleteRemovedAggregatesAsync` must be changed to `DeleteRemovedAggregates`.

## Alternatives Considered

### Alternative 1: Keep Current Pattern (Rejected)

**Approach**: Continue requiring separate repositories for aggregated entities.

**Advantages**:
- No changes to framework code
- Consistent with existing repository pattern

**Disadvantages**:
- Violates DDD aggregate boundaries
- Forces unnecessary repository creation
- Allows direct access to aggregated entities
- Architectural inconsistency

**Why Rejected**: Violates core DDD principles and creates architectural debt.

### Alternative 2: Keep DeleteRemovedAggregatesAsync Async (Rejected)

**Approach**: Keep `DeleteRemovedAggregatesAsync` as an async method even though it's synchronous.

**Advantages**:
- No breaking changes to existing implementations
- Consistent with async pattern in other methods

**Disadvantages**:
- Misleading API - suggests async work when none occurs
- Requires unnecessary `await Task.CompletedTask` in implementations
- Adds complexity without benefit

**Why Rejected**: Since the underlying operations are synchronous and there's no async work, making the method synchronous is more accurate and simpler. The breaking change is acceptable for a cleaner API.

### Alternative 3: Add Async Versions (Rejected)

**Approach**: Make the `RemoveAggregate` methods async and await the Remove operations.

**Advantages**:
- Consistent with other async methods in the class

**Disadvantages**:
- Unnecessary - EF Core Remove operations are synchronous
- Adds complexity without benefit
- Misleading API (suggests I/O operation when none occurs)

**Why Rejected**: EF Core's Remove/RemoveRange are synchronous operations that only update the change tracker. Making them async would be misleading.

### Alternative 4: Require Null Checks in Implementation (Rejected)

**Approach**: Require callers to check for null/empty before calling `RemoveAggregate`.

**Advantages**:
- Simpler base method implementation
- Explicit null handling at call site

**Disadvantages**:
- Adds boilerplate code to every implementation
- Easy to forget null checks
- Less user-friendly API

**Why Rejected**: Handling null/empty internally provides a cleaner, more user-friendly API and reduces the chance of errors.

## Testing Strategy

### Unit Tests

1. **Test RemoveAggregate Single Entity**:
   - Verify entity is marked for deletion in change tracker
   - Verify method works with valid entity
   - Verify method works with null entity (no-op)
   - Verify method works with entity from different DbSet

2. **Test RemoveAggregate Multiple Entities**:
   - Verify all entities are marked for deletion
   - Verify method works with null collection (no-op)
   - Verify method works with empty collection (no-op)
   - Verify method works with large collections

3. **Test DeleteRemovedAggregatesAsync Synchronous Behavior**:
   - Verify method is synchronous (no async state machine)
   - Verify method can be called without await
   - Verify base implementation does nothing

4. **Test Integration with UpdateAsync**:
   - Verify aggregate deletion works in UpdateAsync flow
   - Verify entities are actually deleted on SaveChanges
   - Verify synchronous call doesn't break async flow

### Integration Tests

1. **End-to-End Aggregate Deletion**:
   - Create aggregate root with child entities
   - Remove some child entities via tracker
   - Update aggregate root
   - Verify removed entities are deleted from database

2. **Multiple Aggregate Types**:
   - Test with different aggregate structures
   - Verify no cross-contamination between aggregates

3. **Null/Empty Collection Handling**:
   - Test with null tracker collections
   - Test with empty tracker collections
   - Verify no exceptions are thrown

### Test Location

**File**: `src/Paradigm.Enterprise.Tests/Repositories/EditRepositoryBaseTests.cs` (new file or add to existing repository tests)

## Backward Compatibility

This change includes **one breaking change**:

### Breaking Change: DeleteRemovedAggregatesAsync Signature

The `DeleteRemovedAggregatesAsync` method signature changes from:
```csharp
protected virtual async Task DeleteRemovedAggregatesAsync(TEntity entity)
```

To:
```csharp
protected virtual void DeleteRemovedAggregates(TEntity entity)
```

**Impact**: Any existing repository implementations that override `DeleteRemovedAggregatesAsync` will need to:
1. Remove `async` keyword
2. Remove `await` keywords if present
3. Change return type from `Task` to `void`
4. Remove `return Task.CompletedTask` or `await Task.CompletedTask` statements
5. Change the method name.

### Backward Compatible Aspects

- Existing implementations using separate repositories still function (though not recommended). We recommend to delete aggregate repositories.
- New `RemoveAggregate` methods are additive only
- No breaking changes to public APIs (`IEditRepository` interface unchanged)

## Migration Guide

### For New Implementations

Use the new `RemoveAggregate` methods:

```csharp
protected override void DeleteRemovedAggregates(SalesOrder entity)
{
    // Null/empty checks handled internally - no need to check
    RemoveAggregate(entity.SalesOrderLineTracker.Deleted);
}
```

## Security Considerations

No security implications. The methods are protected and only accessible within the repository hierarchy, maintaining the same security model as existing repository methods.

## Performance Considerations

1. **No Performance Impact**: Methods are thin wrappers around EF Core's existing `Remove`/`RemoveRange` operations
2. **Change Tracker Efficiency**: EF Core efficiently handles batch removals
3. **Memory**: No additional memory overhead beyond existing EF Core operations
4. **Synchronous Overhead**: Removing async/await overhead improves performance slightly for synchronous operations
5. **Null Check Efficiency**: Early returns for null/empty collections avoid unnecessary work

## Open Questions

1. Should we add overloads that accept `IReadOnlyCollection<T>` for better immutability guarantees?
2. Should we add logging/tracing for aggregate deletion operations?
3. Should we add validation to ensure aggregated entities belong to the same aggregate root?

## References

- [Domain-Driven Design - Aggregates](https://martinfowler.com/bliki/DDD_Aggregate.html)
- [Entity Framework Core - Change Tracking](https://learn.microsoft.com/en-us/ef/core/change-tracking/)
- [Repository Pattern Best Practices](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)

