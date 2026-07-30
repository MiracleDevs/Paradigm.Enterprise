using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace Paradigm.Enterprise.Data.Repositories;

/// <summary>
/// Implements Entity Framework change staging for an entity and provides aggregate-removal hooks.
/// </summary>
/// <typeparam name="TEntity">The entity managed by the repository.</typeparam>
/// <typeparam name="TContext">The Entity Framework context used to stage changes.</typeparam>
/// <typeparam name="TId">The value type used for entity identifiers.</typeparam>
/// <remarks>Add, update, and delete operations do not save the context; commit through the unit of work.</remarks>
/// <example>
/// An aggregate repository can remove children omitted by an update while keeping persistence inside
/// the aggregate boundary:
/// <code>
/// public sealed class OrderRepository
///     : EditRepositoryBase&lt;Order, SalesDbContext, int&gt;
/// {
///     public OrderRepository(IServiceProvider services) : base(services) { }
///
///     protected override IQueryable&lt;Order&gt; AsQueryable() =&gt;
///         EntityContext.Orders
///             .Include(order =&gt; order.Lines);
///
///     protected override void DeleteRemovedAggregates(Order order)
///     {
///         RemoveAggregate(order.LineTracker.Removed);
///     }
///
///     // Implement GetSearchPaginatedFunction as shown on ReadRepositoryBase.
/// }
///
/// var order = await repository.GetByIdAsync(orderId);
/// order!.ChangeShippingAddress(newAddress);
/// await repository.UpdateAsync(order);       // stages entity and child removals
/// await unitOfWork.CommitChangesAsync();      // executes SaveChangesAsync
/// </code>
/// <see cref="UpdateAsync(TEntity)"/> calls <see cref="DeleteRemovedAggregates"/> before marking the
/// root modified. Override that hook when the domain model tracks removed aggregate children.
/// </example>
public abstract class EditRepositoryBase<TEntity, TContext, TId> : ReadRepositoryBase<TEntity, TContext, TId>, IEditRepository<TEntity, TId>
    where TEntity : EntityBase<TId>
     where TContext : DbContextBase<TId>
    where TId : struct, IEquatable<TId>
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="EditRepositoryBase{TEntity, TContext, TId}" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected EditRepositoryBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Begins tracking a new entity in the added state.
    /// </summary>
    /// <param name="entity">The entity to stage for insertion.</param>
    /// <returns>The same entity, now tracked in the added state.</returns>
    /// <remarks>No database write occurs until the context or unit of work is committed.</remarks>
    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await GetDbSet().AddAsync(entity);
        return entity;
    }

    /// <summary>
    /// Begins tracking a sequence of new entities in the added state.
    /// </summary>
    /// <param name="entities">The entities to stage for insertion.</param>
    /// <remarks>The sequence is enumerated by Entity Framework. No database write occurs until commit.</remarks>
    public virtual async Task AddAsync(IEnumerable<TEntity> entities)
    {
        await GetDbSet().AddRangeAsync(entities);
    }

    /// <summary>
    /// Stages aggregate removals and marks an entity graph for update.
    /// </summary>
    /// <param name="entity">The entity to stage for update.</param>
    /// <returns>The same entity, now tracked for update.</returns>
    /// <remarks>
    /// <see cref="DeleteRemovedAggregates"/> runs first. Entity Framework's <c>Update</c> semantics
    /// are then applied to the graph; no database write occurs until commit.
    /// </remarks>
    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        DeleteRemovedAggregates(entity);
        GetDbSet().Update(entity);
        return await Task.FromResult(entity);
    }

    /// <summary>
    /// Stages aggregate removals and marks a sequence of entity graphs for update.
    /// </summary>
    /// <param name="entities">The entities to stage for update.</param>
    /// <remarks>
    /// The sequence is enumerated once for aggregate cleanup and again by <c>UpdateRange</c>; pass a
    /// repeatable sequence or materialize it before calling this method.
    /// </remarks>
    public async Task UpdateAsync(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
            DeleteRemovedAggregates(entity);

        GetDbSet().UpdateRange(entities);
    }

    /// <summary>
    /// Marks an entity for deletion.
    /// </summary>
    /// <param name="entity">The entity to stage for deletion.</param>
    /// <remarks>No database write occurs until commit.</remarks>
    public virtual async Task DeleteAsync(TEntity entity)
    {
        GetDbSet().Remove(entity);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Marks a sequence of entities for deletion.
    /// </summary>
    /// <param name="entities">The entities to stage for deletion.</param>
    /// <remarks>No database write occurs until commit.</remarks>
    public virtual async Task DeleteAsync(IEnumerable<TEntity> entities)
    {
        GetDbSet().RemoveRange(entities);
        await Task.CompletedTask;
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Gets the Entity Framework set used for change staging.
    /// </summary>
    /// <returns>The Entity Framework set used to stage changes for <typeparamref name="TEntity"/>.</returns>
    protected virtual DbSet<TEntity> GetDbSet() => EntityContext.Set<TEntity>();

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

    /// <summary>
    /// Stages deletion of aggregate children removed from the supplied root.
    /// </summary>
    /// <param name="entity">The aggregate root being updated.</param>
    /// <remarks>
    /// The base implementation does nothing. Override it to read the domain model's removal trackers
    /// and call <see cref="RemoveAggregate{TAggregatedEntity}(IEnumerable{TAggregatedEntity}?)"/>.
    /// This hook runs before the root graph is marked for update.
    /// </remarks>
    protected virtual void DeleteRemovedAggregates(TEntity entity)
    {
    }

    #endregion
}
