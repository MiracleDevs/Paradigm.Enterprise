# RFC: Integration of Entity and View Repositories

- **RFC ID**: 2025-05-12-join-view-repository
- **Status**: Draft
- **Author(s)**: [Your Name]
- **Created**: 2025-05-12
- **Last Updated**: 2025-05-12

## Summary

This RFC proposes merging the current separate `EditRepository` (for entities) and `ReadRepository` (for views) into a unified `EntityViewRepository` pattern. This integration would simplify development workflows while maintaining the performance benefits of database views for read operations and proper entity management for write operations. The goal is to reduce duplication, simplify provider implementation, and maintain a cleaner codebase without sacrificing the architectural benefits of the current separation.

## Motivation

Currently, our framework employs a separate repository pattern for entities (`EditRepository`) and views (`ReadRepository/ViewRepository`). This separation provides clean responsibilities but introduces several challenges:

1. **Development Overhead**: Developers must create and maintain separate repository classes for each entity and its corresponding view.
2. **Decision Paralysis**: Sometimes it's difficult to know if one should create a view method or an entity method, and what's the best place to create the entity. Further more, when creating simplified views like a SummaryView for an entity, creating a third repository is a waste of resources, for something that will probably be used once.
3. **Provider Complexity**: Providers must manage two repository types, increasing code complexity and cognitive load.
4. **Duplicate Method Definitions**: Similar methods are often implemented in both repositories.
5. **Workflow Friction**: Working with both repositories creates additional steps when implementing new features.

Despite these challenges, the separation serves an important purpose: views optimize read operations by pre-joining related entities, while pure entities provide clean write capabilities. This RFC aims to preserve these benefits while eliminating the drawbacks of complete separation.

## Detailed Design

### JoinRepository Pattern

We propose a new `EntityViewRepository<TEntity, TView>` pattern that encapsulates both entity and view operations:

```csharp
public interface IEntityViewRepository<TEntity, TView> : IRepository
    where TEntity : EntityBase
    where TView : EntityBase
{
    // Read operations (formerly in ViewRepository)
    Task<TView?> GetViewByIdAsync(int id);
    Task<IEnumerable<TView>> GetViewByIdsAsync(IEnumerable<int> ids);
    Task<IEnumerable<TView>> GetAllViewsAsync();
    Task<PaginatedResultDto<TView>> SearchViewPaginatedAsync(FilterTextPaginatedParameters parameters);

    // Write operations (formerly in EntityRepository)
    Task<TEntity> AddAsync(TEntity entity);
    Task AddAsync(IEnumerable<TEntity> entities);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task UpdateAsync(IEnumerable<TEntity> entities);
    Task DeleteAsync(TEntity entity);
    Task DeleteAsync(IEnumerable<TEntity> entities);

    // Entity read operations
    Task<TEntity?> GetEntityByIdAsync(int id);
    Task<IEnumerable<TEntity>> GetEntityByIdsAsync(IEnumerable<int> ids);
}
```

### Implementation Base Class

```csharp
public abstract class EntityViewRepositoryBase<TEntity, TView, TContext> : RepositoryBase<TContext>, IJoinRepository<TEntity, TView>
    where TEntity : EntityBase
    where TView : EntityBase
    where TContext : DbContextBase
{
    protected EntityViewRepositoryBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    // View operations implementations
    public virtual async Task<TView?> GetViewByIdAsync(int id) =>
        await AsQueryableView().FirstOrDefaultAsync(x => x.Id == id);

    public virtual async Task<IEnumerable<TView>> GetViewByIdsAsync(IEnumerable<int> ids) =>
        await AsQueryableView().Where(x => ids.Contains(x.Id)).ToListAsync();

    public virtual async Task<IEnumerable<TView>> GetAllViewsAsync() =>
        await AsQueryableView().ToListAsync();

    public virtual async Task<PaginatedResultDto<TView>> SearchViewPaginatedAsync(FilterTextPaginatedParameters parameters)
    {
        // Implementation similar to current ReadRepositoryBase
    }

    // Entity operations implementations
    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await GetEntityDbSet().AddAsync(entity);
        return entity;
    }

    public virtual async Task AddAsync(IEnumerable<TEntity> entities)
    {
        await GetEntityDbSet().AddRangeAsync(entities);
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        await DeleteRemovedAggregatesAsync(entity);
        GetEntityDbSet().Update(entity);
        return await Task.FromResult(entity);
    }

    // Other methods from EditRepositoryBase

    // Helper methods
    protected virtual IQueryable<TView> AsQueryableView() { ... }
    protected virtual DbSet<TEntity> GetEntityDbSet() { ... }
    protected virtual DbSet<TView> GetViewDbSet() { ... }

    // Method for handling aggregates similar to current EditRepositoryBase
    protected virtual Task DeleteRemovedAggregatesAsync(TEntity entity)
    {
        return Task.CompletedTask;
    }
}
```

### Provider Changes

The existing provider pattern would be simplified to use a single repository:

```csharp
public abstract class EntityViewProviderBase<TInterface, TEntity, TView, TRepository> : ProviderBase, IEditProvider<TView>
    where TInterface : Interfaces.IEntity
    where TEntity : EntityBase<TInterface, TEntity, TView>, TInterface, new()
    where TView : EntityBase, TInterface, new()
    where TRepository : IJoinRepository<TEntity, TView>
{
    protected TRepository Repository { get; }
    protected IUnitOfWork UnitOfWork { get; }

    protected JoinProviderBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Repository = serviceProvider.GetRequiredService<TRepository>();
        UnitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
    }

    // Implementation of IEditProvider<TView> methods using Repository for both
    // entity and view operations
}
```

### Migration Strategy

The implementation will follow these steps:

1. Create new interfaces and base classes without modifying existing ones
2. Provide extension methods for easier migration from existing repositories
3. Allow gradual migration of repositories on a per-entity basis
4. Maintain backward compatibility with existing `IReadRepository` and `IEditRepository` interfaces

### Backward Compatibility

To ensure backward compatibility:

1. Existing repositories will continue to function without changes
2. New `IJoinRepository` can implement both `IReadRepository<TView>` and `IEditRepository<TEntity>` interfaces
3. Extension methods will bridge the gap during transition

## Alternatives Considered

### Alternative 1: Complete Repository Redesign

We considered a complete redesign of the repository pattern:

**Pros:**

- Opportunity to address all current limitations
- Cleaner implementation without legacy concerns

**Cons:**

- Significant migration effort
- High risk of introducing regressions
- Requires retraining developers

## Testing Strategy

The implementation will be tested via:

1. **Unit Tests**:
   - Tests for the new `EntityViewRepositoryBase` class
   - Tests for backward compatibility with existing interfaces
   - Tests for the migration extension methods

2. **Integration Tests**:
   - Tests using real database connections to ensure proper function
   - Performance comparison tests between old and new implementations

3. **Example Implementation**:
   - Migration of selected entities in the example application
   - Side-by-side comparison of implementations

## Rollout Plan

1. **Phase 1 - Infrastructure**:
   - Implement new interfaces and base classes
   - Create comprehensive unit tests
   - Document the new approach

2. **Phase 2 - Example Implementation**:
   - Migrate example entities to the new pattern
   - Create migration guides with examples
   - Collect feedback from development team

3. **Phase 3 - Toolkit Enhancement**:
   - Update code generation tools to support the new pattern
   - Add migration utilities
   - Update documentation

4. **Phase 4 - Gradual Adoption**:
   - Encourage teams to adopt the new pattern for new entities
   - Provide support for migrating existing entities
   - Monitor performance and developer feedback

## Dependencies

- No direct dependencies on other RFCs
- Requires changes to the core Paradigm.Enterprise libraries:
  - Paradigm.Enterprise.Domain
  - Paradigm.Enterprise.Data
  - Paradigm.Enterprise.Providers

## Open Questions

1. Should we consider a different naming convention for the methods to better indicate their purpose?
2. How will this integration affect performance for read-heavy operations?
3. Can this be difficult for cases with a lot of custom methods?

## Example Implementation

Below is an example of how a product repository would be implemented with the new pattern:

```csharp
// Interface
public interface IProductRepository : IEntityViewRepository<Product, ProductView>
{
    // Additional business-specific methods
    Task<IEnumerable<ProductView>> FindByCategoryAsync(string category);
    Task<IEnumerable<ProductView>> GetAvailableProductsAsync();
}

// Implementation
public class ProductRepository : EntityViewRepositoryBase<Product, ProductView, ApplicationDbContext>, IProductRepository
{
    public ProductRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<IEnumerable<ProductView>> FindByCategoryAsync(string category)
    {
        return await AsQueryableView()
            .Where(p => p.Category == category)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductView>> GetAvailableProductsAsync()
    {
        return await AsQueryableView()
            .Where(p => p.IsAvailable && p.StockQuantity > 0)
            .ToListAsync();
    }
}

// Provider
public class ProductProvider : EntityViewProviderBase<IProduct, Product, ProductView, IProductRepository>, IProductProvider
{
    public ProductProvider(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<IEnumerable<ProductView>> GetByCategoryAsync(string category)
    {
        return await Repository.FindByCategoryAsync(category);
    }

    public async Task<IEnumerable<ProductView>> GetAvailableProductsAsync()
    {
        return await Repository.GetAvailableProductsAsync();
    }
}
```

## References

- [Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design#the-repository-pattern)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- Current implementation in Paradigm.Enterprise framework