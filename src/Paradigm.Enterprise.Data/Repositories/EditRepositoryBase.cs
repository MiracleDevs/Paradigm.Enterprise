using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace Paradigm.Enterprise.Data.Repositories;

[Obsolete("This class it's replaced by EntityViewRepositoryBase instead")]
public abstract class EditRepositoryBase<TEntity, TContext> : ReadRepositoryBase<TEntity, TContext>, IEditRepository<TEntity>
     where TEntity : EntityBase
     where TContext : DbContextBase
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="EditRepositoryBase{TEntity, TContext}" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected EditRepositoryBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns></returns>
    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await GetDbSet().AddAsync(entity);
        return entity;
    }

    /// <summary>
    /// Adds the new entities.
    /// </summary>
    /// <param name="entities">The entities.</param>
    public virtual async Task AddAsync(IEnumerable<TEntity> entities)
    {
        await GetDbSet().AddRangeAsync(entities);
    }

    /// <summary>
    /// Updates the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns></returns>
    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        DeleteRemovedAggregates(entity);
        GetDbSet().Update(entity);
        return await Task.FromResult(entity);
    }

    /// <summary>
    /// Updates the entities.
    /// </summary>
    /// <param name="entities">The entities.</param>
    public async Task UpdateAsync(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
            DeleteRemovedAggregates(entity);

        GetDbSet().UpdateRange(entities);
    }

    /// <summary>
    /// Deletes the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    public virtual async Task DeleteAsync(TEntity entity)
    {
        GetDbSet().Remove(entity);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Deletes the asynchronous.
    /// </summary>
    /// <param name="entities">The entities.</param>
    public virtual async Task DeleteAsync(IEnumerable<TEntity> entities)
    {
        GetDbSet().RemoveRange(entities);
        await Task.CompletedTask;
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Gets the database set.
    /// </summary>
    /// <returns></returns>
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
    /// Deletes the removed aggregates.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual void DeleteRemovedAggregates(TEntity entity)
    {
    }

    #endregion
}