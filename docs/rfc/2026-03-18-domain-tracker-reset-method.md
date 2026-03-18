# RFC: DomainTracker Reset Method

- **RFC ID**: 2026-03-18-domain-tracker-reset-method
- **Status**: Implemented
- **Author(s)**: Iván Falletti
- **Created**: 2026-03-18
- **Last Updated**: 2026-03-18

## Summary

This RFC proposes adding a `Reset()` method to the `DomainTracker<TEntity>` class to enable clearing the Added, Edited, and Removed entity lists on demand. The change addresses edge cases where entity mapping operations require a clean tracker state before processing, but currently there is no way to clear these private lists. The solution is minimal, non-breaking, and provides explicit control over tracker state management.

## Motivation

During entity mapping scenarios, edge cases have been discovered where the DomainTracker lists need to be cleared before an entity is mapped. Currently, once entities are added to the tracker lists (Added, Edited, or Removed), there is no public mechanism to reset them.

**Problems:**

1. **No Reset Capability**: The internal lists (`_addedList`, `_editedList`, `_removedList`) are private with read-only collection properties, preventing clearing of tracked entities
2. **Edge Case Handling**: Specific mapping scenarios require a clean tracker state, but developers must work around this limitation by creating new tracker instances or accepting stale tracking data
3. **Implicit State**: Workarounds lead to unclear code intent and potential bugs where tracking state is unknowingly stale
4. **Limited Flexibility**: Cannot manage tracker lifecycle within the same instance across multiple mapping operations

**Example Scenario:**

```csharp
var tracker = new DomainTracker<User>();
// ... entity operations ...
tracker.Add(user1);

// Edge case: need to reset before next mapping operation
// Currently: must create new tracker instance
tracker = new DomainTracker<User>(); // Wasteful and unclear

// Proposed: simple reset
tracker.Reset(); // Clear all lists, ready for next operation
```

By adding a `Reset()` method, we provide:

- Explicit, intentional tracker state management
- Clear code semantics for mapping scenarios
- Flexibility to reuse tracker instances across operations
- Minimal architectural impact

## Detailed Design

### API Change

Add a single public method to `DomainTracker<TEntity>`:

```csharp
/// <summary>
/// Resets the tracker by clearing all Added, Edited, and Removed lists.
/// This is useful for edge case scenarios where tracker state needs to be cleared
/// before entity mapping operations.
/// </summary>
public void Reset()
{
    _addedList.Clear();
    _editedList.Clear();
    _removedList.Clear();
}
```

### Behavioral Specification

- **Clears all three internal lists**: `_addedList`, `_editedList`, `_removedList`
- **Idempotent**: Calling `Reset()` multiple times has no side effects beyond the first call
- **Non-breaking**: No change to existing public API; only an addition
- **Semantics**: Explicitly indicates intent to discard all tracked changes

### Implementation Details

- **Location**: `Paradigm.Enterprise.Domain/Entities/DomainTracker.cs`
- **Visibility**: Public method
- **No parameters**: Simple, clear contract
- **No return value**: Void method
- **Thread safety**: Same as existing tracker (not thread-safe by design)

### Integration Points

The `Reset()` method will be called in:

1. **Specific mapping scenarios**: Before entity mapping operations that require clean tracker state
2. **Provider workflows**: In provider methods where tracker reuse across operations is beneficial
3. **Integration tests**: To reset state between test cases when using the same tracker instance

### Architecture Compliance

This change:

- ✅ Maintains domain layer purity (DomainTracker contains no infrastructure dependencies)
- ✅ Does not violate layered architecture (no new cross-layer dependencies introduced)
- ✅ Is minimal and focused (single method addition)
- ✅ Preserves existing behavior (fully backward compatible)

## Alternatives Considered

### Alternative 1: Dispose and Recreate Pattern
**Approach**: Treat DomainTracker as disposable; create new instances when reset is needed.

**Advantages**:
- No new method; uses existing patterns
- Clear lifecycle management

**Disadvantages**:
- Inefficient (allocates new objects repeatedly)
- Requires more verbose code in calling contexts
- Less intentional (semantic ambiguity: why create new instance?)
- Impacts garbage collection pressure in high-throughput scenarios

**Decision**: Rejected in favor of explicit `Reset()` method for efficiency and clarity.

### Alternative 2: Expose Mutable Properties
**Approach**: Change Added/Edited/Removed properties to return mutable lists instead of IReadOnlyCollection.

**Advantages**:
- Maximum flexibility for callers

**Disadvantages**:
- ❌ Violates encapsulation (lists could be externally modified)
- ❌ Creates implicit state via side effects (callers could clear lists unexpectedly)
- ❌ Makes tracking behavior unpredictable
- ❌ Breaks the read-only contract

**Decision**: Rejected because it violates design principles.

### Alternative 3: Constructor Parameter for Reset Behavior
**Approach**: Add a constructor parameter to enable/disable automatic reset behavior.

**Advantages**:
- Flexible reset strategy

**Disadvantages**:
- Over-engineered for current use cases
- Adds unnecessary complexity
- Harder to reason about tracker state
- Constructor complexity

**Decision**: Rejected in favor of explicit method call for clarity.

## Testing Strategy

### Unit Tests

**Test 1: Reset clears all lists**
```csharp
[Fact]
public void Reset_ClearsAddedEditedAndRemovedLists()
{
    var tracker = new DomainTracker<User>();
    var user1 = new User { Id = 1 };
    var user2 = new User { Id = 2 };
    var user3 = new User { Id = 3 };

    tracker.Add(user1);
    tracker.Edit(user2);
    tracker.Remove(user3);

    Assert.NotEmpty(tracker.Added);
    Assert.NotEmpty(tracker.Edited);
    Assert.NotEmpty(tracker.Removed);

    tracker.Reset();

    Assert.Empty(tracker.Added);
    Assert.Empty(tracker.Edited);
    Assert.Empty(tracker.Removed);
}
```

**Test 2: Reset is idempotent**
```csharp
[Fact]
public void Reset_IsIdempotent()
{
    var tracker = new DomainTracker<User>();
    tracker.Add(new User { Id = 1 });

    tracker.Reset();
    var firstResetAdded = tracker.Added.Count;

    tracker.Reset();
    var secondResetAdded = tracker.Added.Count;

    Assert.Equal(firstResetAdded, secondResetAdded);
    Assert.Equal(0, secondResetAdded);
}
```

**Test 3: Operations after reset work correctly**
```csharp
[Fact]
public void OperationsAfterReset_WorkCorrectly()
{
    var tracker = new DomainTracker<User>();
    var user1 = new User { Id = 1 };
    var user2 = new User { Id = 2 };

    tracker.Add(user1);
    tracker.Reset();
    tracker.Add(user2);

    Assert.Single(tracker.Added);
    Assert.Contains(user2, tracker.Added);
    Assert.DoesNotContain(user1, tracker.Added);
}
```

### Integration Tests

- Test tracker reset in a provider workflow where mapping operations occur
- Test that reset doesn't affect other domain tracker instances
- Test reset in multi-operation provider methods

### Coverage

- Line coverage: 100% (trivial implementation)
- Branch coverage: 100% (no branching in method)

## Rollout Plan

### Phase 1: Implementation (No Release)
- Add `Reset()` method to `DomainTracker<TEntity>`
- Add comprehensive unit tests
- Update documentation (IntelliSense comments already included)

### Phase 2: Internal Adoption
- Identify all edge case scenarios requiring reset
- Update provider and mapping logic to call `Reset()` where needed
- Validate against existing integration tests

### Phase 3: Release
- Include in next minor version release
- Include in release notes as new feature
- No breaking changes; fully backward compatible

### Backward Compatibility

- ✅ **Fully backward compatible**: New public method only; no existing API changes
- ✅ **No migration needed**: Existing code continues to work unchanged
- ✅ **Opt-in**: Callers choose when to use `Reset()`

### Documentation Updates

- Update [DomainTracker documentation](../../overview.md) with reset method usage
- Add IntelliSense XML comments (included in implementation)
- Update domain layer best practices guide with scenarios where reset is appropriate

## Dependencies

- None. This is a utility method on an existing class with no external dependencies.

## Open Questions

1. **Scope**: Are there other domain tracker operations that might benefit from state management methods (e.g., `Count()`, `HasChanges()`)?
2. **Concurrency**: Should we document thread-safety guarantees explicitly in the reset method comment?
3. **Telemetry**: Should we track when Reset is called for monitoring edge case frequency?

## References

- [DomainTracker Source](../../../src/Paradigm.Enterprise.Domain/Entities/DomainTracker.cs)
- [Architecture Rules](../rules/01-architecture.md)
- [Domain-Driven Design Rules](../rules/04-ddd.md)
- [Coding Standards](../rules/02-coding-standards.md)
